using System.Text;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;

namespace GoodStuff.ApiGateway.Extensions;

public static class GatewayExtensions
{
    public static WebApplicationBuilder AddGatewayConfiguration(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        var azureAd = builder.Configuration.GetSection("AzureAd");
        var kvUrl = azureAd["KvUrl"];
        if (string.IsNullOrWhiteSpace(kvUrl))
        {
            throw new InvalidOperationException("AzureAd:KvUrl is not configured.");
        }

        builder.Configuration.AddAzureKeyVault(new Uri(kvUrl), new DefaultAzureCredential());

        return builder;
    }

    public static IServiceCollection AddGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOcelot();
        services.AddOpenApi();

        var keyValue = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(keyValue))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        services
            .AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Token))
                        {
                            var token = context.Request.Cookies["access_token"];
                            if (!string.IsNullOrWhiteSpace(token))
                            {
                                context.Token = token;
                            }
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization();

        return services;
    }

    public static WebApplication UseGatewayPipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
