import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { SprintReportExportFormat, SprintReportExportResult, SprintReportRow } from '../models/sprint-report-models';

import { environment } from '../../../environments/environment';

const WEBHOOK_HEADER = 'X-SprintFlow-Webhook';

@Injectable()
export class SprintReportService {
  private readonly http = inject(HttpClient);

  list(): Observable<readonly SprintReportRow[]> {
    return this.http.get<readonly SprintReportRow[]>(`${environment.apiBaseUrl}/admin/sprint-report`);
  }

  export(format: SprintReportExportFormat): Observable<SprintReportExportResult> {
    return this.http
      .get(`${environment.apiBaseUrl}/admin/sprint-report/export/${format}`, {
        responseType: 'blob',
        observe: 'response'
      })
      .pipe(map(response => SprintReportService.toResult(response, format)));
  }

  private static toResult(
    response: HttpResponse<Blob>,
    format: SprintReportExportFormat
  ): SprintReportExportResult {
    const webhook = SprintReportService.parseWebhook(response.headers.get(WEBHOOK_HEADER));

    return {
      blob: response.body ?? new Blob(),
      fileName: SprintReportService.fileNameOf(response.headers.get('Content-Disposition'))
        ?? `SprintReport.${format}`,
      delivered: webhook.delivered,
      failed: webhook.failed
    };
  }

  private static fileNameOf(disposition: string | null): string | null {
    if (disposition === null) {
      return null;
    }

    const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition);

    if (encoded !== null) {
      return decodeURIComponent(encoded[1]);
    }

    const plain = /filename="?([^";]+)"?/i.exec(disposition);

    return plain === null ? null : plain[1];
  }

  private static parseWebhook(header: string | null): { delivered: number; failed: number } {
    const values = new Map<string, number>();

    for (const part of (header ?? '').split(';')) {
      const [key, value] = part.split('=');
      const count = Number(value);

      if (key && Number.isInteger(count)) {
        values.set(key.trim(), count);
      }
    }

    return { delivered: values.get('delivered') ?? 0, failed: values.get('failed') ?? 0 };
  }
}
