import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';

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
