export type SystemColumnKind = 'ToDo' | 'Completed' | 'Cancelled';

export type TaskPriority = 0 | 1 | 2 | 3;

export interface TaskAssignee {
  readonly userId: number;
  readonly fullName: string;
}

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

  readonly position: number;

  readonly priority: TaskPriority;

  readonly createdByUserId: number;
  readonly createdByFullName: string;

  readonly createdDate: string;

  readonly assignee: TaskAssignee | null;

  readonly sprintId: number | null;

  readonly sprintName: string | null;
}

export interface BoardColumn {
  readonly workflowColumnId: number;
  readonly boardId: number;
  readonly name: string;
  readonly position: number;

  readonly systemKind: SystemColumnKind | null;

  readonly tasks: readonly BoardTask[];
}

export interface BoardDetail {
  readonly boardId: number;
  readonly name: string;
  readonly areaId: number;

  readonly createdByUserId: number;
  readonly createdByFullName: string;

  readonly createdDate: string;

  readonly teamMembers: readonly TeamMember[];

  readonly selectedSprint: BoardSelectedSprint | null;

  readonly columns: readonly BoardColumn[];
}

export interface BoardSelectedSprint {
  readonly sprintId: number;
  readonly name: string;
  readonly status: 'Upcoming' | 'Active' | 'Archived';
  readonly taskCount: number;
}

export interface CreateTaskRequest {
  readonly title: string;
  readonly description?: string;

  readonly priority?: TaskPriority;

  readonly columnId?: number;

  readonly assigneeUserId?: number | null;

  readonly sprintId?: number | null;
}

export interface UpdateTaskRequest {
  readonly title: string;
  readonly description?: string;

  readonly priority: TaskPriority;

  readonly assigneeUserId?: number | null;
}

export interface TaskComment {
  readonly taskCommentId: number;

  readonly userId: number;

  readonly userFullName: string;
  readonly content: string;
  readonly createdDate: string;

  readonly attachments: readonly TaskAttachment[];
}

export interface TaskAttachment {
  readonly taskAttachmentId: number;
  readonly fileName: string;
  readonly contentType: string;
  readonly sizeBytes: number;
  readonly uploadedByUserId: number;
  readonly uploadedByFullName: string;
  readonly createdDate: string;
}

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

  readonly attachments: readonly TaskAttachment[];
}

export interface CreateTaskCommentRequest {
  readonly content: string;
}

export interface MoveTaskRequest {
  readonly targetColumnId: number;
  readonly targetIndex: number;

  readonly sprintId?: number | null;
}

export interface AssignTaskSprintRequest {
  readonly sprintId: number | null;
}

export interface ColumnNameRequest {
  readonly name: string;
}

export interface ReorderColumnsRequest {
  readonly orderedColumnIds: readonly number[];
}
