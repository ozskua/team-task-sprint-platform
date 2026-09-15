import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Team } from '../models/team-models';

@Injectable()
export class TeamService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<readonly Team[]> {
    return this.http.get<readonly Team[]>(`${environment.apiBaseUrl}/teams`);
  }
}
