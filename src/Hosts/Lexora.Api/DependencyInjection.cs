using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Lexora.Api.Auth;
using Lexora.Api.ExceptionHandling;
using Lexora.Api.OpenApi;
using Lexora.Api.RateLimiting;
using Lexora.Api.Results;
using Microsoft.AspNetCore.Authorization;

namespace Lexora.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions))
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressMapClientErrors = true;
                options.InvalidModelStateResponseFactory = InvalidModelStateResponseFactory.Create;
            });
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        AddVersioning(services);

        services.AddSwaggerGen();
        services.ConfigureOptions<ConfigureSwaggerOptions>();

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, EnvelopeAuthorizationResultHandler>();

        services.AddExceptionHandler<InputValidationExceptionHandler>();
        services.AddExceptionHandler<UnhandledExceptionHandler>();

        AddRefreshTokenCookie(services);
        AddRateLimiting(services, configuration);

        return services;
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthRateLimitOptions>()
            .Bind(configuration.GetSection(AuthRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = RateLimitRejectionWriter.WriteAsync;
            options.AddPolicy(RateLimitPolicies.Login, context => ClientIpRateLimiter.Partition(context, limits => limits.LoginPermitsPerMinute));
            options.AddPolicy(RateLimitPolicies.Refresh, context => ClientIpRateLimiter.Partition(context, limits => limits.RefreshPermitsPerMinute));
        });
    }

    private static void AddVersioning(IServiceCollection services) =>
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

    private static void AddRefreshTokenCookie(IServiceCollection services)
    {
        services.AddOptions<RefreshTokenCookieOptions>()
            .Configure<IHostEnvironment>((options, environment) => options.Secure = !environment.IsDevelopment());

        services.AddSingleton<RefreshTokenCookie>();
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }
}
