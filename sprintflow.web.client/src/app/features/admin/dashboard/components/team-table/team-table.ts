import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { TeamRow } from '../../dashboard-models';

/**
 * Renders exactly the rows it is given, in the order given.
 * Ordering, filtering and paging are backend concerns and belong to a later phase.
 */
@Component({
  selector: 'app-team-table',
  templateUrl: './team-table.html',
  standalone: false,
  styleUrl: './team-table.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TeamTable {
  readonly rows = input.required<readonly TeamRow[]>();
  readonly loading = input(false);

  /** Rows drawn while loading, so the table keeps its shape. */
  protected readonly skeletonRows = [0, 1, 2];

  /** Share of a team's tasks that are done. Zero tasks reads as 0%, not an error. */
  protected progressOf(row: TeamRow): number {
    const total = row.openTasks + row.completedTasks;
    return total === 0 ? 0 : Math.round((row.completedTasks / total) * 100);
  }
}
