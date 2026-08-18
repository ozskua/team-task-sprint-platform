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

  /** Computed by the server from participation. An empty list is a normal, correct state. */
  readonly teamMembers: readonly TeamMember[];

  /** Already in board order: To Do, custom columns, Completed, Cancelled. */
  readonly columns: readonly BoardColumn[];
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
}

export interface ColumnNameRequest {
  readonly name: string;
}

/** The board's custom columns in the wanted order. System columns are never part of this. */
export interface ReorderColumnsRequest {
  readonly orderedColumnIds: readonly number[];
}
