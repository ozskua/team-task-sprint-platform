import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AdminDashboard } from '../models/admin-dashboard-models';

import { environment } from '../../../environments/environment';

/**
 * Reads the system-wide admin overview.
 *
 * No state and no signal: one page reads this once on open. Separate from DashboardService, which
 * reads the caller's own dashboard — the two answer different questions and share no data.
 */
@Injectable()
export class AdminDashboardService {
  private readonly http = inject(HttpClient);

  get(): Observable<AdminDashboard> {
    return this.http.get<AdminDashboard>(`${environment.apiBaseUrl}/admin/dashboard`);
  }
}
