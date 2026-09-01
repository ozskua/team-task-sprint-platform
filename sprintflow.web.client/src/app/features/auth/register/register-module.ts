import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';

import { Register } from './register';
import { RegisterRoutingModule } from './register-routing-module';

/**
 * Its own lazy module rather than another component inside AuthModule: that module's routing already
 * claims the empty path for Login, and `/login` must keep working exactly as it does.
 *
 * RouterLink is imported as a single directive — the routing module below re-exports RouterModule
 * for the route itself, and the template only needs the link.
 */
@NgModule({
  declarations: [
    Register
  ],
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    RouterLink,
    RegisterRoutingModule
  ]
})
export class RegisterModule { }
