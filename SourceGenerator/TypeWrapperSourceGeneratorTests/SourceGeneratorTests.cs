using TypeWrapperSourceGenerator;

namespace TypeWrapperSourceGeneratorTests
{
    [TypeWrapper(typeof(int))]
    partial struct WrappedInt
    {
    }
    
    [TypeWrapper(typeof(int))]
    partial struct OtherWrappedInt
    {
    }
    
    [TypeWrapper(typeof(string))]
    partial struct WrappedString
    {
    }
    
    
    [TypeWrapper(typeof(RefType))]
    partial struct WrappedRefType
    {
    }

    class RefType : IEquatable<RefType>
    {
        private readonly int _value;
        private readonly int _hashCode;

        public RefType(int value, int? hashCode = null)
        {
            _value = value;
            _hashCode = hashCode ?? value.GetHashCode();
        }


        public bool Equals(RefType? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return _value == other._value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RefType)obj);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }

    public class SourceGeneratorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void It_generates_a_type_wrapped_int()
        {
            // Given
            WrappedInt wrappedInt = new(123);
            WrappedString wrappedString = new("hello");

            // Then
            Assert.That(wrappedInt.Value, Is.EqualTo(123));
            Assert.That(wrappedString.Value, Is.EqualTo("hello"));
        }

        [Test]
        public void It_is_equal_to_another_instance_with_the_same_value()
        {
            // Given  
            WrappedInt wrapped = new(123);
            WrappedInt wrapped2 = new(123);
            WrappedInt wrapped3 = new(124);
            
            // Then
            Assert.That(wrapped, Is.EqualTo(wrapped2));
            Assert.That(wrapped, Is.Not.EqualTo(wrapped3));
            
            Assert.That(wrapped == wrapped2);
            Assert.That(wrapped != wrapped3);
        }
        
        [Test]
        public void It_is_not_equal_to_another_type_with_the_same_value()
        {
            // Given  
            WrappedInt wrapped = new(123);
            OtherWrappedInt wrapped2 = new(123);
            
            // Then
            // ReSharper disable once SuspiciousTypeConversion.Global
            // ReSharper disable once UsageOfDefaultStructEquality
            Assert.That(wrapped.Equals(wrapped2), Is.False);
        }
        
        [Test]
        public void It_compares_reference_types_using_their_equality_methods()
        {
            // Given  
            WrappedRefType wrapped = new(new RefType(123));
            WrappedRefType wrapped2 = new(new RefType(123));
            WrappedRefType wrapped3 = new(new RefType(124));
            
            // Then
            Assert.That(wrapped, Is.EqualTo(wrapped2));
            Assert.That(wrapped, Is.Not.EqualTo(wrapped3));
        }
        
        [Test]
        public void It_returns_the_underlying_types_hashcode()
        {
            // Given  
            WrappedRefType wrapped = new(new RefType(123, 456));
            
            // Then
            Assert.That(wrapped.GetHashCode(), Is.EqualTo(456));
        }

        
    }
}