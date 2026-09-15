import { TaskPriority } from '../../core/models/board-detail-models';

export const TASK_PRIORITY_LABELS: Readonly<Record<TaskPriority, string>> = {
  0: 'Önceliksiz',
  1: 'Normal',
  2: 'Önemli',
  3: 'Kritik'
};

export const TASK_PRIORITIES: readonly TaskPriority[] = [0, 1, 2, 3];

export const DEFAULT_TASK_PRIORITY: TaskPriority = 1;

export function taskPriorityLabel(priority: TaskPriority): string {
  return TASK_PRIORITY_LABELS[priority];
}
