using Microsoft.AspNetCore.Mvc;
using Nuts.Datalog;
using Nuts.Datalog.Web;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR().AddJsonProtocol(options => { options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
var app = builder.Build();

DatalogWriterCache datalogWriterCache = new(Path.Combine(Directory.GetCurrentDirectory(), "Configs"), Path.Combine(Directory.GetCurrentDirectory(), "Datalogs"));

app.MapGet("/", (httpContext) =>
{
    var htmlContent = File.ReadAllText("Pages/Home.html");
    httpContext.Response.Headers.Add("Content-Type", "text/html");
    return httpContext.Response.WriteAsync(htmlContent);
});

app.MapPost("/api/datalog/{text}", ([FromRoute] string text, [FromBody] CaseInsensitiveDictionary<string> keyValuePairs) =>
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