import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Decorative background for the sign-in screen: tonal light, a gauze of
 * medical crosses, and an ECG trace that draws itself once on load.
 *
 * Purely presentational and `aria-hidden` — it carries no meaning, takes no
 * input, and is specific to the login screen rather than shared.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'aria-hidden': 'true',
  },
  imports: [],
  selector: 'app-login-backdrop',
  styleUrl: './login-backdrop.component.scss',
  templateUrl: './login-backdrop.component.html',
})
export class LoginBackdropComponent {}
