import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Area, Board, CreateAreaRequest, CreateBoardRequest } from '../models/area-models';

/**
 * Owns the Area list. The signal exists so both shells show the same Areas and a newly created one
 * appears without a reload — the same approach AuthService uses for the session.
 */
@Injectable()
export class AreaService {
  private readonly http = inject(HttpClient);

  private readonly areaState = signal<readonly Area[]>([]);

  readonly areas = this.areaState.asReadonly();

  load(): Observable<readonly Area[]> {
    return this.http
      .get<readonly Area[]>(`${environment.apiBaseUrl}/areas`)
      .pipe(tap(areas => this.areaState.set(areas)));
  }

  /**
   * Sends only the name. The Area added to the list is the one the server returned — no locally
   * assembled stand-in, so `createdByFullName` and `createdDate` are real from the first render.
   * A new Area arrives with no boards.
   */
  create(name: string): Observable<Area> {
    const request: CreateAreaRequest = { name };

    return this.http
      .post<Area>(`${environment.apiBaseUrl}/areas`, request)
      .pipe(tap(area => this.areaState.update(areas => [area, ...areas])));
  }

  /**
   * Adds a board to an Area. The server also creates the board's team; that has no client-side
   * representation, so nothing about it is modelled here.
   *
   * The board appended to the Area is the server's object, never one built from the typed name.
   */
  createBoard(areaId: number, name: string): Observable<Board> {
    const request: CreateBoardRequest = { name };

    return this.http
      .post<Board>(`${environment.apiBaseUrl}/areas/${areaId}/boards`, request)
      .pipe(tap(board => this.areaState.update(areas =>
        areas.map(area => area.areaId === areaId
          ? { ...area, boards: [...area.boards, board] }
          : area))));
  }
}
