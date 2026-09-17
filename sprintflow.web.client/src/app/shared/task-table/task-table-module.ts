import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { NgModule } from '@angular/core';

import { AvatarModule } from '../avatar/avatar-module';
import { TaskTable } from './task-table';

@NgModule({
  declarations: [
    TaskTable
  ],
  imports: [
    DatePipe,
    NgTemplateOutlet,
    AvatarModule
  ],
  exports: [
    TaskTable
  ]
})
export class TaskTableModule { }
