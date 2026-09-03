import { DragDropModule } from '@angular/cdk/drag-drop';
import { DatePipe } from '@angular/common';
import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';

import { BoardActions } from './board-actions';
import { BoardBacklog } from './board-backlog/board-backlog';
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
/**
 * One board, in two views: the kanban and the backlog. Both are loaded by both shells at their own
 * URL, and both use the same header, the same dialogs and the same actions.
 *
 * The board's state lives in CoreModule, so opening the page twice does not create two copies of
 * it. BoardActions is provided here instead: it is this feature's own behaviour, it holds no state,
 * and nothing outside these two pages has any use for it.
 *
 * DragDropModule comes from the CDK the project already depends on — no new package was added.
 * RouterModule arrives through the routing module, which re-exports it, so the tab strip's links
 * work without importing it again.
 */
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
    // The board summary draws the same tiles the Dashboard does.
    MetricCardModule,
    // The Backlog and the sprint detail page draw the same task table.
    TaskTableModule,
    BoardDetailRoutingModule
  ],
  providers: [BoardActions]
})
export class BoardDetailModule { }
