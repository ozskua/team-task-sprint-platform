import { NgTemplateOutlet } from '@angular/common';
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';

import { MetricCard } from './metric-card';

/**
 * Declares and exports the metric card, and nothing else.
 *
 * A component can only be declared in one NgModule, and two pages now draw these tiles — the
 * Dashboard's system-wide summary and each board's own. One focused module is what lets them share
 * the definition; the card itself is unchanged.
 *
 * Deliberately not a general SharedModule: it carries exactly one component.
 */
@NgModule({
  declarations: [
    MetricCard
  ],
  imports: [
    // MetricCard renders its body through an ng-template so the card can be a link or a plain div.
    NgTemplateOutlet,
    // A card with a `link` renders as a routerLink.
    RouterModule
  ],
  exports: [
    MetricCard
  ]
})
export class MetricCardModule { }
