import { DragDropModule } from '@angular/cdk/drag-drop';
import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

import { BoardActions } from './board-actions';
import { BoardBacklog } from './board-backlog/board-backlog';
import { AvatarModule } from '../../shared/avatar/avatar-module';
import { MetricCardModule } from '../../shared/metric-card/metric-card-module';
import { TaskTableModule } from '../../shared/task-table/task-table-module';
import { BoardDetailRoutingModule } from './board-detail-routing-module';
import { BoardDetail } from './board-detail/board-detail';
import { BoardHeader } from './board-header/board-header';
import { BoardSummary } from './board-summary/board-summary';
import { BoardViewNav } from './board-view-nav/board-view-nav';
import { ColumnFormDialog } from './dialogs/column-form-dialog/column-form-dialog';
import { ConfirmDialog } from './dialogs/confirm-dialog/confirm-dialog';
import { TaskDetailDialog } from './dialogs/task-detail-dialog/task-detail-dialog';
import { TaskFormDialog } from './dialogs/task-form-dialog/task-form-dialog';
import { SprintFormDialog } from './dialogs/sprint-form-dialog/sprint-form-dialog';
import { OutcomeLane } from './outcome-lane/outcome-lane';
import { SprintDetail } from './sprint-detail/sprint-detail';
import { SprintList } from './sprint-list/sprint-list';
import { TaskCard } from './task-card/task-card';
import { TaskComments } from './task-comments/task-comments';
import { WorkflowColumnComponent } from './workflow-column/workflow-column';
@NgModule({
  declarations: [
    BoardDetail,
    BoardBacklog,
    BoardHeader,
    BoardSummary,
    BoardViewNav,
    SprintList,
    SprintDetail,
    WorkflowColumnComponent,
    OutcomeLane,
    TaskCard,
    TaskComments,
    TaskFormDialog,
    TaskDetailDialog,
    ColumnFormDialog,
    SprintFormDialog,
    ConfirmDialog
  ],
  imports: [
    DatePipe,
    DragDropModule,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatMenuModule,
    AvatarModule,
    MetricCardModule,
    TaskTableModule,
    BoardDetailRoutingModule
  ],
  providers: [BoardActions]
})
export class BoardDetailModule { }
