const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7129';

const realtimeTarget = env.SPRINTFLOW_SIGNALR_URL ?? 'https://localhost:7200';

const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
      "/api",
    ],
    target,
    secure: false
  },
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
