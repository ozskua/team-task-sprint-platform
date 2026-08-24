import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  AdminDashboardCounts,
  AdminDashboardTask
} from '../../../core/models/admin-dashboard-models';
import { AdminDashboardService } from '../../../core/services/admin-dashboard.service';

const EMPTY_COUNTS: AdminDashboardCounts = {
  toDo: 0,
  open: 0,
  completed: 0,
  cancelled: 0,
  totalUsers: 0,
  totalBoards: 0
};

/**
 * The admin overview: the state of the system, and every task in it.
 *
 * System-wide, and the same for every admin — unlike the member dashboard, which answers "what is
 * mine". The task table owns its own search and filter; this component only fetches and reports.
 */
@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  standalone: false,
  styleUrl: './dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Dashboard {
  private readonly adminDashboardService = inject(AdminDashboardService);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  protected readonly counts = signal<AdminDashboardCounts>(EMPTY_COUNTS);
  protected readonly tasks = signal<readonly AdminDashboardTask[]>([]);

  constructor() {
    this.adminDashboardService.get()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: dashboard => {
          this.counts.set(dashboard.counts);
          this.tasks.set(dashboard.tasks);
          this.loading.set(false);
        },
        error: () => {
          // The message is a local constant; the server's text is never shown.
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

  /**
   * Completed over everything that was ever going to be done. Cancelled is left out of the
   * denominator deliberately: abandoning work is not progress, and counting it would let the figure
   * be raised by cancelling tasks.
   */
  protected readonly progressPercent = computed(() => {
    const { toDo, open, completed } = this.counts();
    const total = toDo + open + completed;

    return total === 0 ? 0 : Math.round((completed / total) * 100);
  });
}
