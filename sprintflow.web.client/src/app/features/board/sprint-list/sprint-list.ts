import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';

import { Sprint } from '../../../core/models/sprint-models';
import { SprintService } from '../../../core/services/sprint.service';
import { SprintFormDialog, SprintFormResult } from '../dialogs/sprint-form-dialog/sprint-form-dialog';

/**
 * One board's sprints: what they are called, when they run, and how they stand.
 *
 * Sprints on a board form a single timeline, so this is a sequence rather than a set — it is
 * ordered by date and never by name or by when it was created.
 *
 * Opening one from here scopes the kanban to it; the sprint's own page is a click further on.
 */
@Component({
  selector: 'app-sprint-list',
  templateUrl: './sprint-list.html',
  standalone: false,
  styleUrl: './sprint-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SprintList {
  private readonly sprintService = inject(SprintService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly boardId = signal(0);
  protected readonly sprints = signal<readonly Sprint[]>([]);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);

  /** A local, generic message. The server's own wording is never shown. */
  protected readonly actionError = signal<string | null>(null);

  protected readonly skeletonRows = [0, 1, 2];

  constructor() {
    this.route.paramMap
      .pipe(takeUntilDestroyed())
      .subscribe(params => this.load(Number(params.get('boardId'))));
  }

  protected dismissError(): void {
    this.actionError.set(null);
  }

  /** '' in the member shell and '/admin' in the other, so one component serves both. */
  protected boardPath(): string {
    return this.router.url.split('/boards/')[0] + `/boards/${this.boardId()}`;
  }

  protected create(): void {
    this.dialog
      .open<SprintFormDialog, undefined, SprintFormResult>(SprintFormDialog, { autoFocus: 'dialog' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (result !== undefined) {
          this.submit(result);
        }
      });
  }

  private submit(result: SprintFormResult): void {
    this.actionError.set(null);

    this.sprintService.create(this.boardId(), result)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        // Re-read rather than appending: the list is ordered by date, and a new sprint rarely
        // belongs at the end of it.
        next: () => this.load(this.boardId()),
        error: () => this.actionError.set(
          'That sprint could not be created. Its dates may overlap another sprint on this board.')
      });
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

    this.sprintService.list(boardId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: sprints => {
          this.sprints.set(sprints);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }
}
