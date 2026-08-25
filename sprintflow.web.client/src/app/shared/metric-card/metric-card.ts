import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Visual weight. Driven by size and position only — never colour. */
export type MetricTier = 'primary' | 'secondary' | 'tertiary';

@Component({
  selector: 'app-metric-card',
  templateUrl: './metric-card.html',
  standalone: false,
  styleUrl: './metric-card.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MetricCard {
  readonly label = input.required<string>();
  readonly value = input.required<number>();
  readonly tier = input<MetricTier>('secondary');
  readonly loading = input(false);

  /** When set the card renders as a link. Task metrics link to the tasks page (FR-D3). */
  readonly link = input<string | null>(null);
  readonly linkParams = input<Record<string, string> | null>(null);

  protected readonly tierClass = computed(() => `metric-card--${this.tier()}`);

  /** "Overdue tasks, 12" reads better than a bare number for screen readers. */
  protected readonly accessibleName = computed(() => `${this.label()}, ${this.value()}`);
}
