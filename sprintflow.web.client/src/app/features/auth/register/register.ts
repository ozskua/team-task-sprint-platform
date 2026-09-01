import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { InvitationService } from '../../../core/services/invitation.service';

/** Matches the server's rule exactly. Eight characters, and nothing about composition. */
const MINIMUM_PASSWORD_LENGTH = 8;

const UNEXPECTED_ERROR_MESSAGE = 'Something went wrong. Please try again.';

const MISSING_TOKEN_MESSAGE =
  'This page needs an invitation link. Ask a colleague to send you one.';

/**
 * Creating an account from an invitation.
 *
 * Reachable only with a token in the query string — there is no link to it from the login page, and
 * without a usable token the form is never shown. That is what keeps account creation something an
 * existing user starts rather than something the public can do.
 *
 * Registering does not sign anyone in. On success this navigates to `/login`, so a session is still
 * only ever established in one place.
 */
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

  /** Null while the invitation is still being checked, which is what the page shows first. */
  protected readonly invitationUsable = signal<boolean | null>(null);

  /** Why the invitation cannot be used. Comes from the server, which explains the specific cause. */
  protected readonly invitationError = signal<string | null>(null);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    password: ['', [Validators.required, Validators.minLength(MINIMUM_PASSWORD_LENGTH)]],
    // The match is checked on this control rather than on the group: Material only renders a
    // mat-error when the control itself is invalid, so a group-level error would block the submit
    // while showing the user nothing at all.
    confirmPassword: ['', [Validators.required, matchesPassword]]
  });

  ngOnInit(): void {
    // Editing the first password has to re-judge the second, which Angular does not do on its own —
    // otherwise correcting a typo in the first field would leave a stale "do not match" underneath.
    this.form.controls.password.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.form.controls.confirmPassword.updateValueAndValidity());

    // Read once. Nothing navigates within this route, so the token cannot change underneath us.
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
    // Saves a round trip only; the API validates every one of these again.
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

          // No session is created here. The new account signs in through the ordinary login page,
          // which is the only place a token is ever issued or stored.
          void this.router.navigate(['/login'], { queryParams: { registered: 1 } });
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(this.explain(error));
        }
      });
  }

  /**
   * The server's own wording for a rejected registration.
   *
   * Unlike the login page, which replaces the server's message with a fixed string, these are worth
   * showing: "this invitation has expired" or "that address is already registered" tell the reader
   * what to do next, and neither reveals anything a holder of the invitation could not already find
   * out. Anything unrecognised still falls back to a generic message.
   */
  private explain(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 400) {
      const detail = error.error?.detail;

      if (typeof detail === 'string' && detail.length > 0) {
        return detail;
      }

      // Field-level failures arrive as a validation dictionary rather than a detail line.
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

/**
 * The confirmation must equal the password.
 *
 * Written as a validator on the confirmation control, reading its sibling through the parent, so
 * the error lands where Angular Material looks for it. Client-side only: the server is sent one
 * password and has nothing to compare. It earns its place because an invitation is single-use — a
 * mistyped password would cost the whole link, not just a retry.
 */
function matchesPassword(control: AbstractControl): ValidationErrors | null {
  const group = control.parent;

  // Nothing to compare before the control is attached, or while it is still empty — `required`
  // already covers empty.
  if (!group || !control.value) {
    return null;
  }

  return group.get('password')?.value === control.value ? null : { passwordsDiffer: true };
}
