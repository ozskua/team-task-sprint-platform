import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Area, Board, CreateAreaRequest, CreateBoardRequest } from '../models/area-models';

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

  create(name: string): Observable<Area> {
    const request: CreateAreaRequest = { name };

    return this.http
      .post<Area>(`${environment.apiBaseUrl}/areas`, request)
      .pipe(tap(area => this.areaState.update(areas => [area, ...areas])));
  }

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
