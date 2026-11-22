using Lexora.Api.OpenApi;
using Lexora.Hosting.Health;

namespace Lexora.Api;

public static class ApiPipeline
{
    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseExceptionHandler(_ => { });

        if (app.Configuration.GetValue<bool>(SwaggerSettings.EnabledKey))
        {
            UseSwagger(app);
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthEndpoints();

        return app;
    }

    private static void UseSwagger(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in app.DescribeApiVersions())
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
            }
        });
    }
}
