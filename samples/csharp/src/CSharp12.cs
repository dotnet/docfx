namespace CSharp12;

using Markdown = string;

public class PrimaryConstructors
{
    public readonly struct Distance(double dx, double dy)
    {
        public readonly double Magnitude = Math.Sqrt(dx * dx + dy * dy);
        public readonly double Direction = Math.Atan2(dy, dx);
    }

    public class BankAccount(string accountID, string owner)
    {
        public string AccountID { get; } = accountID;
        public string Owner { get; } = owner;

        public override string ToString() => $"Account ID: {AccountID}, Owner: {Owner}";
    }

    public class CheckAccount(string accountID, string owner, decimal overdraftLimit = 0) : BankAccount(accountID, owner)
    {
        public decimal CurrentBalance { get; private set; } = 0;

        public void Deposit(decimal amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive");
            }
            CurrentBalance += amount;
        }

        public void Withdrawal(decimal amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Withdrawal amount must be positive");
            }
            if (CurrentBalance - amount < -overdraftLimit)
            {
                throw new InvalidOperationException("Insufficient funds for withdrawal");
            }
            CurrentBalance -= amount;
        }
        
        public override string ToString() => $"Account ID: {AccountID}, Owner: {Owner}, Balance: {CurrentBalance}";
    }
}

public class CollectionExpressions
{
    static CollectionExpressions()
    {
        Span<int> b = ['a', 'b', 'c', 'd', 'e', 'f', 'h', 'i'];
    }

    public static int[] a = [1, 2, 3, 4, 5, 6, 7, 8];

    public static int[][] twoD = [[1, 2, 3], [4, 5, 6], [7, 8, 9]];
}

public class DefaultLambdaParameters
{
    public void Foo()
    {
        var addWithDefault = (int addTo = 2) => addTo + 1;
        addWithDefault(); // 3
        addWithDefault(5); // 6

        var counter = (params int[] xs) => xs.Length;
        counter(); // 0
        counter(1, 2, 3); // 3
    }
}

[System.Runtime.CompilerServices.InlineArray(10)]
public struct InlineArrays
{
    private int _element0;
}

public class RefReadOnlyParameters
{
    public void Foo(ref readonly int bar) { }
}
