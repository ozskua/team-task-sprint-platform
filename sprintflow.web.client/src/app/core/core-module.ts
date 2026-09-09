import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule } from '@angular/core';

import { AuthInterceptor } from './interceptors/auth-interceptor';
import { AdminDashboardService } from './services/admin-dashboard.service';
import { AreaService } from './services/area.service';
import { AuthService } from './services/auth.service';
import { BoardDetailService } from './services/board-detail.service';
import { BoardRealtimeService } from './services/board-realtime.service';
import { BoardSummaryService } from './services/board-summary.service';
import { DashboardService } from './services/dashboard.service';
import { InvitationService } from './services/invitation.service';
import { NotificationRealtimeService } from './services/notification-realtime.service';
import { NotificationService } from './services/notification.service';
import { SprintService } from './services/sprint.service';
import { TeamService } from './services/team.service';
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
    AreaService,
    // One board is on screen at a time, so its state is a singleton like the Area list rather than
    // something each visit to the page recreates.
    BoardDetailService,
    BoardRealtimeService,
    BoardSummaryService,
    SprintService,
    // Stateless, but a singleton like the rest: both shells' user menu creates invitations, and the
    // register page redeems them.
    InvitationService,
    TeamService,
    DashboardService,
    AdminDashboardService,
    // Singletons for the same reason as the board's pair: the header's badge and the panel it opens
    // must show the same thing, whichever shell is on screen.
    NotificationService,
    NotificationRealtimeService,
    // Class-based rather than functional: AppModule imports HttpClientModule, which has no
    // withInterceptors equivalent. `multi` appends to the chain instead of replacing it.
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ]
})
export class CoreModule { }
