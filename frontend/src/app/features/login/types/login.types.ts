export type LoginBannerTone = 'error' | 'warn' | 'info' | '';

export type LoginError = {
  message: string;
  tone: LoginBannerTone;
};
