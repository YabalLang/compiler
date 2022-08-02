using System.Diagnostics.CodeAnalysis;

namespace Astro8;

public readonly struct Either<TLeft, TRight> : IEquatable<Either<TLeft, TRight>>
    where TLeft : notnull
    where TRight : notnull
{
    private Either(TLeft left)
    {
        Left    = left;
        Right   = default;
        IsRight = false;
    }

    private Either(TRight right)
    {
        Left    = default;
        Right   = right;
        IsRight = true;
    }

    [MemberNotNullWhen(true, nameof(Right))]
    [MemberNotNullWhen(false, nameof(Left))]
    public bool IsRight { get; }

    public TRight? Right { get; }

    [MemberNotNullWhen(true, nameof(Left))]
    [MemberNotNullWhen(false, nameof(Right))]
    public bool IsLeft => !IsRight;

    public TLeft? Left { get; }

    public static implicit operator Either<TLeft, TRight>(TLeft left) => new(left);
    public static implicit operator Either<TLeft, TRight>(TRight right) => new(right);

    public override string? ToString()
    {
        return IsRight ? Right.ToString() : Left.ToString();
    }

    public bool Equals(Either<TLeft, TRight> other)
    {
        return IsRight == other.IsRight && EqualityComparer<TRight?>.Default.Equals(Right, other.Right) && EqualityComparer<TLeft?>.Default.Equals(Left, other.Left);
    }

    public override bool Equals(object? obj)
    {
        return obj is Either<TLeft, TRight> other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = IsRight.GetHashCode();
            hashCode = (hashCode * 397) ^ (Right is null ? 0 : EqualityComparer<TRight?>.Default.GetHashCode(Right));
            hashCode = (hashCode * 397) ^ (Left is null ? 0 : EqualityComparer<TLeft?>.Default.GetHashCode(Left));
            return hashCode;
        }
    }

    public static bool operator ==(Either<TLeft, TRight> left, Either<TLeft, TRight> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Either<TLeft, TRight> left, Either<TLeft, TRight> right)
    {
        return !left.Equals(right);
    }
}
