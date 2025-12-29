using Bazario.Api.StartupExtensions;
using Serilog;
using Serilog.Events;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration) // Read configuration settings from built-in IConfiguration
        .ReadFrom.Services(services) // Read out current app's services and make them available to serilog
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId();
});

// Log startup
Log.Information("Starting Bazario API...");

try
{
    builder.Services.ConfigureServices(builder.Configuration, builder.Environment);

    var app = builder.Build();

    // Configure the HTTP request pipeline.

    // 1. HTTPS Redirection (BEFORE routing and static files)
    app.UseHttpsRedirection();

    // 2. Static Files
    app.UseStaticFiles();

    // 3. Request Logging Middleware
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("UserName", httpContext.User?.Identity?.Name ?? "anonymous");
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString() ?? "unknown");
            diagnosticContext.Set("IPAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
    });

    // 4. Routing
    app.UseRouting();

    // 5. Authentication & Authorization
    app.UseAuthentication(); // Reading Identity cookie from the browser.
    app.UseAuthorization(); // Checking if the user is authorized to access the resource.

    // 6. Endpoints
    app.MapControllers(); // Execute the filter pipeline (action + filter)

    // 7. Development tools (can be anywhere, but usually at end)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        Log.Information("Swagger UI enabled for development environment");
    }

    Log.Information("Bazario API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Bazario API failed to start");
}
finally
{
    Log.CloseAndFlush();
}

