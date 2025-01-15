using System;

namespace TypeWrapperSourceGenerator
{
    [Flags]
    public enum TypeWrapperFeature
    {
        None = 0,
        NewtonSoftJsonConverter = 1,
    }
}