using TypeWrapperSourceGenerator;

namespace TypeWrapperSourceGeneratorTests
{
    
[TypeWrapper(typeof(int))]
partial struct WrappedInt { }

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

        // Then
    }
}
}
