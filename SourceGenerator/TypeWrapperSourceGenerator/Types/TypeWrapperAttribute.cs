using System;
using System.Diagnostics;

namespace TypeWrapperSourceGenerator
{
    public class TypeWrapperAttribute : Attribute
    {
        public readonly Type WrappedType;
        public readonly WrapperFeature Features;

        public TypeWrapperAttribute(Type wrappedType, WrapperFeature features = WrapperFeature.None)
        {
            Debug.Assert(wrappedType != null);
            Features = features;
        }
        
    }
}