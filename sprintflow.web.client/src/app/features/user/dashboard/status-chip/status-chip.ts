import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { TaskStatus } from '../user-dashboard-models';

@Component({
  selector: 'app-status-chip',
  templateUrl: './status-chip.html',
  standalone: false,
  styleUrl: './status-chip.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatusChip {
  readonly status = input.required<TaskStatus>();

  protected readonly label = computed(() =>
    this.status() === 'InProgress' ? 'In Progress' : this.status()
  );

  protected readonly chipClass = computed(() => `status-chip--${this.status().toLowerCase()}`);
}
