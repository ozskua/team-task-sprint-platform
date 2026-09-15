import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

export interface SprintFormResult {
  readonly name: string;
  readonly startDate: string;
  readonly endDate: string;
}

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

  protected readonly endDate = signal('');

  protected readonly maxLength = 200;

  protected readonly datesOutOfOrder = computed(() => {
    const start = this.startDate();
    const end = this.endDate();

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
