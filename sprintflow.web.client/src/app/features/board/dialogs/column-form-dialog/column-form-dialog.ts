import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface ColumnFormDialogData {
  readonly heading: string;
  readonly confirmLabel: string;

  /** Present when renaming, absent when creating. */
  readonly name?: string;
}

/**
 * Names a custom column. Only ever opened for custom columns — the three system columns keep
 * their names on every board, so nothing offers this for them.
 */
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

  /** Matches the server's column-name limit, so the field cannot produce a request it will reject. */
  protected readonly maxLength = 100;

  protected onNameInput(value: string): void {
    this.name.set(value);
  }

  protected submit(): void {
    const trimmed = this.name().trim();

    // Whitespace alone is not a name. The server rejects it too; stopping here saves a round trip.
    if (trimmed.length === 0) {
      return;
    }

    this.dialogRef.close(trimmed);
  }
}
