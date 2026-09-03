// Relative, not absolute: the dev server proxies /api to the backend and production serves both
// from one origin. Both cases resolve the same, so there is no second environment file.
export const environment = {
  apiBaseUrl: '/api',

  // Not under /api: it is a hub, not a REST resource. Relative for the same reason as above — the
  // dev server proxies /hubs, and production serves it from the same origin.
  boardHubUrl: '/hubs/board'
};
