# Farol.Core

Host-agnostic OpenTelemetry defaults for .NET 10: OTLP exporter (standard `OTEL_*`
env vars respected, default `localhost:4317`), resource identity fallbacks
(service name/version/environment), HttpClient traces, runtime (`System.Runtime`)
and process metrics.

    builder.AddFarolCore();

Use this package directly in Worker Services and console hosts. ASP.NET Core apps
should install the `Farol` meta-package instead. Docs: https://github.com/vitorafgomes/farol
