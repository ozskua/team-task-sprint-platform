import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Team } from '../../core/models/team-models';
import { TeamService } from '../../core/services/team.service';
import { initialsOf } from '../../shared/initials';

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
          this.failed.set(true);
          this.loading.set(false);
        }
      });
  }

  protected initials(fullName: string): string {
    return initialsOf(fullName);
  }
}
