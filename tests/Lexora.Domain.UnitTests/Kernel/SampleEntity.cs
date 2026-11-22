using Lexora.Shared.Domain;

namespace Lexora.Domain.UnitTests.Kernel;

internal sealed class SampleEntity : Entity
{
    public SampleEntity()
    {
    }

    public SampleEntity(Guid id) : base(id)
    {
    }
}
