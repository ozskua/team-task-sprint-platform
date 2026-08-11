import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthSession, LoginRequest, LoginResponse } from '../models/auth-models';
import { TokenStorage } from './token-storage';

/**
 * Owns the signed-in session. Makes no authorization decision of its own: `isAdmin` is whatever the
 * backend returned. The token is never decoded here and no role string is compared.
 */
@Injectable()
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorage);

  // Seeded from storage so a page refresh does not sign the user out.
  private readonly sessionState = signal<AuthSession | null>(this.tokenStorage.read());

  readonly session = this.sessionState.asReadonly();

  readonly isAuthenticated = computed(() => this.sessionState() !== null);

  readonly isAdmin = computed(() => this.sessionState()?.isAdmin ?? false);

  readonly fullName = computed(() => this.sessionState()?.fullName ?? '');

  login(request: LoginRequest): Observable<AuthSession> {
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(
        // Fields are picked one by one, so new backend fields cannot reach storage unnoticed.
        map(response => ({
          token: response.token,
          fullName: response.fullName,
          isAdmin: response.isAdmin
        })),
        tap(session => {
          this.tokenStorage.save(session);
          this.sessionState.set(session);
        })
      );
  }

  /** No server call: the token is stateless, so signing out means forgetting it locally. */
  logout(): void {
    this.tokenStorage.clear();
    this.sessionState.set(null);
  }
}
