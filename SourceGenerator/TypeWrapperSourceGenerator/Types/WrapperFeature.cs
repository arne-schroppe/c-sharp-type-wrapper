using System;

namespace TypeWrapperSourceGenerator
{
    [Flags]
    public enum WrapperFeature
    {
        None = 0,
        SystemTextJsonConverter = 1,
        NewtonSoftJsonConverter = 2,
    }
}