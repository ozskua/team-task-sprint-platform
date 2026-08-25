import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';

/**
 * The two ways of looking at one board: the kanban, and the backlog list.
 *
 * The links are absolute and worked out from the current URL, because the same pair of pages is
 * mounted under `/boards/...` in one shell and `/admin/boards/...` in the other. A relative link
 * would have to mean something different depending on which of the two views is showing it, and
 * this way both views can render the identical strip in the identical place.
 */
@Component({
  selector: 'app-board-view-nav',
  templateUrl: './board-view-nav.html',
  standalone: false,
  styleUrl: './board-view-nav.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardViewNav {
  private readonly router = inject(Router);

  readonly boardId = input.required<number>();

  protected readonly links = computed(() => {
    // '' in the member shell, '/admin' in the other. The shell cannot change without this component
    // being recreated, so reading the URL once here is safe.
    const prefix = this.router.url.split('/boards/')[0];
    const base = `${prefix}/boards/${this.boardId()}`;

    return {
      summary: `${base}/summary`,
      kanban: base,
      backlog: `${base}/backlog`,
      sprints: `${base}/sprints`
    };
  });
}
