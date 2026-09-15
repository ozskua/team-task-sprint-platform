import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TokenStorage } from '../services/token-storage';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private readonly tokenStorage = inject(TokenStorage);

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.tokenStorage.read()?.token;

    if (token === undefined || !request.url.startsWith(environment.apiBaseUrl)) {
      return next.handle(request);
    }

    return next.handle(
      request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    );
  }
}
