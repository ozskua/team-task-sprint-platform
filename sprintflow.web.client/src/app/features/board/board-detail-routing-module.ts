import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { BoardBacklog } from './board-backlog/board-backlog';
import { BoardDetail } from './board-detail/board-detail';
import { BoardSummary } from './board-summary/board-summary';
import { SprintDetail } from './sprint-detail/sprint-detail';
import { SprintList } from './sprint-list/sprint-list';

/**
 * The board id is already in the URL by the time this module loads — each shell mounts it under
 * its own prefix, so the paths here are relative to it:
 *
 *   /boards/:boardId              ·  /boards/:boardId/backlog
 *   /admin/boards/:boardId        ·  /admin/boards/:boardId/backlog
 *
 * The named path is listed first and the empty one is `pathMatch: 'full'`, so neither can swallow
 * the other whichever prefix they are mounted under.
 */
const routes: Routes = [
  // More specific first, so 'sprints/5' is never swallowed by 'sprints'.
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
