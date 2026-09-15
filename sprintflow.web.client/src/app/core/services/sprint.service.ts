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

@Injectable()
export class SprintService {
  private readonly http = inject(HttpClient);

  list(boardId: number): Observable<readonly Sprint[]> {
    return this.http.get<readonly Sprint[]>(
      `${environment.apiBaseUrl}/boards/${boardId}/sprints`);
  }

  listAll(): Observable<readonly SprintListItem[]> {
    return this.http.get<readonly SprintListItem[]>(`${environment.apiBaseUrl}/sprints`);
  }

  get(sprintId: number): Observable<SprintDetail> {
    return this.http.get<SprintDetail>(`${environment.apiBaseUrl}/sprints/${sprintId}`);
  }

  tasks(sprintId: number): Observable<readonly SprintTask[]> {
    return this.http.get<readonly SprintTask[]>(
      `${environment.apiBaseUrl}/sprints/${sprintId}/tasks`);
  }

  create(boardId: number, request: CreateSprintRequest): Observable<Sprint> {
    return this.http.post<Sprint>(
      `${environment.apiBaseUrl}/boards/${boardId}/sprints`, request);
  }
}
