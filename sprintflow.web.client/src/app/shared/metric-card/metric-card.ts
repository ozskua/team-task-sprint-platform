import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

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

  readonly link = input<string | null>(null);
  readonly linkParams = input<Record<string, string> | null>(null);

  protected readonly tierClass = computed(() => `metric-card--${this.tier()}`);

  protected readonly accessibleName = computed(() => `${this.label()}, ${this.value()}`);
}
