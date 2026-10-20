
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  username: string;
  roles: string[];
  permissions: string[];
  refreshToken: string;
  mustChangePassword: boolean;
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

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ConfirmEmailRequest {
  userId: string;
  token: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  userId: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ApiMessage {
  message: string;
  errors?: string[];
}

