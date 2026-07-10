using Xunit;

// Every test here builds a WebApplicationFactory whose TracerProvider subscribes to the
// process-global "Microsoft.AspNetCore.Hosting" ActivitySource. Concurrent factories would
// each export the other's server spans into their own InMemoryExporter, so the suite must
// run serially to keep exported-activity assertions isolated.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
