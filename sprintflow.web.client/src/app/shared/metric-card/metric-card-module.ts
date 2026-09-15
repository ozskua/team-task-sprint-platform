import { NgTemplateOutlet } from '@angular/common';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';

import { MetricCard } from './metric-card';

@NgModule({
  declarations: [
    MetricCard
  ],
  imports: [
    NgTemplateOutlet,
    RouterModule
  ],
  exports: [
    MetricCard
  ]
})
export class MetricCardModule { }
