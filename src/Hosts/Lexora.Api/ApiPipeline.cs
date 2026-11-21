namespace Lexora.Api;

public static class ApiPipeline
{
    public static WebApplication UseApi(this WebApplication app)
    {
        app.MapControllers();

        return app;
    }
}
