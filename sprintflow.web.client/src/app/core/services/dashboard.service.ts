import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Dashboard } from '../models/dashboard-models';

@Injectable()
export class DashboardService {
  private readonly http = inject(HttpClient);

  get(): Observable<Dashboard> {
    return this.http.get<Dashboard>(`${environment.apiBaseUrl}/dashboard`);
  }
}
