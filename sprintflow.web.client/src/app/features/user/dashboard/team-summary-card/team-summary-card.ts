import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { DashboardTeam } from '../../../../core/models/dashboard-models';
import { initialsOf } from '../../../../shared/initials';

/**
 * One team the signed-in user belongs to.
 *
 * A team has no name of its own — it exists because a board does, one for one — so the board names
 * the card and is what it links to.
 */
@Component({
  selector: 'app-team-summary-card',
  templateUrl: './team-summary-card.html',
  standalone: false,
  styleUrl: './team-summary-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TeamSummaryCard {
  readonly team = input.required<DashboardTeam>();

  /**
   * Every column, including Completed and Cancelled — the same tasks that make this person a member
   * in the first place. Worded "assigned" rather than "open" for exactly that reason: it can be
   * larger than the number of rows this board contributes to My Tasks.
   */
  protected readonly assignedTaskLabel = computed(() => {
    const count = this.team().myAssignedTaskCount;

    return `${count} assigned ${count === 1 ? 'task' : 'tasks'}`;
  });

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }
}
