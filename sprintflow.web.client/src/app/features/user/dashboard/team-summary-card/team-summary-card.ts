import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { DashboardTeam } from '../../../../core/models/dashboard-models';

@Component({
  selector: 'app-team-summary-card',
  templateUrl: './team-summary-card.html',
  standalone: false,
  styleUrl: './team-summary-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TeamSummaryCard {
  readonly team = input.required<DashboardTeam>();

  protected readonly assignedTaskLabel = computed(() => {
    const count = this.team().myAssignedTaskCount;

    return `${count} assigned ${count === 1 ? 'task' : 'tasks'}`;
  });
}
