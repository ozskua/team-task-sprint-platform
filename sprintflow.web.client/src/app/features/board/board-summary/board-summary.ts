import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';

import { BoardSummary as BoardSummaryModel } from '../../../core/models/board-summary-models';
import { BoardSummaryService } from '../../../core/services/board-summary.service';

@Component({
  selector: 'app-board-summary',
  templateUrl: './board-summary.html',
  standalone: false,
  styleUrl: './board-summary.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BoardSummary {
  private readonly summaryService = inject(BoardSummaryService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly boardId = signal(0);
  protected readonly summary = signal<BoardSummaryModel | null>(null);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => this.load(Number(params.get('boardId'))));
  }

  private load(boardId: number): void {
    if (!Number.isInteger(boardId) || boardId <= 0) {
      this.loading.set(false);
      this.failed.set(true);

      return;
    }

    this.boardId.set(boardId);
    this.loading.set(true);
    this.failed.set(false);

    this.summary.set(null);

    this.summaryService.get(boardId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: summary => {
          this.summary.set(summary);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }
}
