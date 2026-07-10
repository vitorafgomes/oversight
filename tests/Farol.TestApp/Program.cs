using Farol;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddFarol();

// A single kept-open connection: Sqlite ":memory:" databases vanish when their
// connection closes, so DbContext instances must share this one.
var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();
builder.Services.AddSingleton(connection);
builder.Services.AddDbContext<TodoDbContext>((sp, options) =>
    options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    db.Database.EnsureCreated();
    if (!db.Todos.Any())
    {
        db.Todos.Add(new Todo { Title = "first" });
        db.SaveChanges();
    }
}

app.MapGet("/api/hello", () => "hello");
app.MapGet("/health", () => "healthy");
app.MapGet("/api/todos", async (TodoDbContext db) => await db.Todos.ToListAsync());

app.Run();

public partial class Program { }

public class Todo
{
    public int Id { get; set; }

    public required string Title { get; set; }
}

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<Todo> Todos => Set<Todo>();
}
