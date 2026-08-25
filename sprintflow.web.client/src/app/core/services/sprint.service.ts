import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CreateSprintRequest,
  Sprint,
  SprintDetail,
  SprintListItem,
  SprintTask
} from '../models/sprint-models';

/**
 * Reads and creates sprints.
 *
 * Dates pass straight through in both directions. The server's interval is half-open —
 * `[startDate, endDate)`, the end day excluded — and the form now collects exactly that, so there is
 * no conversion here and no date arithmetic anywhere in the client. A `YYYY-MM-DD` string goes from
 * the date input to the API and back to the template untouched, which is also why no browser
 * timezone can shift a day.
 *
 * Unlike BoardDetailService this holds no state. A sprint list is small, belongs to one page, and
 * nothing else on screen has to stay in step with it, so a signal here would be state kept for its
 * own sake.
 */
@Injectable()
export class SprintService {
  private readonly http = inject(HttpClient);

  /** A board's sprints, in date order. */
  list(boardId: number): Observable<readonly Sprint[]> {
    return this.http.get<readonly Sprint[]>(
      `${environment.apiBaseUrl}/boards/${boardId}/sprints`);
  }

  /**
   * Every sprint in the system, already ordered area → board → start day by the server.
   *
   * For the overview page, which groups by board. Read-only: creating a sprint goes through
   * {@link create}, which is board-scoped.
   */
  listAll(): Observable<readonly SprintListItem[]> {
    return this.http.get<readonly SprintListItem[]>(`${environment.apiBaseUrl}/sprints`);
  }

  get(sprintId: number): Observable<SprintDetail> {
    return this.http.get<SprintDetail>(`${environment.apiBaseUrl}/sprints/${sprintId}`);
  }

  /** The tasks in one sprint, in the shape the task table renders. */
  tasks(sprintId: number): Observable<readonly SprintTask[]> {
    return this.http.get<readonly SprintTask[]>(
      `${environment.apiBaseUrl}/sprints/${sprintId}/tasks`);
  }

  create(boardId: number, request: CreateSprintRequest): Observable<Sprint> {
    return this.http.post<Sprint>(
      `${environment.apiBaseUrl}/boards/${boardId}/sprints`, request);
  }
}
