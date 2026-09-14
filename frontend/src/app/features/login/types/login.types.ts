export type LoginRequestBody = {
  email: string;
  password: string;
};

export type LoginBannerTone = 'error' | 'warn' | 'info' | '';

export type LoginError = {
  message: string;
  tone: LoginBannerTone;
};
