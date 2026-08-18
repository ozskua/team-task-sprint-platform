import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Team } from '../models/team-models';

/**
 * Reads the teams. There is nothing to write: a team is created with its board, has no name of its
 * own, and its members follow from who is assigned to the board's tasks.
 */
@Injectable()
export class TeamService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<readonly Team[]> {
    return this.http.get<readonly Team[]>(`${environment.apiBaseUrl}/teams`);
  }
}
