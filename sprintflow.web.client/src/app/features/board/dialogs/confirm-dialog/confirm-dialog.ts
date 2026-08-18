import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

/** What the dialog should say. The caller owns the wording, because only it knows the consequence. */
export interface ConfirmDialogData {
  readonly title: string;
  readonly message: string;

  /** Spelled out separately so a permanent deletion never hides inside a sentence. */
  readonly consequence?: string;
  readonly confirmLabel: string;

  /**
   * True when there is nothing to agree to — the dialog is explaining why something cannot happen,
   * so it offers one way out instead of a choice between doing it and not.
   */
  readonly acknowledgeOnly?: boolean;
}

/**
 * A yes/no gate in front of something that cannot be undone. Closes with `true` only when the
 * confirming button is pressed; every other way out means no.
 */
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
