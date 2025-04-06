using System;
using System.Diagnostics;

namespace TypeWrapper
{
    public class TypeWrapperAttribute : Attribute
    {
        public readonly Type WrappedType;
        public readonly Feature Features;

        public TypeWrapperAttribute(Type wrappedType, Feature features = Feature.None)
        {
            Debug.Assert(wrappedType != null);
            Features = features;
        }
    }
}