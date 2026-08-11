// Relative, not absolute: the dev server proxies /api to the backend and production serves both
// from one origin. Both cases resolve the same, so there is no second environment file.
export const environment = {
  apiBaseUrl: '/api'
};
