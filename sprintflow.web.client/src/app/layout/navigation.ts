export interface NavItem {
  readonly label: string;
  readonly path: string;
}

export const ADMIN_NAVIGATION: readonly NavItem[] = [
  { label: 'Dashboard', path: '/admin/dashboard' },
  { label: 'Users', path: '/admin/users' },
  { label: 'Teams', path: '/admin/teams' },
  { label: 'Sprints', path: '/admin/sprints' },
  { label: 'Sprint Report', path: '/admin/sprint-report' }
];

export const USER_NAVIGATION: readonly NavItem[] = [
  { label: 'Dashboard', path: '/dashboard' },
  { label: 'Users', path: '/users' },
  { label: 'Teams', path: '/teams' },
  { label: 'Sprints', path: '/sprints' }
];
