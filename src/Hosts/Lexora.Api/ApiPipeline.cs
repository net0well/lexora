using Lexora.Hosting.Health;

namespace Lexora.Api;

public static class ApiPipeline
{
    public static WebApplication UseApi(this WebApplication app)
    {
        app.MapControllers();
        app.MapHealthEndpoints();

        return app;
    }
}
