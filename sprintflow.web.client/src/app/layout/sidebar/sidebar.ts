import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { NavItem } from '../navigation';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.html',
  standalone: false,
  styleUrl: './sidebar.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Sidebar {
  readonly items = input.required<readonly NavItem[]>();
}
