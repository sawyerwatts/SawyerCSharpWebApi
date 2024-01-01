using System.Reflection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.OpenApi.Models;
using SawyerWebApiCtlrs.HealthChecks;
using SawyerWebApiCtlrs.Middleware;
using Serilog;

// todo: paging? page blobbing?

// todo: review servers, owasp, and logging notes

// todo: test template installation/usage

// todo: docs features in README, and move TODOs to TODO.md

var builder = WebApplication.CreateBuilder(args);

// TODO: Review the registered middleware to ensure it is appropriate for the
//      solution using this template (and pay special attention to the rate
//      limiting section).
//      Of note, this template has both JWT and API key middleware. Remove at
//      least one of them.

// TODO: Replace SampleHealthCheck with a real healthcheck
//      Additionally, MapHealthChecks could possibly have RequireHost instead
//      of (or in addition to) existing access control.

// TODO: Proxy (or replace) IdempotentPostsInMemoryCache with another,
//      persistent cache (if you aren't removing the IdempotentPosts
//      middleware). See that class for more.

// TODO: RequestSizeLimitAttribute doesn't seem to have middleware support, so
//      be careful when increasing the request timeout, and/or configure this
//      in a gateway.

// TODO: You likely want to ensure alerting around CPU, RAM, and billing are
//      configured.

// ----------------------------------------------------------------------------
// Non-middleware services
// -----------------------

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Services.AddSerilog();


// ----------------------------------------------------------------------------
// Middleware services
// -------------------
// Configuration should be completed at this point.

builder.Services.AddHealthChecks()
    .AddCheck<SampleHealthCheck>("Sample");

builder.Services.AddControllers(options =>
{
    // If request body cannot be formatted, ASP.NET Core will automatically
    // return 415; the following enables 406 when Accept's value doesn't have a
    // formatter (because otherwise JSON is returned regardless of the Accept).
    options.ReturnHttpNotAcceptable = true;
});

builder.Services
    .AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
    })
    .AddMvc();

builder.Services.AddScoped<ObfuscatePayloadOfServerErrors>();

TraceGuid.RegisterTo(builder);

IdempotentPosts.RegisterTo(builder);
IdempotentPostsInMemoryCache.RegisterTo(builder);

RequestTimeouts.Add(builder);
RateLimiting.Add(builder);

ApiKeyAuthenticationSchemeHandler.Add(builder);
// todo: uncomment this
//JwtAuthentication.Add(builder);

// Set the fallback/default authorization policy to requiring authenticated
// users. Add [AllowAnonymous] or [Authorize(PolicyName="MyPolicy")] to
// loosen/harden the authorization.
// Don't forget policies can require claims, optionally with specific values.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo()
    {
        Version = "v1",
        Title = "SawyerWebApiCtlrs",
        Description = "",
    });

    string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    IdempotentPosts.SetupSwaggerGen(options);

    ApiKeyAuthenticationSchemeHandler.SetupSwaggerGen(options);
    // todo: uncomment this
    //JwtAuthentication.SetupSwaggerGen(options);
});


// ----------------------------------------------------------------------------
// Request pipeline
// ----------------
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware#middleware-order
//      Note that lots of the built in middleware need to run in a specific
//      order, so deviate from that list with caution.

var app = builder.Build();
ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    app.Use(TraceGuid.Middleware);
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ObfuscatePayloadOfServerErrors>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseRequestTimeouts();

    app.UseAuthorization();
    app.Use(async (context, next) =>
    {
        string name = context.User.Identity?.Name ?? "`unknown`";
        string host = context.Request.Host.ToString();
        string url = context.Request.GetEncodedUrl();
        context.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogInformation(
                "Request made by {User} from {Host} for {Url}",
                name,
                host,
                url);

        await next(context);
    });

    app.UseRateLimiter();

    app.UseMiddleware<IdempotentPosts>();

    // ----------------------------------------------------------------------------
    // Routing and startup
    // -------------------

    app.MapControllers();
    app.MapHealthChecks("/_health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    });

    logger.LogInformation("Instantiating app services and running");
    app.Run();
    logger.LogInformation("App completed");
}
catch (Exception exc)
{
    logger.LogCritical(exc, "An unhandled exception occurred, the app has crashed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
