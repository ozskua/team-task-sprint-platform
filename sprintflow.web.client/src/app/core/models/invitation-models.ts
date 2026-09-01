/**
 * A freshly created invitation. The token is readable exactly once — only its digest is stored — so
 * losing this response means creating another invitation.
 */
export interface Invitation {
  readonly token: string;

  /** UTC instant, as an ISO string. */
  readonly expiresAt: string;
}

/**
 * What someone holding an invitation submits. There is deliberately no role, id or active flag: the
 * server owns all three, and a new account is always a Member.
 */
export interface RegisterRequest {
  readonly token: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly password: string;
}

/**
 * Confirmation of the account that was created. Not a session — registering does not sign anyone
 * in, so there is no token here and nothing to store.
 */
export interface RegisterResponse {
  readonly fullName: string;
  readonly email: string;
}
