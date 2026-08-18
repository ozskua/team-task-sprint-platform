import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { USER_NAVIGATION } from '../navigation';

/**
 * Shell for the normal user area. Same structure and behaviour as AdminLayout —
 * the only difference is the navigation list it hands to the shared Sidebar.
 */
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

  /** Below 1024px the sidebar overlays the content instead of sitting beside it. */
  protected readonly compact = toSignal(
    this.breakpoints.observe('(max-width: 1023.98px)').pipe(map(state => state.matches)),
    { initialValue: false }
  );

  protected readonly sidebarOpen = signal(true);
  protected readonly pageTitle = signal('');

  /**
   * Whether the current page wants the whole content area rather than the centred reading column.
   * Declared by the route itself, the same way the header title is, so the shell never has to
   * recognise particular URLs.
   */
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
    this.authService.logout(); // Clear first, so a browser "back" is rejected by authGuard.
    void this.router.navigate(['/login']);
  }

  /** The deepest activated route's data — where both the header title and the layout mode live. */
  private resolveRouteData(): Record<string, unknown> {
    let route = this.router.routerState.snapshot.root;

    while (route.firstChild) {
      route = route.firstChild;
    }

    return route.data;
  }
}
