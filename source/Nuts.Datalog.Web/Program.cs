using Microsoft.AspNetCore.Mvc;
using Nuts.Datalog;
using Nuts.Datalog.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR().AddJsonProtocol(options => { options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(5555));
}
var app = builder.Build();

app.MapGet("/", () =>
{
    var path = Path.Combine(builder.Environment.WebRootPath, "pages", "home.html");
    var stream = File.OpenRead(path);
    return Results.Stream(stream, "text/html");
});

app.MapGet("/download/{text}", ([FromRoute] string text) =>
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), "Datalogs", text);
    var stream = File.OpenRead(path);
    return Results.File(stream, "text/csv", $"{text}");
});

DatalogWriterCache datalogWriterCache = new(Path.Combine(Directory.GetCurrentDirectory(), "Configs"), Path.Combine(Directory.GetCurrentDirectory(), "Datalogs"));
app.MapPost("/api/datalog/{text}", ([FromRoute] string text, [FromBody] CaseInsensitiveDictionary<JsonElement> keyValuePairs) =>
{
    if (datalogWriterCache.TryGetDatalogWriter(text, out DatalogWriter writer))
    {
        writer.WriteLine(keyValuePairs);
    }
    else
    {
        return Results.BadRequest($"Config file with name \"{text}\" not found");
    }
    return Results.Ok();
});

app.UseStaticFiles();
app.MapHub<DatalogHub>("/datalogHub");
app.Run();