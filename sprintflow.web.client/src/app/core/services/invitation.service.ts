import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Invitation, RegisterRequest, RegisterResponse } from '../models/invitation-models';

@Injectable()
export class InvitationService {
  private readonly http = inject(HttpClient);

  create(): Observable<Invitation> {
    return this.http.post<Invitation>(`${environment.apiBaseUrl}/invitations`, {});
  }

  checkToken(token: string): Observable<void> {
    return this.http.get<void>(
      `${environment.apiBaseUrl}/invitations/${encodeURIComponent(token)}`);
  }

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${environment.apiBaseUrl}/auth/register`, request);
  }

  registrationUrl(token: string): string {
    return `${window.location.origin}/register?token=${encodeURIComponent(token)}`;
  }
}
