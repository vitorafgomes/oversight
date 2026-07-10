# Farol.EntityFrameworkCore

Database instrumentation for Farol: EF Core and SqlClient command traces.
Security default: `db.query.text` is stripped from spans unless you opt in via
`Farol:EntityFrameworkCore:CaptureQueryText = true`.

    builder.AddFarolEntityFrameworkCore();

Most apps should install the `Farol` meta-package instead.
Docs: https://github.com/vitorafgomes/farol
