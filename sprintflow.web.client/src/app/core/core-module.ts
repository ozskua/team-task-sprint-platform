import { NgModule } from '@angular/core';

import { AuthService } from './services/auth.service';
import { TokenStorage } from './services/token-storage';

/**
 * Application-wide singletons. Imported once by AppModule, never by a feature module.
 * Guards are plain functions and need no registration.
 */
@NgModule({
  providers: [
    TokenStorage,
    AuthService
  ]
})
export class CoreModule { }
