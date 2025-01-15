using System;
using System.Diagnostics;

namespace TypeWrapperSourceGenerator
{
    public class TypeWrapperAttribute : Attribute
    {
        public readonly Type WrappedType;
        public readonly TypeWrapperFeature Features;

        public TypeWrapperAttribute(Type wrappedType, TypeWrapperFeature features = TypeWrapperFeature.None)
        {
            Debug.Assert(wrappedType != null);
            Features = features;
        }
        
    }
}