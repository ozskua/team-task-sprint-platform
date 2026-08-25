import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BoardSummary } from '../models/board-summary-models';

/**
 * One board's figures.
 *
 * Holds no state: each board's summary belongs to the page showing it, and nothing else on screen
 * has to stay in step with it — so a signal here would be state kept for its own sake, and would
 * risk one board's numbers outliving the page that asked for them.
 */
@Injectable()
export class BoardSummaryService {
  private readonly http = inject(HttpClient);

  get(boardId: number): Observable<BoardSummary> {
    return this.http.get<BoardSummary>(`${environment.apiBaseUrl}/boards/${boardId}/summary`);
  }
}
