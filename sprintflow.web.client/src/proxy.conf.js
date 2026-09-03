const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7129';

// Angular calls the API with relative paths, so the dev server must forward /api to ASP.NET.
// Keeping URLs relative means no backend host is hard-coded, and production needs no proxy at all.
const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
      "/api",
    ],
    target,
    secure: false
  },
  // The SignalR hub, kept as its own entry so the API's forwarding is untouched. `ws` is what
  // matters here: without it the WebSocket upgrade is not proxied and SignalR silently falls back
  // to long polling. Production needs none of this — one origin serves both.
  {
    context: [
      "/hubs",
    ],
    target,
    secure: false,
    ws: true
  }
]

module.exports = PROXY_CONFIG;
