using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SawyerWebApiCtlrs.Middleware;

public class JwtAuthentication
{
    public const string AuthScheme = "jwt";

    public static void Add(
        IHostApplicationBuilder builder)
    {
        Settings settings = new();
        builder.Configuration
            .GetRequiredSection("Middleware:JwtAuthentication")
            .Bind(settings);
        Validator.ValidateObject(
            instance: settings,
            validationContext: new ValidationContext(settings),
            validateAllProperties: true);

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(AuthScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    ValidAlgorithms = settings.Algorithms,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.IssuerSigningKey)),
                    ValidateSignatureLast = true,
                    ClockSkew = TimeSpan.FromMinutes(settings.ClockSkewSec),
                    // Default is (for some reason): "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
                    NameClaimType = "name",
                };

                // Otherwise, sub claim will be changed to http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
                options.MapInboundClaims = false;

#if !DEBUG
                options.RequireHttpsMetadata = true;
#endif
            });
    }

    public static void SetupSwaggerGen(
        SwaggerGenOptions options)
    {
        options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
        {
            Description = "JWT authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "bearerAuth",
                    }
                },
                new string[] { }
            }
        });
    }

    private class Settings
    {
        [Required]
        public string Issuer { get; set; } = "";

        [Required]
        public string Audience { get; set; } = "";

        [Required]
        [MinLength(1)]
        public List<string> Algorithms { get; set; } = [];

        [Required]
        public string IssuerSigningKey { get; set; } = "";

        [Range(1, 60 * 3)]
        public int ClockSkewSec { get; set; }
    }
}