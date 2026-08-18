import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { UserListItem } from '../models/user-models';

/** Reads the user list. The endpoint is Admin-only; the server enforces that, not this class. */
@Injectable()
export class UserService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<readonly UserListItem[]> {
    return this.http.get<readonly UserListItem[]>(`${environment.apiBaseUrl}/users`);
  }
}
