import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { NgModule } from '@angular/core';

import { TaskTable } from './task-table';

@NgModule({
  declarations: [
    TaskTable
  ],
  imports: [
    DatePipe,
    NgTemplateOutlet
  ],
  exports: [
    TaskTable
  ]
})
export class TaskTableModule { }
