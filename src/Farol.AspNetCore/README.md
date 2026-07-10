# Farol.AspNetCore

ASP.NET Core layer for Farol: HTTP server traces (with `/health`, `/healthz`,
`/alive`, `/ready`, `/metrics` excluded by default — configurable glob list),
`http.server.request.duration` metrics, and an optional Prometheus scrape
endpoint at `/metrics` (off by default).

    builder.AddFarolAspNetCore();

Most apps should install the `Farol` meta-package instead.
Docs: https://github.com/vitorafgomes/farol
