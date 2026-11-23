using Testcontainers.RabbitMq;

namespace Lexora.TestKit.Containers;

public static class RabbitMqContainerFactory
{
    public const string Image = "rabbitmq:4-management";

    public static RabbitMqContainer Create() =>
        new RabbitMqBuilder(Image)
            .WithUsername("lexora")
            .WithPassword("lexora-tests")
            .Build();
}
