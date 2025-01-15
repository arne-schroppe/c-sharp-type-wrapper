using Newtonsoft.Json;
using TypeWrapperSourceGenerator;

namespace TypeWrapperSourceGeneratorTests
{
    [TypeWrapper(typeof(int))]
    readonly partial struct WrappedInt
    {
    }

    [TypeWrapper(typeof(int))]
    readonly partial struct OtherWrappedInt
    {
    }

    [TypeWrapper(typeof(string))]
    readonly partial struct WrappedString
    {
    }

    [TypeWrapper(typeof(RefType))]
    readonly partial struct WrappedRefType
    {
    }

    [TypeWrapper(typeof(int), TypeWrapperFeature.NewtonSoftJsonConverter)]
    readonly partial struct WrappedJsonInt
    {
    }

    [TypeWrapper(typeof(string), TypeWrapperFeature.NewtonSoftJsonConverter)]
    readonly partial struct WrappedJsonString
    {
    }

    enum SomeEnum
    {
        Red,
        Green,
        Blue,
    }
    
    [TypeWrapper(typeof(SomeEnum), TypeWrapperFeature.NewtonSoftJsonConverter)]
    readonly partial struct WrappedEnum
    {
    }
    
    [TypeWrapper(typeof(int))]
    readonly partial struct GenericWrappedInt<T> where T : struct
    {
    }
    
    [TypeWrapper(typeof(string))]
    readonly partial struct GenericWrappedString<T> where T : struct
    {
    }
    

    partial class SomeClass
    {
        [TypeWrapper(typeof(int))]
        public readonly partial struct ClassWrappedInt
        {
        }
        
        [TypeWrapper(typeof(string))]
        public readonly partial struct ClassWrappedString
        {
        }

        public partial class SomeClass2
        {
            [TypeWrapper(typeof(string))]
            public readonly partial struct DoubleClassWrappedString
            {
            }
        }
    }

    [Serializable]
    struct SerializableStruct
    {
        public Dictionary<WrappedJsonInt, int> IntDictionary;
        public Dictionary<WrappedJsonString, int> StringDictionary;
        public Dictionary<WrappedEnum, int> EnumDictionary;
    }

    public class SourceGeneratorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void It_generates_a_type_wrapped_value()
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
            WrappedRefType wrapped = new(new RefType(123, 1));
            WrappedRefType wrapped2 = new(new RefType(123, 2));
            WrappedRefType wrapped3 = new(new RefType(124, 1));

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
        
        [Test]
        public void It_generates_a_type_wrapper_inside_a_class()
        {
            // Given
            SomeClass.ClassWrappedInt wrappedInt = new(123);
            SomeClass.ClassWrappedString wrappedString = new("hello");
            SomeClass.SomeClass2.DoubleClassWrappedString wrappedString2 = new("hello2");

            // Then
            Assert.That(wrappedInt.Value, Is.EqualTo(123));
            Assert.That(wrappedString.Value, Is.EqualTo("hello"));
            Assert.That(wrappedString2.Value, Is.EqualTo("hello2"));
        }

        [Test]
        public void It_serializes_to_json_and_back_using_newtonsoft_json()
        {
            // Given  
            WrappedJsonInt wrapped = new(123);
            WrappedJsonString wrapped2 = new("test");

            // When
            string jsonInt = JsonConvert.SerializeObject(wrapped);
            WrappedJsonInt deserializedInt = JsonConvert.DeserializeObject<WrappedJsonInt>(jsonInt);
            string jsonString = JsonConvert.SerializeObject(wrapped2);
            WrappedJsonString deserializedString = JsonConvert.DeserializeObject<WrappedJsonString>(jsonString);

            // Then
            Assert.That(deserializedInt, Is.EqualTo(wrapped));
            Assert.That(jsonInt, Is.EqualTo("123"));

            Assert.That(deserializedString, Is.EqualTo(wrapped2));
            Assert.That(jsonString, Is.EqualTo("\"test\""));
        }
        
        [Test]
        public void Wrapped_values_can_be_used_as_dictionary_keys()
        {
            // Given  
            Dictionary<WrappedInt, int> intDictionary = new();
            Dictionary<WrappedString, int> stringDictionary = new();
            Dictionary<WrappedEnum, int> enumDictionary = new();
            WrappedInt wrapped = new(123);
            WrappedString wrapped2 = new("test");
            WrappedEnum wrapped3 = new(SomeEnum.Blue);

            // When
            intDictionary[wrapped] = 111;
            stringDictionary[wrapped2] = 222;
            enumDictionary[wrapped3] = 333;

            // Then
            Assert.That(intDictionary[wrapped], Is.EqualTo(111));
            Assert.That(stringDictionary[wrapped2], Is.EqualTo(222));
            Assert.That(enumDictionary[wrapped3], Is.EqualTo(333));
        }
        
        [Test]
        public void Wrapped_values_can_be_used_as_dictionary_keys_when_serializing_to_json()
        {
            // Given  
            SerializableStruct serializableStruct = new();
            serializableStruct.IntDictionary = new();
            serializableStruct.StringDictionary = new();
            serializableStruct.EnumDictionary = new();
            WrappedJsonInt wrapped = new(123);
            WrappedJsonString wrapped2 = new("test");
            WrappedEnum wrapped3 = new(SomeEnum.Blue);
            
            serializableStruct.IntDictionary[wrapped] = 111;
            serializableStruct.StringDictionary[wrapped2] = 222;
            serializableStruct.EnumDictionary[wrapped3] = 333;

            // When
            string serialized = JsonConvert.SerializeObject(serializableStruct);
            SerializableStruct deserialized = JsonConvert.DeserializeObject<SerializableStruct>(serialized);

            // Then
            Assert.That(deserialized.IntDictionary[wrapped], Is.EqualTo(111));
            Assert.That(deserialized.StringDictionary[wrapped2], Is.EqualTo(222));
            Assert.That(deserialized.EnumDictionary[wrapped3], Is.EqualTo(333));
        }
        
        [Test]
        public void It_generates_a_type_wrapped_value_where_the_wrapper_is_generic()
        {
            // Given
            GenericWrappedInt<int> wrappedInt = new(123);
            GenericWrappedString<int> wrappedString = new("hello");

            // Then
            Assert.That(wrappedInt.Value, Is.EqualTo(123));
            Assert.That(wrappedString.Value, Is.EqualTo("hello"));
        }
    }
}