import { effect, Service, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

@Service()
export class ThemeService {
  theme = signal<Theme>(this.readTheme());

  constructor() {
    effect(() => {
      document.body.style.colorScheme = this.theme();
      localStorage.setItem('clinic-theme', this.theme());
    });
  }

  /**
   * Switch between the light and dark scheme
   */
  toggle = (): void => {
    this.theme.update((theme) => (theme === 'dark' ? 'light' : 'dark'));
  }

  /**
   * Read the stored theme, falling back to the device preference
   */
  readTheme(): Theme {
    const stored = localStorage.getItem('clinic-theme');

    if (stored === 'light' || stored === 'dark') {
      return stored;
    }

    return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
