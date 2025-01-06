var builder = WebApplication.CreateBuilder(args);

// hard code config folder
var folder = ".config";

// hard code 10001 port
builder.WebHost.UseUrls("http://0.0.0.0:10001");

builder.Services.AddCors();
var app = builder.Build();

// ensure config folder and create a token
if (!Directory.Exists(folder))
{
    Directory.CreateDirectory(folder);
    var guid = Convert
        .ToBase64String(Guid.NewGuid().ToByteArray())
        .Replace("/", "_")
        .Replace("+", "-");
    File.WriteAllText(Path.Combine(folder, "token"), guid[..22]);
}

var token = File.ReadAllText(".config/token");
Console.WriteLine($"TOKEN: {token}");

app.UseCors(p => p.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

app.Use(
    async (context, next) =>
    {
        var rq = context.Request;
        var rp = context.Response;

        if (!rq.Headers.TryGetValue("X-Access-Token", out var t) || t != token)
        {
            rp.StatusCode = 403;
            await rp.WriteAsync("Invalid access token");
            return;
        }

        rp.StatusCode = 204; // Set default status code
        await next();
    }
);

Task WriteFile(string name, string? data)
{
    if (!string.IsNullOrEmpty(data))
    {
        return File.WriteAllTextAsync(Path.Combine(folder, name), data);
    }
    return Task.CompletedTask;
}

app.MapPut(
    "/config",
    (HttpContext context) => WriteFile("app-config.json", context.Request.Form["conf"])
);

app.MapPut(
    "/board/{id}",
    (HttpContext context, string id) => WriteFile($"{id}.json", context.Request.Form["data"])
);

app.MapDelete(
    "/board/{id}",
    (string id) =>
    {
        var filePath = Path.Combine(folder, $"{id}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
);

app.Run();