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
builder.Services.AddControllers();
builder.Services.AddHostedService<ObsProxy.KeepObsUpService>();
if (System.OperatingSystem.IsWindows())
{
	builder.Logging.AddEventLog(settings =>
	{
		settings.SourceName = "ObsProxyService"; // Matches the name in appsettings.json
	});
}
;
var app = builder.Build();

app.Logger.LogInformation("Starting ObsProxy on port {Port}; obs-cmd path is {CmdPath}, obs-ws password is {Password}", OBS_PROXY_PORT, OBS_CMD_PATH, OBS_WS_PW);

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

app.MapPost("/", (ObsProxy.ScenePack res) =>
{
	var psi = new ProcessStartInfo
	{
		FileName = OBS_CMD_PATH,
		Arguments = $"-w obsws://localhost:{OBS_WS_PORT}/{OBS_WS_PW} scene switch {res.Scene}",
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

namespace ObsProxy
{
	public record ScenePack(string Scene);

	public class KeepObsUpService(ILogger<KeepObsUpService> logger) : BackgroundService
	{
		private readonly ILogger<KeepObsUpService> _logger = logger;
		private readonly string OBS_PATH = Environment.GetEnvironmentVariable("OBS_CMD_PATH") ?? throw new Exception("Missing OBS_CMD_PATH environment variable");
		private const string SENTINEL_PATH = "%APPDATA%\\obs-studio\\.sentinel";

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("KeepObsUpService is starting.");

			stoppingToken.Register(() =>
				_logger.LogInformation("KeepObsUpService is stopping."));

			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("KeepObsUpService is doing background work at: {time}", DateTimeOffset.Now);

				if (Process.GetProcessesByName("obs").Length == 0)
				{
					_logger.LogWarning("OBS is not running. Attempting to start it.");
					try
					{
						if (Directory.Exists(Environment.ExpandEnvironmentVariables(SENTINEL_PATH)))
						{
							_logger.LogInformation("Sentinel directory exists at {path}. Deleting it to allow OBS to start.", SENTINEL_PATH);
							Directory.Delete(SENTINEL_PATH, true);
						}
						var psi = new ProcessStartInfo
						{
							FileName = OBS_PATH,
							UseShellExecute = false,
							RedirectStandardOutput = true,
							RedirectStandardError = true,
						};
						var p = Process.Start(psi);
						if (p == null)
						{
							_logger.LogError("Failed to start OBS: process spawn failed");
						}
						else
						{
							p.WaitForExit();
							string err = p.StandardError.ReadToEnd();
							if (err.Length > 0)
							{
								_logger.LogError("Failed to start OBS: {error}", err);
							}
							else
							{
								string o = p.StandardOutput.ReadToEnd();
								_logger.LogInformation("OBS started successfully: {output}", o);
							}
							p.Dispose();
						}
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Exception occurred while trying to start OBS");
					}
				}

				await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
			}

			_logger.LogInformation("KeepObsUpService has stopped.");
		}
	}
}
