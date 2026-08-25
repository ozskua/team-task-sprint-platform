import { NgModule } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { TaskTableModule } from '../../shared/task-table/task-table-module';
import { AllSprints } from './all-sprints/all-sprints';
import { SprintsRoutingModule } from './sprints-routing-module';

/**
 * The system-wide sprint overview, loaded by both shells at their own URL.
 *
 * RouterModule arrives through the routing module, which re-exports it, so the rows' links work
 * without importing it again.
 */
@NgModule({
  declarations: [
    AllSprints
  ],
  imports: [
    // The disclosure chevron on each sprint row.
    MatIconModule,
    // The same task table the Backlog and the sprint detail page draw.
    TaskTableModule,
    SprintsRoutingModule
  ]
})
export class SprintsModule { }
