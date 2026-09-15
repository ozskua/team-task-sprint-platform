export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface LoginResponse {
  readonly userId: number;

  readonly token: string;
  readonly fullName: string;

  readonly isAdmin: boolean;
}

export interface AuthSession {
  readonly userId?: number;
  readonly token: string;
  readonly fullName: string;
  readonly isAdmin: boolean;
}
