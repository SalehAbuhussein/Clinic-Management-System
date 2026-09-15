export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
};

export type LoginRequestBody = {
  email: string;
  password: string;
};

export type ApiError = {
  status: number;
  message: string;
  traceId: string | null;
};
