import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs';

import { ADMIN_NAVIGATION } from '../navigation';

@Component({
  selector: 'app-admin-layout',
  templateUrl: './admin-layout.html',
  standalone: false,
  styleUrl: './admin-layout.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminLayout {
  private readonly router = inject(Router);
  private readonly breakpoints = inject(BreakpointObserver);

  protected readonly navigation = ADMIN_NAVIGATION;

  /** Below 1024px the sidebar overlays the content instead of sitting beside it. */
  protected readonly compact = toSignal(
    this.breakpoints.observe('(max-width: 1023.98px)').pipe(map(state => state.matches)),
    { initialValue: false }
  );

  protected readonly sidebarOpen = signal(true);
  protected readonly pageTitle = signal('');

  constructor() {
    // Open beside the content on desktop, closed over it on narrower screens.
    effect(() => this.sidebarOpen.set(!this.compact()));

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        startWith(null),
        map(() => this.resolveTitle()),
        takeUntilDestroyed()
      )
      .subscribe(title => this.pageTitle.set(title));
  }

  protected toggleSidebar(): void {
    this.sidebarOpen.update(open => !open);
  }

  protected onLogout(): void {
    void this.router.navigate(['/login']);
  }

  /** The header title is the deepest activated route's `title` data. */
  private resolveTitle(): string {
    let route = this.router.routerState.snapshot.root;

    while (route.firstChild) {
      route = route.firstChild;
    }

    return route.data['title'] ?? '';
  }
}
