import { NgModule } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { TaskTableModule } from '../../shared/task-table/task-table-module';
import { AllSprints } from './all-sprints/all-sprints';
import { SprintsRoutingModule } from './sprints-routing-module';

@NgModule({
  declarations: [
    AllSprints
  ],
  imports: [
    MatIconModule,
    TaskTableModule,
    SprintsRoutingModule
  ]
})
export class SprintsModule { }
