import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Invitation, RegisterRequest, RegisterResponse } from '../models/invitation-models';

/**
 * Creating registration invitations, and redeeming one.
 *
 * Deliberately separate from AuthService, which owns the session. Registration establishes no
 * session — the new account signs in afterwards through the ordinary login — so nothing here reads
 * or writes the stored token, and there is still exactly one place a session begins.
 *
 * Holds no state: an invitation is used the moment it is created, and the register page owns the
 * one token it was opened with.
 */
@Injectable()
export class InvitationService {
  private readonly http = inject(HttpClient);

  /** Mints an invitation for the signed-in user to share. */
  create(): Observable<Invitation> {
    return this.http.post<Invitation>(`${environment.apiBaseUrl}/invitations`, {});
  }

  /**
   * Checks whether a link still works, so the page can show the form or explain why it cannot.
   * Completes with no value when the token is usable; errors otherwise.
   */
  checkToken(token: string): Observable<void> {
    return this.http.get<void>(
      `${environment.apiBaseUrl}/invitations/${encodeURIComponent(token)}`);
  }

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${environment.apiBaseUrl}/auth/register`, request);
  }

  /**
   * The link to share for a token.
   *
   * Composed here rather than returned by the API. The browser knows its own origin for certain,
   * whereas the server would have to take it from the request's Host header — and nothing in the
   * client hard-codes a backend address, which is the same reason `apiBaseUrl` is relative.
   */
  registrationUrl(token: string): string {
    return `${window.location.origin}/register?token=${encodeURIComponent(token)}`;
  }
}
