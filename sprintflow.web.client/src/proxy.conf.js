const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7129';

// The realtime service is a separate process now, on its own port. Overridable the same way, so a
// different machine needs no file edit.
const realtimeTarget = env.SPRINTFLOW_SIGNALR_URL ?? 'https://localhost:7200';

// Angular calls the API with relative paths, so the dev server must forward /api to ASP.NET.
// Keeping URLs relative means no backend host is hard-coded in the application itself.
const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
      "/api",
    ],
    target,
    secure: false
  },
  // The hubs, now answered by SprintFlow.Web.SignalR rather than by the API. This entry is the
  // whole of the client-side change: the application still asks for /hubs/board and
  // /hubs/notifications, so no Angular source file knows the split happened.
  //
  // `ws` is what matters here: without it the WebSocket upgrade is not proxied and SignalR
  // silently falls back to long polling.
  //
  // Production is a genuinely open question — with two origins it needs a reverse proxy in front
  // of both, or absolute URLs plus CORS. This dev proxy is not that decision.
  {
    context: [
      "/hubs",
    ],
    target: realtimeTarget,
    secure: false,
    ws: true
  }
]

module.exports = PROXY_CONFIG;
