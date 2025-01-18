using System;

namespace TypeWrapperSourceGenerator
{
    [Flags]
    public enum Feature
    {
        None = 0,
        NewtonSoftJsonConverter = 1,
        Serializable = 2
    }
}