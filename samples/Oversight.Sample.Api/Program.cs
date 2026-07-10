using Oversight;

var builder = WebApplication.CreateBuilder(args);

builder.AddOversight(oversight => oversight.Prometheus.Enabled = true);

var app = builder.Build();

app.MapGet("/", () => "Oversight sample is running. Try /api/time, /health and /metrics.");
app.MapGet("/api/time", () => new { utc = DateTimeOffset.UtcNow });
app.MapGet("/health", () => "healthy");

app.Run();
