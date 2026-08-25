import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';

// Aliased: the model and this component are both called BoardSummary.
import { BoardSummary as BoardSummaryModel } from '../../../core/models/board-summary-models';
import { BoardSummaryService } from '../../../core/services/board-summary.service';

/**
 * One board's Özet: how much work it holds and how far along it is.
 *
 * Only this board. The Dashboard answers the same kind of question about the whole system and is
 * untouched by this page — the two read different endpoints and never share figures.
 *
 * The board id is watched rather than read once, so moving between boards re-reads the summary and
 * one board's numbers can never be left on screen under another board's name.
 *
 * There are no overdue or upcoming figures. Nothing in the schema records when a task is due or when
 * it was finished, so those would be invented rather than measured.
 */
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

    // Cleared first: a slow response must not leave the previous board's figures on screen under
    // the new board's heading.
    this.summary.set(null);

    this.summaryService.get(boardId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: summary => {
          this.summary.set(summary);
          this.loading.set(false);
        },
        error: () => {
          // The message is a local constant; the server's text is never shown.
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }
}
