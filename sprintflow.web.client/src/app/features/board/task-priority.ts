import { TaskPriority } from '../../core/models/board-detail-models';

/**
 * The Turkish wording for each priority, and the order the four are offered in.
 *
 * Here rather than in each component because the card, the detail dialog and the task form all draw
 * the same four words, and the domain keeps the numbers — only the screen speaks Turkish.
 */
export const TASK_PRIORITY_LABELS: Readonly<Record<TaskPriority, string>> = {
  0: 'Önceliksiz',
  1: 'Normal',
  2: 'Önemli',
  3: 'Kritik'
};

/** Least important first, so the list reads as a scale rather than as a set of options. */
export const TASK_PRIORITIES: readonly TaskPriority[] = [0, 1, 2, 3];

/** What a new task gets, matching the column default in the database. */
export const DEFAULT_TASK_PRIORITY: TaskPriority = 1;

export function taskPriorityLabel(priority: TaskPriority): string {
  return TASK_PRIORITY_LABELS[priority];
}
