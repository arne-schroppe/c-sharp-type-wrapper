using System;
using System.Diagnostics;

namespace TypeWrapperSourceGenerator
{
    public class TypeWrapperAttribute : Attribute
    {
        public readonly Type WrappedType;

        public TypeWrapperAttribute(Type wrappedType)
        {
            Debug.Assert(wrappedType != null);
            WrappedType = wrappedType;
        }
        
    }
}