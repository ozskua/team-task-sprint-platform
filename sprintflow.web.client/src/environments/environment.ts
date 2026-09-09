// Relative, not absolute: the dev server proxies /api to the backend and production serves both
// from one origin. Both cases resolve the same, so there is no second environment file.
export const environment = {
  apiBaseUrl: '/api',

  // Not under /api: it is a hub, not a REST resource. Relative for the same reason as above — the
  // dev server proxies /hubs, and production serves it from the same origin.
  boardHubUrl: '/hubs/board',

  // A second hub rather than a second use of the first: the board hub's connection only exists
  // while a board is open, and a notification has to reach the header from anywhere in the app.
  notificationHubUrl: '/hubs/notifications'
};
