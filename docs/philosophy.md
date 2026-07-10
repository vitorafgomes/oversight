# Philosophy

- **One call, real defaults.** `AddFarol()` must be enough for production.
- **Never re-invent OTel.** Anything the OTel spec standardizes (endpoints,
  samplers, resource attributes) is configured the OTel way.
- **Telemetry never breaks the app.** No throwing on the hot path; an
  unreachable collector is the SDK's problem (retry/drop), not yours.
  Misconfiguration fails fast at startup with an actionable message.
- **Explicit composition.** The meta-package calls three granular methods; no
  reflection, no assembly scanning. Workers compose granular packages directly.
- **Secure by default.** SQL text capture is opt-in.

Origin: packaging model inspired by the (abandoned) Farfetch Monitoring
Framework, rebuilt on OpenTelemetry.
