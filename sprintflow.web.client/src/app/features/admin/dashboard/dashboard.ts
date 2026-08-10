import { ChangeDetectionStrategy, Component, signal } from '@angular/core';

import { DashboardMetrics, EMPTY_METRICS, TeamRow, TrendPoint } from './dashboard-models';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  standalone: false,
  styleUrl: './dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Dashboard {
  /**
   * There is no API yet, so the page renders its zero/empty state — which is also
   * the truthful state of a database with no users, teams or tasks. When the
   * dashboard endpoint exists it sets these same signals; nothing else changes.
   */
  protected readonly loading = signal(false);
  protected readonly metrics = signal<DashboardMetrics>(EMPTY_METRICS);
  protected readonly trend = signal<readonly TrendPoint[]>([]);
  protected readonly teams = signal<readonly TeamRow[]>([]);
}
