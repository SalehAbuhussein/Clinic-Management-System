import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { email, form, FormField, minLength, required } from '@angular/forms/signals';

import { finalize } from 'rxjs';

import { LoginService } from './services/login.service';

import { LoginBackdropComponent } from './components/login-backdrop/login-backdrop.component';
import { ThemeToggleComponent } from '@shared/components/theme-toggle/theme-toggle.component';

import { LoginBannerTone, LoginError, LoginRequestBody } from './types/login.types';

@Component({
  imports: [
    LoginBackdropComponent,
    ThemeToggleComponent,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    FormField,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  selector: 'app-login',
  styleUrl: './login.component.scss',
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnDestroy {
  loginService = inject(LoginService);

  readonly TONE_BY_STATUS: Record<number, LoginBannerTone> = {
    400: "error",
    401: "error",
    429: "warn",
    200: "info"
  };

  loading = signal(false);
  hidePassword = signal(true);

  error = signal<LoginError>({
    message: "",
    tone: "error",
  });
  errorIcon = computed(() => {
    switch (this.error().tone) {
      case 'warn':
        return 'timer';
      case 'info':
        return 'cloud_off';
      default:
        return 'error';
    }
  });

  loginModel = signal<LoginRequestBody>({
    email: '',
    password: ''
  });
  loginForm = form(this.loginModel, (schemaPath) => {
    required(schemaPath.email);
    email(schemaPath.email);
    required(schemaPath.password);
    minLength(schemaPath.password, 6);
  });

  /**
   * Submit Login form
   */
  onSubmit(event: Event): void {
    event.preventDefault();

    if (this.loginForm().invalid()) {
      this.loginForm().markAsTouched();
      return;
    }

    this.error.set({
      message: '',
      tone: '',
    });
    this.loading.set(true);

    this.loginService.postLogin(this.loginForm().value()).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: response => {
        console.log(response);
      },
      error: error => {
        error = error.error;

        this.error.set({
          tone: this.TONE_BY_STATUS[error.status],
          message: error.detail
        });
      }
    });
  }

  /**
   * Angular onDestroy lifecycle method
   */
  ngOnDestroy(): void {
    this.error.set({
      message: '',
      tone: '',
    });
  }
}
