import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

import { Area } from '../../core/models/area-models';

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
