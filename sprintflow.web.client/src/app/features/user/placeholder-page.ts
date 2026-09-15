import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-placeholder-page',
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="placeholder">
      <p class="placeholder-title">This section is not available yet</p>
      <p class="placeholder-hint">It will be added in a later phase.</p>
    </div>
  `,
  styles: `
    .placeholder {
      padding: 64px 24px;
      border: 1px solid var(--app-border-card);
      border-radius: var(--app-radius-card);
      background-color: var(--app-surface-card);
      box-shadow: var(--app-shadow-card);
      text-align: center;
    }

    .placeholder-title {
      margin: 0 0 4px;
      font-size: 0.9375rem;
      font-weight: 600;
      color: var(--mat-sys-on-surface);
    }

    .placeholder-hint {
      margin: 0;
      font-size: 0.875rem;
      color: var(--mat-sys-on-surface-variant);
    }
  `
})
export class PlaceholderPage { }
