using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Lexora.Api.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lexora.Api.IntegrationTests.OpenApi;

public sealed class ConfigureSwaggerOptionsTests
{
    [Fact]
    public void Configure_EachApiVersion_GetsItsOwnDocument()
    {
        var options = Configure(
            new ApiVersionDescription(new ApiVersion(1, 0), "v1", deprecated: true, sunsetPolicy: null, deprecationPolicy: null),
            new ApiVersionDescription(new ApiVersion(2, 0), "v2", deprecated: false, sunsetPolicy: null, deprecationPolicy: null));

        var documents = options.SwaggerGeneratorOptions.SwaggerDocs;
        documents.Keys.ShouldBe(["v1", "v2"], ignoreOrder: true);
        documents["v1"].Version.ShouldBe("1.0");
        documents["v1"].Description.ShouldNotBeNull().ShouldContain("deprecated");
        documents["v2"].Description.ShouldBeNull();
    }

    [Fact]
    public void Configure_BearerScheme_IsDeclaredAsHttpJwt()
    {
        var options = Configure(new ApiVersionDescription(new ApiVersion(1, 0), "v1", deprecated: false, sunsetPolicy: null, deprecationPolicy: null));

        var scheme = options.SwaggerGeneratorOptions.SecuritySchemes[ConfigureSwaggerOptions.BearerSchemeId];
        scheme.Type.ShouldBe(SecuritySchemeType.Http);
        scheme.Scheme.ShouldBe("bearer");
        scheme.BearerFormat.ShouldBe("JWT");
        options.SwaggerGeneratorOptions.SecurityRequirements.ShouldNotBeEmpty();
    }

    private static SwaggerGenOptions Configure(params ApiVersionDescription[] descriptions)
    {
        var options = new SwaggerGenOptions();
        new ConfigureSwaggerOptions(new StubApiVersionDescriptionProvider(descriptions)).Configure(options);

        return options;
    }
}
