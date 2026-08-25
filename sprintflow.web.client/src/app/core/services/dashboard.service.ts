import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Dashboard } from '../models/dashboard-models';

/**
 * Reads the signed-in user's dashboard.
 *
 * No state and no signal: one page reads this once on open, unlike the Area list or the current
 * board, which several components share. The request carries no user id — the server takes the
 * caller from the token, which is the only reason "my" tasks can mean anything here.
 */
@Injectable()
export class DashboardService {
  private readonly http = inject(HttpClient);

  get(): Observable<Dashboard> {
    return this.http.get<Dashboard>(`${environment.apiBaseUrl}/dashboard`);
  }
}
