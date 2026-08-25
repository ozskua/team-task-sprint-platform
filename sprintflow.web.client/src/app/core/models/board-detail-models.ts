/**
 * Shapes returned by the board endpoints. Every field comes from the API — the client fills in
 * none of them, and in particular never works out a position or a team membership itself.
 */

/** Non-null marks one of the three columns every board is required to have. */
export type SystemColumnKind = 'ToDo' | 'Completed' | 'Cancelled';

/**
 * The one person responsible for a task. Absent when the task is unassigned.
 *
 * Deliberately not the same thing as the creator: one wrote the task down, the other has to do it.
 */
export interface TaskAssignee {
  readonly userId: number;
  readonly fullName: string;
}

/**
 * A member of the board's team. The server computes this from task participation; there is no
 * membership table and the client must not try to derive it a second way.
 */
export interface TeamMember {
  readonly userId: number;
  readonly fullName: string;
}

export interface BoardTask {
  readonly taskId: number;
  readonly title: string;

  /** `null` when nothing was written — not an empty string. */
  readonly description: string | null;

  readonly boardId: number;
  readonly taskColumnId: number;

  /** Decided by the server. Never sent from here. */
  readonly position: number;

  /** Recorded ownership. It grants no permission, and does not make the creator a participant. */
  readonly createdByUserId: number;
  readonly createdByFullName: string;

  /** ISO date-time without an offset, exactly as the server stored it. */
  readonly createdDate: string;

  /** `null` when nobody is responsible for it yet. */
  readonly assignee: TaskAssignee | null;

  /** The sprint this task is in, or `null`. Always a sprint of this task's board. */
  readonly sprintId: number | null;

  /** That sprint's name, or `null` — carried so a card can label itself without a lookup. */
  readonly sprintName: string | null;
}

export interface BoardColumn {
  readonly workflowColumnId: number;
  readonly boardId: number;
  readonly name: string;
  readonly position: number;

  /** `null` for a custom column — which is what makes rename, delete and drag available. */
  readonly systemKind: SystemColumnKind | null;

  readonly tasks: readonly BoardTask[];
}

export interface BoardDetail {
  readonly boardId: number;
  readonly name: string;
  readonly areaId: number;

  /** Recorded ownership. Deliberately separate from `teamMembers` — they are different people. */
  readonly createdByUserId: number;
  readonly createdByFullName: string;

  readonly createdDate: string;

  /**
   * Computed by the server from participation. An empty list is a normal, correct state.
   *
   * Always the whole board's members, even when the page is scoped to a sprint — who is on the
   * team is not a question about one stretch of days.
   */
  readonly teamMembers: readonly TeamMember[];

  /**
   * The sprint the tasks below were filtered to, or `null` when the whole board is showing.
   *
   * Note this is the server's shape, with an exclusive `endDate`. Only the board page reads it, and
   * only to name the sprint; anything rendering dates should go through `SprintService`.
   */
  readonly selectedSprint: BoardSelectedSprint | null;

  /**
   * Already in board order: To Do, custom columns, Completed, Cancelled. The set of columns never
   * depends on the selected sprint — only which tasks sit in them does.
   */
  readonly columns: readonly BoardColumn[];
}

/** Just enough of the selected sprint for the board header to say what it is showing. */
export interface BoardSelectedSprint {
  readonly sprintId: number;
  readonly name: string;
  readonly status: 'Upcoming' | 'Active' | 'Archived';
  readonly taskCount: number;
}

/**
 * The only things a client may supply when creating a task. No creator and no position: the
 * server takes the first from the token and works out the second itself.
 */
export interface CreateTaskRequest {
  readonly title: string;
  readonly description?: string;

  /** Left out for the board-level "New Task" action, which starts the task in To Do. */
  readonly columnId?: number;

  /** `null` or omitted creates the task unassigned. */
  readonly assigneeUserId?: number | null;

  /**
   * The sprint to create the task in — normally the one the board is scoped to, so a task added
   * while looking at a sprint belongs to it instead of seeming to vanish. Omitted on an unscoped
   * board.
   */
  readonly sprintId?: number | null;
}

export interface UpdateTaskRequest {
  readonly title: string;
  readonly description?: string;

  /** `null` unassigns the task; a different id replaces whoever was there. */
  readonly assigneeUserId?: number | null;
}

/** Intent, not arithmetic: which column and which slot. The server renumbers everything. */
export interface MoveTaskRequest {
  readonly targetColumnId: number;
  readonly targetIndex: number;

  /**
   * The sprint the board was scoped to, or omitted for the whole board.
   *
   * Not where the task is going — a move never changes a task's sprint. It tells the server what
   * `targetIndex` was counted against: on a scoped board the user only sees some of a column's
   * cards, so "third" means the third visible one. The server translates.
   */
  readonly sprintId?: number | null;
}

/** Which sprint a task belongs to. `null` takes it out of whichever one it is in. */
export interface AssignTaskSprintRequest {
  readonly sprintId: number | null;
}

export interface ColumnNameRequest {
  readonly name: string;
}

/** The board's custom columns in the wanted order. System columns are never part of this. */
export interface ReorderColumnsRequest {
  readonly orderedColumnIds: readonly number[];
}
