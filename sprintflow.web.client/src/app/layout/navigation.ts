export interface NavItem {
  readonly label: string;
  readonly path: string;
}

/**
 * Admin sidebar entries. Four only — Settings and Profile are Version 2,
 * and Projects/Reports have no counterpart in the data model.
 */
export const ADMIN_NAVIGATION: readonly NavItem[] = [
  { label: 'Dashboard', path: '/admin/dashboard' },
  { label: 'Users', path: '/admin/users' },
  { label: 'Teams', path: '/admin/teams' },
  { label: 'Tasks', path: '/admin/tasks' }
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
  { label: 'Tasks', path: '/tasks' }
];
