import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';

// Fixed message: it must not reveal which field was wrong.
const INVALID_CREDENTIALS_MESSAGE = 'Invalid e-mail or password.';

const UNEXPECTED_ERROR_MESSAGE = 'Something went wrong. Please try again.';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  standalone: false,
  styleUrl: './login.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Login {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  protected onSubmit(): void {
    // Client-side validation only saves a round trip; the API validates the same fields again.
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.loading.set(true);

    const { email, password } = this.form.getRawValue();

    this.authService
      .login({ email: email ?? '', password: password ?? '' })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: session => {
          this.loading.set(false);

          // The backend decided which dashboard; this only applies the answer.
          void this.router.navigate([session.isAdmin ? '/admin/dashboard' : '/dashboard']);
        },
        error: (error: unknown) => {
          this.loading.set(false);

          const rejected = error instanceof HttpErrorResponse && error.status === 401;

          // The server's own message is not echoed back to the user.
          this.errorMessage.set(rejected ? INVALID_CREDENTIALS_MESSAGE : UNEXPECTED_ERROR_MESSAGE);
        }
      });
  }
}
