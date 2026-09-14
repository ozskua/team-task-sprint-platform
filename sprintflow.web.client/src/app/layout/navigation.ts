export interface NavItem {
  readonly label: string;
  readonly path: string;
}

/**
 * Admin sidebar entries. Three only — Settings and Profile are Version 2,
 * Projects/Reports have no counterpart in the data model, and tasks are reached
 * inside a board rather than from a global destination.
 */
export const ADMIN_NAVIGATION: readonly NavItem[] = [
  { label: 'Dashboard', path: '/admin/dashboard' },
  { label: 'Users', path: '/admin/users' },
  { label: 'Teams', path: '/admin/teams' },
  // Below Teams: a sprint belongs to a board, and a board is what a team is, so the two read
  // together. The page surveys every board's sprints; creating one stays on the board itself.
  { label: 'Sprints', path: '/admin/sprints' },
  // Admin only, so it appears here and deliberately not in USER_NAVIGATION.
  { label: 'Sprint Report', path: '/admin/sprint-report' }
];

/**
 * User sidebar entries. The Sidebar component itself is role-agnostic — it
 * renders whichever list it is given, so the only difference between the two
 * shells is which of these arrays they pass in.
 */
export const USER_NAVIGATION: readonly NavItem[] = [
  { label: 'Dashboard', path: '/dashboard' },
  { label: 'Users', path: '/users' },
  { label: 'Teams', path: '/teams' },
  { label: 'Sprints', path: '/sprints' }
];
