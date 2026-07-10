# Oversight.AspNetCore

ASP.NET Core layer for Oversight: HTTP server traces (with `/health`, `/healthz`,
`/alive`, `/ready`, `/metrics` excluded by default — configurable glob list),
`http.server.request.duration` metrics, and an optional Prometheus scrape
endpoint at `/metrics` (off by default).

    builder.AddOversightAspNetCore();

Most apps should install the `Oversight` meta-package instead.
Docs: https://github.com/vitorafgomes/oversight
