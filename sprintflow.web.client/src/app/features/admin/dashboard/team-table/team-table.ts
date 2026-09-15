import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { TeamRow } from '../dashboard-models';

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

  protected readonly skeletonRows = [0, 1, 2];

  protected progressOf(row: TeamRow): number {
    const total = row.openTasks + row.completedTasks;
    return total === 0 ? 0 : Math.round((row.completedTasks / total) * 100);
  }
}
