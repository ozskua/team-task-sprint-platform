export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface LoginResponse {
  readonly token: string;
  readonly fullName: string;

  /** Decided by the server from the authenticated UserId. The client applies it, never re-derives it. */
  readonly isAdmin: boolean;
}

/**
 * What the client persists. Same shape as LoginResponse today, but kept separate so a new field on
 * the response cannot end up in browser storage by accident.
 */
export interface AuthSession {
  readonly token: string;
  readonly fullName: string;
  readonly isAdmin: boolean;
}
