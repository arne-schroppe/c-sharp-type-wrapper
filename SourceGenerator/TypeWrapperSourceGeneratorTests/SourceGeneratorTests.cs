using TypeWrapperSourceGenerator;

namespace TypeWrapperSourceGeneratorTests;

public class SourceGeneratorTests
{
    
    [TypeWrapper(typeof(int))]
    partial struct WrappedInt { }
    
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void It_generates_a_type_wrapped_int()
    {
        // Given
        //WrappedInt wrappedInt = new(123);

        // Then
    }
}