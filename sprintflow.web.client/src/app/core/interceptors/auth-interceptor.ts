import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { TokenStorage } from '../services/token-storage';

/**
 * Attaches the bearer token to API calls, so protected endpoints can identify the caller.
 *
 * Reads TokenStorage rather than AuthService on purpose: AuthService injects HttpClient, and
 * HttpClient depends on the interceptor chain, so injecting it here would be a dependency cycle.
 * Storage is always current — AuthService writes it on login and clears it on logout.
 *
 * Responses are passed through untouched. There is deliberately no global 401 handling here.
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private readonly tokenStorage = inject(TokenStorage);

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.tokenStorage.read()?.token;

    // Nothing to send, or the request is not going to our API — leave it alone.
    if (token === undefined || !request.url.startsWith(environment.apiBaseUrl)) {
      return next.handle(request);
    }

    return next.handle(
      request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    );
  }
}
