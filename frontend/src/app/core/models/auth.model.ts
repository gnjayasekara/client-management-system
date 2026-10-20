
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  username: string;
  roles?: string[];
  permissions?: string[];
  refreshToken?: string;
}

export interface RefreshTokenRequest {
  userId: string;
  refreshToken: string;
}

export interface RegisterUserRequest {
  username: string;
  email: string;
  password: string;
  role: string;
}


