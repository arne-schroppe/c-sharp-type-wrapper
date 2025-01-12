using System;

namespace TypeWrapperSourceGenerator
{
    [Flags]
    public enum WrapperFeature
    {
        None = 0,
        NewtonSoftJsonConverter = 1,
    }
}