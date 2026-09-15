import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BoardSummary } from '../models/board-summary-models';

@Injectable()
export class BoardSummaryService {
  private readonly http = inject(HttpClient);

  get(boardId: number): Observable<BoardSummary> {
    return this.http.get<BoardSummary>(`${environment.apiBaseUrl}/boards/${boardId}/summary`);
  }
}
