import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

import { Area } from '../../core/models/area-models';

/**
 * Read-only details of one Area. Every value shown comes from the API response, including the
 * creator's name — it is never taken from the signed-in session, so an Area someone else created
 * shows their name and not the viewer's.
 */
@Component({
  selector: 'app-area-info-dialog',
  templateUrl: './area-info-dialog.html',
  standalone: false,
  styleUrl: './area-info-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AreaInfoDialog {
  protected readonly area = inject<Area>(MAT_DIALOG_DATA);
}
