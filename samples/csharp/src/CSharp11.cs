namespace CSharp11;

public class StaticAbstractMembersInInterfaces
{
    public interface IGetNext<T> where T : IGetNext<T>
    {
        static abstract T operator ++(T other);
    }

    public struct RepeatSequence : IGetNext<RepeatSequence>
    {
        private const char Ch = 'A';
        public string Text = new string(Ch, 1);

        public RepeatSequence() { }

        public static RepeatSequence operator ++(RepeatSequence other)
            => other with { Text = other.Text + Ch };

        public override string ToString() => Text;
    }
}

public interface CheckedUserDefinedOperators<T> where T : CheckedUserDefinedOperators<T>
{
    static abstract T operator ++(T x);
    static abstract T operator --(T x);
    static abstract T operator -(T x);
    static abstract T operator +(T lhs, T rhs);
    static abstract T operator -(T lhs, T rhs);
    static abstract T operator *(T lhs, T rhs);
    static abstract T operator /(T lhs, T rhs);
    static abstract explicit operator int(T x);

    static abstract T operator checked ++(T x);
    static abstract T operator checked --(T x);
    static abstract T operator checked -(T x);
    static abstract T operator checked +(T lhs, T rhs);
    static abstract T operator checked -(T lhs, T rhs);
    static abstract T operator checked *(T lhs, T rhs);
    static abstract T operator checked /(T lhs, T rhs);
    static abstract explicit operator checked int(T x);
}

public class RequiredModifier
{
    public required string FirstName { get; init; }
    public string MiddleName { get; init; } = "";
    public required string LastName { get; init; }
}

file class FileScopedType
{

}

public class ScopedModifier
{
    public Span<int> CreateSpan(scoped ref int parameter) => throw new NotImplementedException();
}
