import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';

import { AuthInterceptor } from './interceptors/auth-interceptor';
import { AuthService } from './services/auth.service';
import { TokenStorage } from './services/token-storage';
import { UserService } from './services/user.service';

/**
 * Application-wide singletons. Imported once by AppModule, never by a feature module.
 * Guards are plain functions and need no registration.
 */
@NgModule({
  providers: [
    TokenStorage,
    AuthService,
    UserService,
    // Class-based rather than functional: AppModule imports HttpClientModule, which has no
    // withInterceptors equivalent. `multi` appends to the chain instead of replacing it.
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ]
})
export class CoreModule { }
