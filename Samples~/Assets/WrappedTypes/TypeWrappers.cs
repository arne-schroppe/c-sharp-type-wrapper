using TypeWrapperGenerator;

namespace Types
{
    [TypeWrapper(typeof(string))]
    public readonly partial struct UserId
    {
    }

    [TypeWrapper(typeof(int), Feature.UnitySerializable)]
    public partial struct SerializableWrappedInt
    {
    }

    [TypeWrapper(typeof(string), Feature.UnitySerializable)]
    public partial struct SerializableWrappedString
    {
    }
}