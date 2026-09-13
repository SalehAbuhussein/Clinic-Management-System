import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { LoginService } from './services/login.service';

import { LoginBackdropComponent } from './components/login-backdrop/login-backdrop.component';

import { BannerTone } from './types/login.types';

@Component({
  imports: [
    LoginBackdropComponent,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  selector: 'app-login',
  styleUrl: './login.component.scss',
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnDestroy {
  formBuilder = inject(FormBuilder);
  loginService = inject(LoginService);

  loading = signal(false);
  hidePassword = signal(true);

  errorMessage = signal<string>('');
  errorTone = signal<BannerTone>('error');
  rateLimited = signal(false);

  errorIcon = computed(() => {
    switch (this.errorTone()) {
      case 'warn':
        return 'timer';
      case 'info':
        return 'cloud_off';
      default:
        return 'error';
    }
  });

  readonly submitLabel = computed(() => (this.errorTone() === 'info' ? 'Retry' : 'Sign in'));

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  /**
   * Submit Login form
   */
  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.clearError();
    this.loading.set(true);

    // TODO: subscribe to this.loginService.postLogin(this.form.getRawValue()).
    //       next  -> store the tokens and the email (refresh needs both), then navigate.
    //       error -> this.showError(err); finally -> this.loading.set(false).
  }

  /**
   *
   */
  clearError(): void {
    this.errorMessage.set('');
    this.rateLimited.set(false);
    this.errorTone.set('error');
  }

  /**
   * Angular onDestroy lifecycle method
   */
  ngOnDestroy(): void {
    this.clearError();
  }
}
