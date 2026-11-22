using System.ComponentModel;

namespace Lexora.Domain.UnitTests.Kernel;

internal enum SampleColor
{
    Red = 1,

    [Description("Dark green")]
    Green = 2,

    Blue = 3
}
