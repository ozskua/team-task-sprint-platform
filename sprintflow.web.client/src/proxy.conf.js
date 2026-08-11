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
  }
]

module.exports = PROXY_CONFIG;
