# Oversight.EntityFrameworkCore

Database instrumentation for Oversight: EF Core and SqlClient command traces.
Security default: `db.query.text` is stripped from spans unless you opt in via
`Oversight:EntityFrameworkCore:CaptureQueryText = true`.

    builder.AddOversightEntityFrameworkCore();

Most apps should install the `Oversight` meta-package instead.
Docs: https://github.com/vitorafgomes/oversight
