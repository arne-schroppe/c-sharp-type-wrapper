namespace TypeWrapperSourceGeneratorTests;

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