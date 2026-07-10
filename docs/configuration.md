# Configuration reference

## Precedence

1. Standard `OTEL_*` environment variables (highest — never overridden by Farol).
2. The `AddFarol*` lambda.
3. The `"Farol"` appsettings section.
4. Farol defaults.

## OTel standard variables (handled by the SDK, not Farol)

`OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`,
`OTEL_EXPORTER_OTLP_HEADERS`, `OTEL_SERVICE_NAME`, `OTEL_RESOURCE_ATTRIBUTES`,
`OTEL_TRACES_SAMPLER` (default `parentbased_always_on`).

## Farol section

See the README table. Notes:

- `ExcludedPaths` entries must start with `/`; `*` matches any characters
  including `/`. Invalid entries fail at startup, never at request time.
- Configuration-bound `ExcludedPaths` values are appended to the defaults; call
  `ExcludedPaths.Clear()` in the lambda to replace them.
- `Prometheus:Enabled` and `EntityFrameworkCore:Enabled` are read when the
  `AddFarol*` method runs (they add/skip registrations); set them in
  appsettings or in the same call's lambda.
- Configure options once — pass the lambda to a single `AddFarol*` call.

## Security

`db.query.text` / `db.statement` are stripped from database spans unless
`Farol:EntityFrameworkCore:CaptureQueryText` is `true`. Query parameters are
never captured.
