using TypeWrapperSourceGenerator;

namespace TypeWrapperSourceGeneratorTests
{
    [TypeWrapper(typeof(int))]
    partial struct WrappedInt
    {
    }
    
    [TypeWrapper(typeof(string))]
    partial struct WrappedString
    {
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
    }
}