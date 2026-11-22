using JasperFx.CommandLine;
using Lexora.Application.Auth.SeedAdmin;
using Lexora.Application.Auth.Services;
using Lexora.Domain.Abstractions;
using Microsoft.Extensions.Options;

namespace Lexora.Api.CommandLine;

[Description("Creates the first administrator from the Seed:Admin settings", Name = "seed-admin")]
public sealed class SeedAdminCliCommand : JasperFxAsyncCommand<SeedAdminInput>
{
    public override async Task<bool> Execute(SeedAdminInput input)
    {
        using var host = input.BuildHost();
        await using var scope = host.Services.CreateAsyncScope();

        var settings = scope.ServiceProvider.GetRequiredService<IOptions<SeedAdminOptions>>().Value;
        var service = scope.ServiceProvider.GetRequiredService<ISeedAdminService>();

        var result = await service.SeedAsync(new SeedAdminCommand(settings.Email, settings.Name, settings.Password), CancellationToken.None);

        if (result.IsFailure)
        {
            foreach (var notification in result.Notifications)
            {
                await Console.Error.WriteLineAsync($"{notification.Key}: {notification.Message}");
            }

            return false;
        }

        if (!result.Value.Created)
        {
            Console.WriteLine("Admin already exists.");
            return true;
        }

        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);
        Console.WriteLine("Admin created.");

        return true;
    }
}
