using System.Reflection;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.OpenApi.Models;
using SawyerWebApiCtlrs.HealthChecks;
using SawyerWebApiCtlrs.Middleware;
using Serilog;

// todo: get template params working to determine middlewares
//      https://github.com/dotnet/templating/wiki/Reference-for-template.json#parameter-symbol
//      https://github.com/dotnet/templating/wiki/Using-Primary-Outputs-for-Post-Actions
//          https://github.com/dotnet/templating/wiki/Post-Action-Registry
//      https://github.com/dotnet/templating/wiki/Conditional-processing-and-comment-syntax
//      look at .NET's webapi template's --use-controllers

var builder = WebApplication.CreateBuilder(args);


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
