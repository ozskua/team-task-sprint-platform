import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { USER_NAVIGATION } from '../navigation';

@Component({
  selector: 'app-user-layout',
  templateUrl: './user-layout.html',
  standalone: false,
  styleUrl: './user-layout.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserLayout {
  private readonly router = inject(Router);
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly authService = inject(AuthService);

  protected readonly navigation = USER_NAVIGATION;

  protected readonly compact = toSignal(
    this.breakpoints.observe('(max-width: 1023.98px)').pipe(map(state => state.matches)),
    { initialValue: false }
  );

  protected readonly sidebarOpen = signal(true);
  protected readonly pageTitle = signal('');

  protected readonly fullBleed = signal(false);

  constructor() {
    effect(() => this.sidebarOpen.set(!this.compact()));

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        startWith(null),
        map(() => this.resolveRouteData()),
        takeUntilDestroyed()
      )
      .subscribe(data => {
        this.pageTitle.set((data['title'] as string | undefined) ?? '');
        this.fullBleed.set(data['fullBleed'] === true);
      });
  }

  protected toggleSidebar(): void {
    this.sidebarOpen.update(open => !open);
  }

  protected onLogout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  private resolveRouteData(): Record<string, unknown> {
    let route = this.router.routerState.snapshot.root;

    while (route.firstChild) {
      route = route.firstChild;
    }

    return route.data;
  }
}
