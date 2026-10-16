export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
};

export type LoginRequestBody = {
  email: string;
  password: string;
};

export type ChangePasswordRequest = {
  currentPassword?: string;
  newPassword: string;
};

export type Role = 'Admin' | 'Doctor' | 'Staff';

export type ApiError = {
  status: number;
  message: string;
  traceId: string | null;
};
