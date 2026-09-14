import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { SprintReportRow } from '../models/sprint-report-models';

import { environment } from '../../../environments/environment';

/**
 * The admin sprint report. One call, one list — a sprint's tasks are read through
 * `SprintService.tasks` when a row is opened, so nothing here fetches them.
 */
@Injectable()
export class SprintReportService {
  private readonly http = inject(HttpClient);

  list(): Observable<readonly SprintReportRow[]> {
    return this.http.get<readonly SprintReportRow[]>(`${environment.apiBaseUrl}/admin/sprint-report`);
  }
}
