using System.Diagnostics.CodeAnalysis;
using ViewGenerator.Common;
using ViewGenerator.Engine;

namespace ViewGenerator.App;

static class Validators
{
    public static void ValidateViewGenerator([NotNull] this Generator? viewGenerator)
    {
        if (viewGenerator is null)
        {
            throw new Exception($"View generator is null at this point.");
        }
    }
}
