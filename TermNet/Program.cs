using TermNet.Logic;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Mvc", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);

var cs = Environment.GetEnvironmentVariable("TERMNET_DATABASE")
    ?? "Server=127.0.0.1;Port=5432;User Id=postgres;Password=1234;Database=termnetdb";

// Ensure schema exists, then seed from txt files if table is empty
PostgresTermNet.EnsureSchema(cs);
if (PostgresTermNet.IsEmpty(cs))
{
    var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
    var fileNets = Directory.Exists(dataDir)
        ? Directory.GetFiles(dataDir, "*.txt")
              .Select(f => new FileTermNet(Path.GetFileNameWithoutExtension(f), f))
              .ToList<ITermNet>()
        : new List<ITermNet>();
    PostgresTermNet.Seed(cs, fileNets);
}

// Build registry from Postgres — one PostgresTermNet per distinct net_name in DB
var registry = new TermNetRegistry();
foreach (var name in GetNetNames(cs))
    registry.Register(new PostgresTermNet(name, cs));

builder.Services.AddSingleton(registry);

var app = builder.Build();
app.MapControllers();
app.Run();

static List<string> GetNetNames(string connectionString)
{
    var names = new List<string>();
    using var conn = new Npgsql.NpgsqlConnection(connectionString);
    conn.Open();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT DISTINCT net_name FROM synonym_relation ORDER BY net_name";
    using var reader = cmd.ExecuteReader();
    while (reader.Read()) names.Add(reader.GetString(0));
    return names;
}
