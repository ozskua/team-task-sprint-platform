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
import { SprintReportService } from './services/sprint-report.service';
import { SprintService } from './services/sprint.service';
import { TeamService } from './services/team.service';
import { TokenStorage } from './services/token-storage';
import { UserService } from './services/user.service';

@NgModule({
  providers: [
    TokenStorage,
    AuthService,
    UserService,
    AreaService,
    BoardDetailService,
    BoardRealtimeService,
    BoardSummaryService,
    SprintService,
    SprintReportService,
    InvitationService,
    TeamService,
    DashboardService,
    AdminDashboardService,
    NotificationService,
    NotificationRealtimeService,
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
  ]
})
export class CoreModule { }
