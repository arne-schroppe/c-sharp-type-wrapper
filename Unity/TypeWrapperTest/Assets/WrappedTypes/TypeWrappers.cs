using TypeWrapperSourceGenerator;

namespace Types
{
    [TypeWrapper(typeof(string))]
    public readonly partial struct UserId
    {
    }

    [TypeWrapper(typeof(int), Feature.Serializable)]
    public partial struct SerializableWrappedInt
    {
    }

    [TypeWrapper(typeof(string), Feature.Serializable)]
    public partial struct SerializableWrappedString
    {
    }
}