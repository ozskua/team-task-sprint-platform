import { ChangeDetectionStrategy, Component, output } from '@angular/core';

@Component({
  selector: 'app-user-menu',
  templateUrl: './user-menu.html',
  standalone: false,
  styleUrl: './user-menu.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserMenu {
  readonly logout = output<void>();
}
