export interface Invitation {
  readonly token: string;

  readonly expiresAt: string;
}

export interface RegisterRequest {
  readonly token: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly password: string;
}

export interface RegisterResponse {
  readonly fullName: string;
  readonly email: string;
}
