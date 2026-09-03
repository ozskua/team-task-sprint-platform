export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface LoginResponse {
  /** Who is signed in. Lets the client recognise its own rows; it is never a permission. */
  readonly userId: number;

  readonly token: string;
  readonly fullName: string;

  /** Decided by the server from the authenticated UserId. The client applies it, never re-derives it. */
  readonly isAdmin: boolean;
}

/**
 * What the client persists. Same shape as LoginResponse today, but kept separate so a new field on
 * the response cannot end up in browser storage by accident.
 *
 * `userId` is optional because a session saved before it existed does not carry one. Anything that
 * reads it must cope with `undefined` rather than assume a signed-in user knows their own id.
 */
export interface AuthSession {
  readonly userId?: number;
  readonly token: string;
  readonly fullName: string;
  readonly isAdmin: boolean;
}
