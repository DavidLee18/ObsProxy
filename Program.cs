using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var OBS_CMD_PATH = Environment.GetEnvironmentVariable("OBS_CMD_PATH");
var OBS_WS_PORT = Environment.GetEnvironmentVariable("OBS_WS_PORT");
var OBS_WS_PW = Environment.GetEnvironmentVariable("OBS_WS_PW");
var OBS_PROXY_PORT = Environment.GetEnvironmentVariable("OBS_PROXY_PORT");

if (string.IsNullOrEmpty(OBS_CMD_PATH) || string.IsNullOrEmpty(OBS_WS_PORT) || string.IsNullOrEmpty(OBS_WS_PW) || string.IsNullOrEmpty(OBS_PROXY_PORT))
{
    throw new Exception("Missing environment variables");
}

builder.WebHost.UseUrls($"http://0.0.0.0:{OBS_PROXY_PORT}");
builder.Host.UseWindowsService();
builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "ObsProxyService"; // Matches the name in appsettings.json
});
var app = builder.Build();

app.Logger.LogInformation($"Starting ObsProxy on port {OBS_PROXY_PORT}; obs-cmd path is {OBS_CMD_PATH}, obs-ws password is {OBS_WS_PW}");

app.MapGet("/", () =>
{
    var psi = new ProcessStartInfo
    {
        FileName = OBS_CMD_PATH,
        Arguments = $"-w obsws://localhost:{OBS_WS_PORT}/{OBS_WS_PW} info",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    var p = Process.Start(psi);
    if (p == null)
    {
        return Results.InternalServerError("process spawn failed");
    }
    p.WaitForExit();
    string err = p.StandardError.ReadToEnd();
    if (err.Length > 0)
    {
        p.Dispose();
        return Results.InternalServerError(err);
    }
    else
    {
        string o = p.StandardOutput.ReadToEnd();
        p.Dispose();
        return Results.Ok(o);
    }
});

app.MapDelete("/", () =>
{
    var psi = new ProcessStartInfo
    {
        FileName = OBS_CMD_PATH,
        Arguments = $"-w obsws://localhost:{OBS_WS_PORT}/{OBS_WS_PW} streaming stop",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    var p = Process.Start(psi);
    if (p == null)
    {
        return Results.InternalServerError("process spawn failed");
    }
    p.WaitForExit();
    string err = p.StandardError.ReadToEnd();
    if (err.Length > 0)
    {
        p.Dispose();
        return Results.InternalServerError(err);
    }
    else
    {
        string o = p.StandardOutput.ReadToEnd();
        p.Dispose();
        return Results.Ok(o);
    }
});

app.MapPost("/", (ScenePack res) =>
{
    var psi = new ProcessStartInfo
    {
        FileName = OBS_CMD_PATH,
        Arguments = $"-w obsws://localhost:{OBS_WS_PORT}/{OBS_WS_PW} scene switch {res.scene}",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    var p = Process.Start(psi);
    if (p == null)
    {
        return Results.InternalServerError("process spawn failed");
    }
    p.WaitForExit();
    string err = p.StandardError.ReadToEnd();
    if (err.Length > 0)
    {
        p.Dispose();
        return Results.InternalServerError(err);
    }
    else
    {
        string o = p.StandardOutput.ReadToEnd();
        p.Dispose();
        return Results.Ok(o);
    }
});

app.Run();

public record ScenePack(string scene);
