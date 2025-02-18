using Stride.Core;
using Stride.Core.Reflection;

namespace SmartIOT.Stride.Library;

#pragma warning disable S1118 // this is used by Stride

internal class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
    }
}

#pragma warning restore S1118