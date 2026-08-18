import { DragDropModule } from '@angular/cdk/drag-drop';
import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

import { BoardDetailRoutingModule } from './board-detail-routing-module';
import { BoardDetail } from './board-detail/board-detail';
import { BoardHeader } from './board-header/board-header';
import { ColumnFormDialog } from './dialogs/column-form-dialog/column-form-dialog';
import { ConfirmDialog } from './dialogs/confirm-dialog/confirm-dialog';
import { TaskDetailDialog } from './dialogs/task-detail-dialog/task-detail-dialog';
import { TaskFormDialog } from './dialogs/task-form-dialog/task-form-dialog';
import { TaskCard } from './task-card/task-card';
import { WorkflowColumnComponent } from './workflow-column/workflow-column';

/**
 * The board page, loaded by both shells at their own URL. Nothing here is provided at the module
 * level: the board's state lives in CoreModule, so opening the page twice does not create two
 * copies of it.
 *
 * DragDropModule comes from the CDK the project already depends on — no new package was added.
 */
@NgModule({
  declarations: [
    BoardDetail,
    BoardHeader,
    WorkflowColumnComponent,
    TaskCard,
    TaskFormDialog,
    TaskDetailDialog,
    ColumnFormDialog,
    ConfirmDialog
  ],
  imports: [
    DatePipe,
    DragDropModule,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatMenuModule,
    BoardDetailRoutingModule
  ]
})
export class BoardDetailModule { }
