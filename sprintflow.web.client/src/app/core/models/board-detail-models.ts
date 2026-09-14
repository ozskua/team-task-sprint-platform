/**
 * Shapes returned by the board endpoints. Every field comes from the API — the client fills in
 * none of them, and in particular never works out a position or a team membership itself.
 */

/** Non-null marks one of the three columns every board is required to have. */
export type SystemColumnKind = 'ToDo' | 'Completed' | 'Cancelled';

/**
 * How important a task is. The numbers are the server's enum and are stored, so they are sent and
 * received as-is; the Turkish wording on screen comes from `task-priority.ts`.
 */
export type TaskPriority = 0 | 1 | 2 | 3;

/** The one person responsible for a task. Not the creator: one wrote it down, the other does it. */
export interface TaskAssignee {
  readonly userId: number;
  readonly fullName: string;
}

/**
 * A member of the board's team. Computed by the server from task participation — there is no
 * membership table, and the client must not derive it a second way.
 */
export interface TeamMember {
  readonly userId: number;
  readonly fullName: string;
}

export interface BoardTask {
  readonly taskId: number;
  readonly title: string;

  readonly description: string | null;

  readonly boardId: number;
  readonly taskColumnId: number;

  /** Decided by the server. Never sent from here. */
  readonly position: number;

  readonly priority: TaskPriority;

  /** Recorded ownership. It grants no permission, and does not make the creator a participant. */
  readonly createdByUserId: number;
  readonly createdByFullName: string;

  /** ISO date-time without an offset, exactly as the server stored it. */
  readonly createdDate: string;

  readonly assignee: TaskAssignee | null;

  /** Always a sprint of this task's own board. */
  readonly sprintId: number | null;

  /** Carried so a card can label itself without a lookup. */
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
   * Computed by the server from participation; an empty list is normal. Always the whole board's
   * members, even when the page is scoped to a sprint.
   */
  readonly teamMembers: readonly TeamMember[];

  /**
   * The sprint the tasks below were filtered to, or `null` for the whole board. The server's shape,
   * with an exclusive `endDate` — anything rendering dates should go through `SprintService`.
   */
  readonly selectedSprint: BoardSelectedSprint | null;

  /**
   * Already in board order: To Do, custom columns, Completed, Cancelled. The set never depends on
   * the selected sprint — only which tasks sit in them does.
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
 * The only things a client may supply. No creator and no position: the server takes the first from
 * the token and works out the second itself.
 */
export interface CreateTaskRequest {
  readonly title: string;
  readonly description?: string;

  /** Omitted creates the task as Normal, which is also the column default in the database. */
  readonly priority?: TaskPriority;

  /** Left out for the board-level "New Task" action, which starts the task in To Do. */
  readonly columnId?: number;

  readonly assigneeUserId?: number | null;

  /**
   * Normally the sprint the board is scoped to, so a task added while looking at one belongs to it
   * rather than seeming to vanish. Omitted on an unscoped board.
   */
  readonly sprintId?: number | null;
}

export interface UpdateTaskRequest {
  readonly title: string;
  readonly description?: string;

  /**
   * Always sent: the server writes whatever arrives, so omitting it would silently reset the task
   * to Normal. An update carries the whole task, not just what changed.
   */
  readonly priority: TaskPriority;

  /** `null` unassigns the task; a different id replaces whoever was there. */
  readonly assigneeUserId?: number | null;
}

/** One comment on a task. The author is the server's answer, taken from the writer's token. */
export interface TaskComment {
  readonly taskCommentId: number;

  /** Compared with the session's own id to decide whether to offer a delete. */
  readonly userId: number;

  readonly userFullName: string;
  readonly content: string;
  readonly createdDate: string;

  readonly attachments: readonly TaskAttachment[];
}

/**
 * One attached file, as metadata. The bytes are fetched by id, and the name the file has on disk is
 * deliberately not part of this.
 */
export interface TaskAttachment {
  readonly taskAttachmentId: number;
  readonly fileName: string;
  readonly contentType: string;
  readonly sizeBytes: number;
  readonly uploadedByUserId: number;
  readonly uploadedByFullName: string;
  readonly createdDate: string;
}

/** Everything the task card shows, in the one response that opening a card costs. */
export interface TaskDetail {
  readonly taskId: number;
  readonly title: string;
  readonly description: string | null;
  readonly boardId: number;
  readonly boardName: string;
  readonly taskColumnId: number;
  readonly columnName: string;
  readonly position: number;
  readonly priority: TaskPriority;
  readonly createdByUserId: number;
  readonly createdByFullName: string;

  readonly createdDate: string;

  readonly assignee: TaskAssignee | null;
  readonly sprintId: number | null;
  readonly sprintName: string | null;
  readonly comments: readonly TaskComment[];

  /** Only the files belonging to no comment, so the thread draws each file exactly once. */
  readonly attachments: readonly TaskAttachment[];
}

export interface CreateTaskCommentRequest {
  readonly content: string;
}

/** Intent, not arithmetic: which column and which slot. The server renumbers everything. */
export interface MoveTaskRequest {
  readonly targetColumnId: number;
  readonly targetIndex: number;

  /**
   * The sprint the board was scoped to, or omitted for the whole board. Not where the task is
   * going — a move never changes a task's sprint. It says what `targetIndex` was counted against:
   * on a scoped board "third" means the third *visible* card, and the server translates.
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
