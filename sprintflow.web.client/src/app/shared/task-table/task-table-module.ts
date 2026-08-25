import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { NgModule } from '@angular/core';

import { TaskTable } from './task-table';

/**
 * Declares and exports the task table, and nothing else.
 *
 * A component can only be declared in one NgModule, and three features draw this table — the
 * Backlog, the sprint detail page and the system-wide sprint list. One focused module is what lets
 * them share the definition instead of keeping three copies of the markup.
 *
 * Deliberately not a general SharedModule: it carries exactly one component, so there is nothing
 * here to become a dumping ground.
 */
@NgModule({
  declarations: [
    TaskTable
  ],
  imports: [
    // The Created column.
    DatePipe,
    // The caller's extra header and body cells are rendered through ngTemplateOutlet.
    NgTemplateOutlet
  ],
  exports: [
    TaskTable
  ]
})
export class TaskTableModule { }
