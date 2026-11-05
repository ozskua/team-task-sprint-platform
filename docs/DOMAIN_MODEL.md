# SprintFlow — Domain Model

> **Source of truth for the domain.** This file records the agreed structure of Areas, Boards,
> Teams, workflow columns and Tasks, and the boundary between what is decided and what is not.
>
> **Area, Board, Team, workflow columns and Task are all implemented.** See
> [Status summary](#10-status-summary) for exactly what exists. This document remains the place
> where the model is agreed before it is changed, so a later phase starts from a stable model
> instead of re-deciding it.
>
> Authentication is a separate, already-working system — see [`AUTHENTICATION.md`](AUTHENTICATION.md).

---

## 1. One Company

The application currently represents **exactly one company**. Every user already belongs to it.

There is therefore **no `Company` entity**, no company selection, no company switching and no
multi-tenant isolation. "Company" is a framing assumption, not a table.

> If the product ever needs to serve more than one company, that is a new architectural decision —
> not something to be inferred from this document.

---

## 2. Global roles: Admin and Member

`Admin` and `Member` are **global** application roles, stored in the `Roles` table and carried in the
JWT. They exist today and are unchanged by this document.

`Admin` is the highest-privileged global user type.

**However:** "Admin is the highest role" does **not** mean "Admin can manage every Area, Board, Team
or Task". No such behaviour has been designed. Admin receives no Area/Board/Team/Task-specific
capability in the current direction.

---

## 3. The hierarchy

```
ONE COMPANY
└── Admin / Member
    └── Area
        ├── Board
        │   ├── Team                     (one, automatic, membership derived)
        │   └── WorkflowColumns          (To Do → custom… → Completed → Cancelled)
        │       └── Tasks
        │           └── Assignee         (zero or one)
        │
        ├── Board
        │   ├── Team
        │   └── WorkflowColumns
        │       └── Tasks
        │           └── Assignee
        │
        └── Board
            ├── Team
            └── WorkflowColumns
                └── Tasks
                    └── Assignee
```

A Task belongs to a **Board** and sits in one of that Board's columns. It does not hang off the
Team — the Team is the *derived* view of who is working on the Board, not the Task's parent.

---

## 4. Area

- **Any authenticated user** can create an Area.
- The creator automatically becomes the Area's **manager/owner**, and that is recorded.
- Area ownership grants **no additional authorization** in the current direction.
- There is **no Area membership concept**. Do not introduce `AreaMembers`.
- An Area may carry one optional **webhook URL** (`Areas.WebhookUrl`): the
  address of an external system that receives this Area's sprint report whenever the administrator
  exports it. Only the administrator can read or set it; it never appears in the Area DTO members
  receive. The Area is the customer-facing boundary here — a report sent to an Area's URL contains
  that Area's sprints and nothing else. Boards have no webhook.

## 5. Board

- **Boards are created by users, who name them.** Creating an Area does *not* create a Board:

  ```
  Create Area  → Area only
  Create Board → Board + exactly one Team + three system workflow columns
  ```

  All of it in **one save**, so a Board can never exist without its Team or without its columns.

- An Area can contain **multiple Boards**, and starts with none.
- The creator of a Board is recorded as its **creator/owner**.
- Board ownership grants **no additional authorization** in the current direction.
- A Board belongs to exactly one Area.
- Board names are **not unique**; two Boards in one Area may share a name.
- A Board can be **renamed and deleted**. Deleting it cascades to its Team, its columns, its Tasks
  and their assignments — one delete, the database does the rest.

### Workflow columns

A Board's columns are **rows, not an enum**. Each Board owns its own `WorkflowColumns`; there is no
shared, global set of statuses.

- Three **system columns** exist on every Board: `To Do`, `Completed`, `Cancelled`. They are created
  with the Board and are identified by a `SystemKind` value stored on the row. A column whose
  `SystemKind` is `null` is a **custom** column the user added.
- **Order is fixed:** `To Do → custom… → Completed → Cancelled`. `To Do` is always first;
  `Completed` and `Cancelled` are always the last two, in that order.
- System columns can **never** be renamed, deleted or reordered. The only order the user controls is
  the relative order of the custom columns between `To Do` and `Completed`.
- A new custom column is inserted **immediately before `Completed`**.
- Positions are contiguous integers rewritten by the server, not values the client supplies. The
  client sends *intent* (target column, target index); the server recomputes the order.
- A custom column that holds Tasks **cannot be deleted** — the attempt is rejected and **nothing is
  moved** on the caller's behalf. An empty custom column deletes cleanly. This is not only a service
  rule: the foreign key from `Tasks` to its column is `Restrict`, so the database refuses it too.
- Tasks move **freely** between all four kinds of column, in either direction. `Completed` and
  `Cancelled` are **not terminal** — a Task can come back out of them.
- Movement is confined to the Task's own Board. `Tasks` carries `BoardId` alongside its column, and
  a composite foreign key to `(WorkflowColumnId, BoardId)` makes a cross-board move unstorable.

## 6. Team

- Every Board has **exactly one Team**.
- The Team is **created automatically together with the Board**, in the same transaction. Users
  never create a Team manually and never assign its owner manually.
- A Team is **never shared** between Boards: Board A has Team A, Board B has Team B.
- The Team owner is **inherited from the Board creator** — the person who created the Board is the
  Team owner.

> **Team owner is an ownership reference inherited from the Board creator; it does not grant any
> authorization.**

### The three creator fields are not three independent concepts

| Relationship | Rule |
|---|---|
| Area creator ↔ Board creator | **Independent.** They may differ, and that is valid |
| Board creator → Team owner | **Determining.** The Board's creator becomes the Team's owner |
| Team owner | **Inherited, never independent.** It must equal the Board creator |

### Database-enforced invariant

`Team.CreatedByUserId == Board.CreatedByUserId` is **enforced by the database**, not only by
application code. `Teams` carries a composite foreign key on `(BoardId, CreatedByUserId)` pointing
at the alternate key `AK_Boards_BoardId_CreatedByUserId`, so a Team whose owner is not that Board's
creator cannot be stored at all. A unique index on `Teams.BoardId` separately guarantees one Team
per Board.

`Team.CreatedByUserId` is therefore **not an independent creator field** — it is the team owner
reference inherited from `Board.CreatedByUserId`.

## 7. Task and Team membership

- Tasks belong to Boards, and sit in one of that Board's columns.
- A Task has a required **title** and an optional **description**.
- The creator of a Task is recorded as its **creator/owner**, resolved from the token on the server —
  the client cannot supply it.
- **A Task has zero or one assignee.** Zero means Unassigned; naming someone replaces whoever was
  there before. The server checks that the person exists and is **active**.
- **Creator and assignee are separate people-shaped fields with different meanings:** one wrote the
  Task down, the other is answerable for it. They may be the same person, and often are not.
- **Creating a Task never requires membership.** The reverse would be circular — nobody could create
  the first Task on a new Board, so the Board could never gain a member.
- A Member takes part in a Board by being **assigned a Task on it**. Assignment on **any** Task of a
  Board makes that user a member of that Board's Team.
- The same Member assigned Tasks on different Boards is a member of **each** corresponding Board's
  Team, so membership in several Teams at once is normal.
- Users do not manually create Teams, and users do not manually assign Team membership.

> **Cardinality note.** The relationship is stored in a table named `TaskParticipants`, and an
> earlier revision of this document allowed **several** participants per Task. That was superseded:
> the model is now **zero or one assignee**, and it is a database rule, not a convention — a unique
> index on `TaskParticipants.TaskId` makes a second assignee unstorable. The table name was kept
> because the membership derivation below is unchanged; only the cardinality narrowed.

### Team membership is computed, never stored

**Decided.** `TaskParticipants` is the **single source of truth** for Team membership. There is no
separate membership table and no second copy of the relationship.

Membership is derived along this path:

```
Task → its Board → that Board's Team
  ↑
TaskParticipant → User
```

A user is a member of a Team **if and only if** they are assigned at least one Task belonging to
that Team's Board. Nothing else makes someone a member.

The query joins on the **Task's `BoardId`**, not on its column. Moving a Task between columns never
touches `BoardId`, so no column operation — create, rename, reorder, delete, or a drag from `To Do`
to `Cancelled` — can add or remove a member. That is structural, not something the service has to
remember.

Consequences that follow directly:

- Membership is **dynamic**. It changes as assignment changes, with no membership record to
  maintain.
- Unassigning a user from their **last** Task on a Board ends their membership of that Team
  immediately, because there was never a row saying otherwise. Deleting that Task does the same:
  the assignment row is removed by a database cascade, and no clean-up code is involved.
- **`TeamMembers` and `UserTeams` must not exist.** Neither will be created. Persisting membership
  separately would create a second truth that can disagree with `TaskParticipants`.
- `TaskParticipants` is the only assignment table, and there is no equivalent independent
  membership table beside it.

### Three things that are easy to conflate

| Concept | Definition | Storage |
|---|---|---|
| **Team owner** | `Board.CreatedByUserId` | Persisted on `Teams`, database-enforced to equal the Board creator |
| **Team membership** | Assigned ≥ 1 Task on that Team's Board | **Computed** from `TaskParticipants` — never stored |
| **Task assignment** | An explicit user↔task relationship, at most one per Task | Persisted in `TaskParticipants` |

They are independent of one another. In particular:

> **Being the Board creator — and therefore the Team owner — does not make a user a member of that
> Team.** Ownership and membership are separate. A Team owner assigned none of the Board's Tasks is
> not a member of the Team, and that is correct, not a defect. A Board with zero members is valid.

And, as everywhere else in this document, none of the three grants any authorization.

---

## 8. Ownership is not authorization

This distinction is permanent, and it survived into the implementation.

| Relationship | Recorded as |
|---|---|
| Area creator | Area owner/manager |
| Board creator | Board owner |
| Board creator | Team owner (inherited) |
| Task creator | Task owner/creator |
| Task assignee | The person answerable for the Task |

Being the creator or owner of an Area, Board, Team or Task grants **no** permission to edit, delete,
manage, assign users, change permissions or administer that entity. Being a Task's **assignee**
grants none either: any authenticated user may edit, move or delete any Task, and the assignee has
no special standing over their own.

There is **no scoped authorization model**. Do not create `AreaManagerGuard`, `BoardOwnerGuard`,
`TaskOwnerGuard` or any owner-based authorization. Those may become future features; they are not
part of the current direction.

**Authentication** answers *"who is this user?"* — implemented, see [`AUTHENTICATION.md`](AUTHENTICATION.md).
**Global authorization** answers *"what global user type is this?"* — Admin or Member.
**Scoped authorization** would answer *"what may this user do with this specific object?"* — not designed.

---

## 9. Future decisions

### Resolved — the Team membership model

The first three questions in this section are **closed**. They are recorded here rather than
deleted, so the decision and its reasoning stay visible:

| Was open | Now decided |
|---|---|
| Whether a physical `UserTeams` table should exist | **No.** It will never be created |
| Whether `TaskParticipants` is the single source of truth for Team membership | **Yes.** It is the only persisted source |
| Whether Team membership is stored or computed | **Computed**, at read time, from `TaskParticipants` |

In full: Team membership is **computed, not stored**. `TaskParticipants` is the single source of
truth. No independent membership table exists — neither `TeamMembers` nor `UserTeams`, nor any
equivalent. Membership is derived from assignment on Tasks belonging to that Team's Board.
Team **ownership** is a separate concept from membership, and being the Board creator or Team owner
does **not** by itself make a user a member. See [§7](#7-task-and-team-membership).

### Resolved — Task deletion and cascade

Question 5 is **closed and implemented**:

| Was open | Now decided |
|---|---|
| Deletion / cascade behaviour when a **Task** is removed | **Hard delete.** No soft delete and no `IsDeleted` column. The assignment row goes with it by database cascade |

Because membership is derived rather than stored, no separate clean-up exists or is needed: if that
Task carried the user's last assignment on the Board, they stop being a member the moment the row
disappears.

### Still open

Deliberately **not decided**. Do not turn any of these into implementation.

| # | Open question |
|---|---|
| 6 | Future permissions of an Area manager |
| 7 | Future permissions of a Board or Team owner |
| 8 | The scoped authorization model itself |
| 9 | Whether Admin gains domain-level capabilities later |
| 10 | Long-term replacement for the `Auth:AdminUserId` mechanism |

---

## 10. Status summary

### Implemented

**The whole model described above is built** — Area, Board, Team, workflow columns, Task and
assignment. Nothing in sections 1–9 is aspirational.

| Concept | Backend | Database | Angular |
|---|---|---|---|
| Area | entity, repository, service, `POST`/`GET /api/areas` | `Areas` | sidebar: create, list, Area Information |
| Board | entity, repository, service, `POST /api/areas/{areaId}/boards`, `GET`/`PUT`/`DELETE /api/boards/{boardId}` | `Boards` + alternate key `(BoardId, CreatedByUserId)` | sidebar entry links to the board page; rename and delete |
| Team | entity, repository, service, `GET /api/teams` | `Teams`, unique `IX_Teams_BoardId`, composite FK to the board's alternate key | read-only Teams page; avatar-only member list in the board header (initials, or the member's profile photo) |
| Workflow column | entity, repository, service, `POST /api/boards/{boardId}/columns`, `PUT /api/boards/{boardId}/columns/order`, `PUT`/`DELETE /api/columns/{columnId}` | `WorkflowColumns`, alternate key `(WorkflowColumnId, BoardId)`, filtered unique index on `(BoardId, SystemKind)` | lanes on the board page; create, rename, reorder, delete |
| Task | entity, repository, service, `POST /api/boards/{boardId}/tasks`, `PUT`/`DELETE /api/tasks/{taskId}`, `PUT /api/tasks/{taskId}/move` | `Tasks`, composite FK to the column's alternate key with `Restrict` | cards with drag-and-drop, details, create, edit, delete |
| Assignment | handled inside task create and update — there are no separate participant endpoints | `TaskParticipants`, composite PK `(TaskId, UserId)`, unique `IX_TaskParticipants_TaskId`, `Cascade` from `Tasks` | a single `Assignee` selector with a visible `Unassigned` option; avatar on the card — initials, or the assignee's profile photo |

Six migrations cover the domain: `AddAreas`, `AddBoards`, `AddTeams`,
`EnforceTeamOwnerMatchesBoardCreator`, `AddTasksAndWorkflowColumns`, `EnforceSingleTaskAssignee`.
(`InitialCreate` and `SetEmailCollation` belong to authentication.)

Every domain endpoint carries `[Authorize]` and nothing more — see
[§8](#8-ownership-is-not-authorization).

### Not implemented, and not planned for this version

- **No `Company` entity** — one company is a framing assumption, not a table. See [§1](#1-one-company).
- **No scoped authorization** and no owner-based guard of any kind. See [§8](#8-ownership-is-not-authorization).
- **No Area rename or delete endpoint.** The schema cascades correctly, but nothing exposes it.
- **No `UserTeams`, no `TeamMembers`.** These are not "not yet implemented" — they are **ruled out
  permanently**. See [§7](#7-task-and-team-membership).

### Implemented and unaffected by this document

Authentication (login, BCrypt, JWT, guards, Admin/Member), the shared user directory
(`GET /api/users`, readable by any authenticated user) and application logging (NLog).
