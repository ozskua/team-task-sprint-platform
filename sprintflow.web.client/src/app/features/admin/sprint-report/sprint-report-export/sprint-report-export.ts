import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  SprintReportExportFormat,
  SprintReportExportResult
} from '../../../../core/models/sprint-report-models';
import { SprintReportService } from '../../../../core/services/sprint-report.service';

const EXPORT_FAILED_MESSAGE: Record<SprintReportExportFormat, string> = {
  xlsx: 'The Excel report could not be created. Please try again.',
  pdf: 'The PDF report could not be created. Please try again.'
};

@Component({
  selector: 'app-sprint-report-export',
  templateUrl: './sprint-report-export.html',
  standalone: false,
  styleUrl: './sprint-report-export.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SprintReportExport {
  private readonly sprintReportService = inject(SprintReportService);
  private readonly destroyRef = inject(DestroyRef);

  readonly disabled = input(false);

  protected readonly exporting = signal<SprintReportExportFormat | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  protected readonly buttonsDisabled = computed(() => this.disabled() || this.exporting() !== null);

  protected export(format: SprintReportExportFormat): void {
    if (this.buttonsDisabled()) {
      return;
    }

    this.exporting.set(format);
    this.errorMessage.set(null);
    this.notice.set(null);

    this.sprintReportService.export(format)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.exporting.set(null);
          this.saveBlob(result.blob, result.fileName);
          this.notice.set(SprintReportExport.noticeOf(result));
        },
        error: () => {
          this.exporting.set(null);
          this.errorMessage.set(EXPORT_FAILED_MESSAGE[format]);
        }
      });
  }

  private static noticeOf(result: SprintReportExportResult): string | null {
    const areas = (count: number) => `${count} area${count === 1 ? '' : 's'}`;

    if (result.failed > 0) {
      return result.delivered > 0
        ? `Report downloaded; delivered to ${areas(result.delivered)}, delivery to ${areas(result.failed)} failed.`
        : `Report downloaded; delivery to ${areas(result.failed)} failed.`;
    }

    return result.delivered > 0 ? `Report delivered to ${areas(result.delivered)}.` : null;
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = fileName;
    link.click();

    URL.revokeObjectURL(url);
  }
}
