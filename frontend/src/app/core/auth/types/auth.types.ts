export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
};

export type LoginRequestBody = {
  email: string;
  password: string;
};
