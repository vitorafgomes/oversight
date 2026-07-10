using Farol;

var builder = WebApplication.CreateBuilder(args);

builder.AddFarol(farol => farol.Prometheus.Enabled = true);

var app = builder.Build();

app.MapGet("/", () => "Farol sample is running. Try /api/time, /health and /metrics.");
app.MapGet("/api/time", () => new { utc = DateTimeOffset.UtcNow });
app.MapGet("/health", () => "healthy");

app.Run();
