import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AdminDashboard } from '../models/admin-dashboard-models';

import { environment } from '../../../environments/environment';

@Injectable()
export class AdminDashboardService {
  private readonly http = inject(HttpClient);

  get(): Observable<AdminDashboard> {
    return this.http.get<AdminDashboard>(`${environment.apiBaseUrl}/admin/dashboard`);
  }
}
