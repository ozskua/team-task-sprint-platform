import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Team } from '../../core/models/team-models';
import { TeamService } from '../../core/services/team.service';
import { initialsOf } from '../../shared/initials';

/**
 * Every team in the company, read-only for everyone.
 *
 * A team is not something anyone creates or edits here: it comes into existence with its board, and
 * its members are whoever is assigned to that board's tasks. There is nothing to manage, so there
 * are no controls.
 */
@Component({
  selector: 'app-teams',
  templateUrl: './teams.html',
  standalone: false,
  styleUrl: './teams.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Teams {
  private readonly teamService = inject(TeamService);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly teams = signal<readonly Team[]>([]);

  protected readonly skeletonRows = [0, 1, 2];

  constructor() {
    this.teamService.getAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: teams => {
          this.teams.set(teams);
          this.loading.set(false);
        },
        error: () => {
          // The message is a local constant; the server's text is never shown.
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }
}
