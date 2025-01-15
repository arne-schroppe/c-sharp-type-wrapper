using TypeWrapperSourceGenerator;

namespace Types
{

    [TypeWrapper(typeof(string))]
    public readonly partial struct UserId
    {
    }

    public interface ITimeOrigin {}
    public interface Server : ITimeOrigin {}
    public interface Client : ITimeOrigin {}
    
    // [TypeWrapper(typeof(long), WrapperFeature.NewtonSoftJsonConverter)]
    // public readonly partial struct Timestamp<T> where T : ITimeOrigin
    // {
    // }
}