import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

/**
 * What the form collects, in the server's own terms: the interval is `[startDate, endDate)`, so the
 * start day is part of the sprint and the end day is not.
 */
export interface SprintFormResult {
  readonly name: string;
  readonly startDate: string;
  readonly endDate: string;
}

/**
 * Names a sprint and picks the days it covers.
 *
 * Both fields are native date inputs, which is what keeps this whole-days-only: the control has no
 * hour or minute to offer, and it hands back a plain `YYYY-MM-DD` string rather than a `Date`, so
 * no timezone is ever applied. What is typed is what is stored — there is no conversion.
 *
 * Angular Material's datepicker is deliberately not used: it would pull in a date adapter that
 * works in `Date` objects and local time.
 */
@Component({
  selector: 'app-sprint-form-dialog',
  templateUrl: './sprint-form-dialog.html',
  standalone: false,
  styleUrl: './sprint-form-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SprintFormDialog {
  private readonly dialogRef = inject<MatDialogRef<SprintFormDialog, SprintFormResult>>(MatDialogRef);

  protected readonly name = signal('');
  protected readonly startDate = signal('');

  /** The day the sprint stops. Excluded from it, and free for the next sprint to start on. */
  protected readonly endDate = signal('');

  /** Matches the server's column width, so the field cannot produce a request it will reject. */
  protected readonly maxLength = 200;

  /**
   * The end must be strictly after the start. Equal dates describe a sprint covering no days at
   * all, which the database refuses through CK_Sprints_EndAfterStart — catching it here saves the
   * round trip and says so in the form.
   */
  protected readonly datesOutOfOrder = computed(() => {
    const start = this.startDate();
    const end = this.endDate();

    // Plain string comparison is correct for YYYY-MM-DD: the format sorts the same way the calendar
    // does, so there is nothing to parse and no Date object to shift a day.
    return start.length > 0 && end.length > 0 && end <= start;
  });

  protected readonly canSubmit = computed(() =>
    this.name().trim().length > 0
    && this.startDate().length > 0
    && this.endDate().length > 0
    && !this.datesOutOfOrder());

  protected onNameInput(value: string): void {
    this.name.set(value);
  }

  protected onStartInput(value: string): void {
    this.startDate.set(value);
  }

  protected onEndInput(value: string): void {
    this.endDate.set(value);
  }

  protected submit(): void {
    if (!this.canSubmit()) {
      return;
    }

    this.dialogRef.close({
      name: this.name().trim(),
      startDate: this.startDate(),
      endDate: this.endDate()
    });
  }
}
