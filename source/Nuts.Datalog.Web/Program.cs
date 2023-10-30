using Microsoft.AspNetCore.Mvc;
using Nuts.Datalog;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

DatalogWriterCache datalogWriterCache = new(Directory.GetCurrentDirectory());

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
        return Results.BadRequest($"JSON file with name \"{text}\" not found");
    }
    return Results.Ok();
});
app.Run();