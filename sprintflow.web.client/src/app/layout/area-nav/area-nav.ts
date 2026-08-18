import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';

import { Area } from '../../core/models/area-models';
import { AreaService } from '../../core/services/area.service';
import { AreaInfoDialog } from '../area-info-dialog/area-info-dialog';

/**
 * The Areas section of the sidebar. A sibling of Sidebar rather than part of it: Sidebar renders a
 * flat list of navigation links and nothing else, and both shells show this the same way because
 * any authenticated user — Admin or Member — can create an Area.
 *
 * Area rows are not links. There is no Area page yet, and pointing at a route that does not exist
 * would be a dead link. Board rows are, since a board has a page.
 */
@Component({
  selector: 'app-area-nav',
  templateUrl: './area-nav.html',
  standalone: false,
  styleUrl: './area-nav.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AreaNav {
  /**
   * Where a board link points, supplied by whichever shell is rendering this. Both shells show the
   * same boards but reach them at their own URLs, so a fixed path here would be wrong in one of
   * them — the same reason Sidebar is handed its items instead of choosing them.
   */
  readonly boardRoutePrefix = input.required<string>();

  private readonly areaService = inject(AreaService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly areas = this.areaService.areas;

  protected readonly loading = signal(true);
  protected readonly loadFailed = signal(false);

  protected readonly formOpen = signal(false);
  protected readonly draftName = signal('');
  protected readonly creating = signal(false);
  protected readonly createFailed = signal(false);

  constructor() {
    this.areaService.load()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: () => this.loading.set(false),
        error: () => {
          this.loadFailed.set(true);
          this.loading.set(false);
        }
      });
  }

  protected openForm(): void {
    this.formOpen.set(true);
    this.createFailed.set(false);
  }

  protected closeForm(): void {
    this.formOpen.set(false);
    this.draftName.set('');
    this.createFailed.set(false);
  }

  protected onDraftInput(value: string): void {
    this.draftName.set(value);
  }

  protected submit(): void {
    const name = this.draftName().trim();

    // Guard both conditions: an empty name has nothing to send, and a second submit while the
    // first is in flight would create a duplicate Area.
    if (name.length === 0 || this.creating()) {
      return;
    }

    this.creating.set(true);
    this.createFailed.set(false);

    this.areaService.create(name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        // The service already added the server's Area to the list; nothing is built here.
        next: () => {
          this.creating.set(false);
          this.closeForm();
        },
        error: () => {
          // The typed name is kept so the attempt is not lost.
          this.creating.set(false);
          this.createFailed.set(true);
        }
      });
  }

  protected openInformation(area: Area): void {
    this.dialog.open(AreaInfoDialog, { data: area, autoFocus: 'dialog' });
  }

  // --- Boards -------------------------------------------------------------
  // One form at a time: boardFormAreaId identifies which Area is currently taking a board name,
  // so two Areas cannot have an open form and two half-typed names between them.

  protected readonly boardFormAreaId = signal<number | null>(null);
  protected readonly boardDraftName = signal('');
  protected readonly creatingBoard = signal(false);
  protected readonly boardCreateFailed = signal(false);

  protected openBoardForm(areaId: number): void {
    this.boardFormAreaId.set(areaId);
    this.boardDraftName.set('');
    this.boardCreateFailed.set(false);
  }

  protected closeBoardForm(): void {
    this.boardFormAreaId.set(null);
    this.boardDraftName.set('');
    this.boardCreateFailed.set(false);
  }

  protected onBoardDraftInput(value: string): void {
    this.boardDraftName.set(value);
  }

  protected submitBoard(): void {
    const areaId = this.boardFormAreaId();
    const name = this.boardDraftName().trim();

    // An empty name has nothing to send, and a second submit while the first is in flight would
    // create a duplicate board.
    if (areaId === null || name.length === 0 || this.creatingBoard()) {
      return;
    }

    this.creatingBoard.set(true);
    this.boardCreateFailed.set(false);

    this.areaService.createBoard(areaId, name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        // The service already added the server's board to the Area; nothing is built here.
        next: () => {
          this.creatingBoard.set(false);
          this.closeBoardForm();
        },
        error: () => {
          // The typed name is kept so the attempt is not lost.
          this.creatingBoard.set(false);
          this.boardCreateFailed.set(true);
        }
      });
  }
}
