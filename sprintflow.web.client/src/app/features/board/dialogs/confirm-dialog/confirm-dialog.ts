import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

export interface ConfirmDialogData {
  readonly title: string;
  readonly message: string;

  readonly consequence?: string;
  readonly confirmLabel: string;

  readonly acknowledgeOnly?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  templateUrl: './confirm-dialog.html',
  standalone: false,
  styleUrl: './confirm-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
}
