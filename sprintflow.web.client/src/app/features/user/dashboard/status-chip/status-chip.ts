import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { TaskStatus } from '../user-dashboard-models';

/**
 * Shows a single task status as a badge.
 *
 * Exists mainly to keep one rule in one place: a status is a tinted fill with
 * dark ink on top, never coloured text. The project's status colours measure
 * 2.1-3.8:1 against white, so as text they would be unreadable.
 */
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
