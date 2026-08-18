import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { BoardDetail } from './board-detail/board-detail';

/**
 * The board id is already in the URL by the time this module loads — each shell mounts it under
 * its own prefix, so the path here is empty on purpose.
 */
const routes: Routes = [
  { path: '', component: BoardDetail }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class BoardDetailRoutingModule { }
