import { HttpClient } from '@angular/common/http';
import { Injectable, Signal, WritableSignal, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';

@Injectable()
export class ProfilePhotoService {
  private readonly http = inject(HttpClient);

  private readonly urls = new Map<number, WritableSignal<string | null>>();

  urlFor(userId: number): Signal<string | null> {
    let entry = this.urls.get(userId);

    if (entry === undefined) {
      entry = signal<string | null>(null);
      this.urls.set(userId, entry);
      this.load(userId, entry);
    }

    return entry.asReadonly();
  }

  upload(userId: number, file: File): Observable<void> {
    const form = new FormData();
    form.append('file', file);

    return this.http
      .put<void>(`${environment.apiBaseUrl}/users/me/photo`, form)
      .pipe(tap(() => this.refresh(userId)));
  }

  remove(userId: number): Observable<void> {
    return this.http
      .delete<void>(`${environment.apiBaseUrl}/users/me/photo`)
      .pipe(tap(() => this.clear(userId)));
  }

  private refresh(userId: number): void {
    const entry = this.urls.get(userId);

    if (entry === undefined) {
      return;
    }

    this.revoke(entry);
    this.load(userId, entry);
  }

  private clear(userId: number): void {
    const entry = this.urls.get(userId);

    if (entry !== undefined) {
      this.revoke(entry);
    }
  }

  private load(userId: number, entry: WritableSignal<string | null>): void {
    this.http
      .get(`${environment.apiBaseUrl}/users/${userId}/photo`, { responseType: 'blob' })
      .subscribe({
        next: blob => entry.set(URL.createObjectURL(blob)),
        error: () => entry.set(null)
      });
  }

  private revoke(entry: WritableSignal<string | null>): void {
    const url = entry();

    if (url !== null) {
      URL.revokeObjectURL(url);
      entry.set(null);
    }
  }
}
