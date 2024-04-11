using System;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Honeycomb.OpenTelemetry;
using OpenTelemetry.Resources;
using System.Collections.Generic;
using System.Reflection;
using OpenTelemetry.Trace;

var builder = new HostBuilder();

//Logging
Serilog.Debugging.SelfLog.Enable(Console.WriteLine);
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentUserName()
    .Enrich.WithProcessId()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{ProcessId}] [{Level}] - {Message:j}{NewLine}{Exception}", theme: AnsiConsoleTheme.Code, applyThemeToRedirectedOutput: true)
    .CreateLogger();


Log.Logger.Information("Application Startup");

var honeycombOptions = new HoneycombOptions
{
    Endpoint = "https://api.honeycomb.io:443",
    ServiceName = "Time Sleeper",
    ServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
    ApiKey = Environment.GetEnvironmentVariable("HoneycombApiKey"),
    ResourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(
        new Dictionary<string, object>
        {
            ["telemetry.sdk.name"] = "opentelemetry",
            ["telemetry.sdk.language"] = "dotnet",
        })
};

builder
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.AllowTrailingCommas = true;
        });
        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights()
            .AddLogging(loggingBuilder => loggingBuilder.AddSerilog(Log.Logger, true))
            .AddSingleton(honeycombOptions)
            .AddSingleton(TracerProvider.Default.GetTracer(honeycombOptions.ServiceName))
            .AddOpenTelemetry().WithTracing(oTel =>
            {
                oTel
                    .AddHoneycomb(honeycombOptions)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddCommonInstrumentations();
            });

    });

var host = builder.Build();
await host.RunAsync();
