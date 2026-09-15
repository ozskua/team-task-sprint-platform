import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthSession, LoginRequest, LoginResponse } from '../models/auth-models';
import { TokenStorage } from './token-storage';

@Injectable()
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorage);

  private readonly sessionState = signal<AuthSession | null>(this.tokenStorage.read());

  readonly session = this.sessionState.asReadonly();

  readonly isAuthenticated = computed(() => this.sessionState() !== null);

  readonly isAdmin = computed(() => this.sessionState()?.isAdmin ?? false);

  readonly fullName = computed(() => this.sessionState()?.fullName ?? '');

  readonly userId = computed(() => this.sessionState()?.userId);

  login(request: LoginRequest): Observable<AuthSession> {
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(
        map(response => ({
          userId: response.userId,
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

  logout(): void {
    this.tokenStorage.clear();
    this.sessionState.set(null);
  }
}
