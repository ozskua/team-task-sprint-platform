import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { BoardBacklog } from './board-backlog/board-backlog';
import { BoardDetail } from './board-detail/board-detail';
import { BoardSummary } from './board-summary/board-summary';
import { SprintDetail } from './sprint-detail/sprint-detail';
import { SprintList } from './sprint-list/sprint-list';

const routes: Routes = [
  { path: 'sprints/:sprintId', component: SprintDetail },
  { path: 'sprints', component: SprintList },
  { path: 'backlog', component: BoardBacklog },
  { path: 'summary', component: BoardSummary },
  { path: '', pathMatch: 'full', component: BoardDetail }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class BoardDetailRoutingModule { }
