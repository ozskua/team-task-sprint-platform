import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { InvitationService } from '../../../core/services/invitation.service';

const MINIMUM_PASSWORD_LENGTH = 8;

const UNEXPECTED_ERROR_MESSAGE = 'Something went wrong. Please try again.';

const MISSING_TOKEN_MESSAGE =
  'This page needs an invitation link. Ask a colleague to send you one.';

@Component({
  selector: 'app-register',
  templateUrl: './register.html',
  standalone: false,
  styleUrl: './register.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Register implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly invitationService = inject(InvitationService);
  private readonly destroyRef = inject(DestroyRef);

  private token = '';

  protected readonly invitationUsable = signal<boolean | null>(null);

  protected readonly invitationError = signal<string | null>(null);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.minLength(MINIMUM_PASSWORD_LENGTH)]],
    confirmPassword: ['', [Validators.required, matchesPassword]]
  });

  ngOnInit(): void {
    this.form.controls.password.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.form.controls.confirmPassword.updateValueAndValidity());

    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.token) {
      this.invitationUsable.set(false);
      this.invitationError.set(MISSING_TOKEN_MESSAGE);
      return;
    }

    this.invitationService
      .checkToken(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.invitationUsable.set(true),
        error: (error: unknown) => {
          this.invitationUsable.set(false);
          this.invitationError.set(this.explain(error));
        }
      });
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);

    const { firstName, lastName, email, password } = this.form.getRawValue();

    this.invitationService
      .register({
        token: this.token,
        firstName: firstName ?? '',
        lastName: lastName ?? '',
        email: email ?? '',
        password: password ?? ''
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);

          void this.router.navigate(['/login'], { queryParams: { registered: 1 } });
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(this.explain(error));
        }
      });
  }

  private explain(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 400) {
      const detail = error.error?.detail;

      if (typeof detail === 'string' && detail.length > 0) {
        return detail;
      }

      const errors = error.error?.errors;

      if (errors && typeof errors === 'object') {
        const first = Object.values(errors as Record<string, string[]>)[0];

        if (Array.isArray(first) && first.length > 0) {
          return first[0];
        }
      }
    }

    return UNEXPECTED_ERROR_MESSAGE;
  }
}

function matchesPassword(control: AbstractControl): ValidationErrors | null {
  const group = control.parent;

  if (!group || !control.value) {
    return null;
  }

  return group.get('password')?.value === control.value ? null : { passwordsDiffer: true };
}
