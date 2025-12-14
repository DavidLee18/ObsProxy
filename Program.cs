using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(String.Format("http://0.0.0.0:{0}", Environment.GetEnvironmentVariable("OBS_PROXY_PORT")));
builder.Host.UseWindowsService();
var app = builder.Build();

app.MapGet("/", () =>
{
    var psi = new ProcessStartInfo
    {
        FileName = Environment.GetEnvironmentVariable("OBS_CMD_PATH"),
        Arguments = String.Format("-w obsws://localhost:{0}/{1} info", Environment.GetEnvironmentVariable("OBS_WS_PORT"), Environment.GetEnvironmentVariable("OBS_WS_PW")),
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
        FileName = Environment.GetEnvironmentVariable("OBS_CMD_PATH"),
        Arguments = String.Format("-w obsws://localhost:{0}/{1} streaming stop", Environment.GetEnvironmentVariable("OBS_WS_PORT"), Environment.GetEnvironmentVariable("OBS_WS_PW")),
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
        FileName = Environment.GetEnvironmentVariable("OBS_CMD_PATH"),
        Arguments = String.Format("-w obsws://localhost:{0}/{1} scene switch {2}", Environment.GetEnvironmentVariable("OBS_WS_PORT"), Environment.GetEnvironmentVariable("OBS_WS_PW"), res.scene),
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
