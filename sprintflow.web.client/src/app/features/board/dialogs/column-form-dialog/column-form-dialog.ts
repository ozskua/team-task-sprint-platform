import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface ColumnFormDialogData {
  readonly heading: string;
  readonly confirmLabel: string;

  readonly name?: string;
}

@Component({
  selector: 'app-column-form-dialog',
  templateUrl: './column-form-dialog.html',
  standalone: false,
  styleUrl: './column-form-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColumnFormDialog {
  protected readonly data = inject<ColumnFormDialogData>(MAT_DIALOG_DATA);

  private readonly dialogRef = inject<MatDialogRef<ColumnFormDialog, string>>(MatDialogRef);

  protected readonly name = signal(this.data.name ?? '');

  protected readonly maxLength = 100;

  protected onNameInput(value: string): void {
    this.name.set(value);
  }

  protected submit(): void {
    const trimmed = this.name().trim();

    if (trimmed.length === 0) {
      return;
    }

    this.dialogRef.close(trimmed);
  }
}
