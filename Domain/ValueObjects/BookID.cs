namespace Domain.ValueObjects;

public sealed record BookId
{
    public BookId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("BookId cannot be an empty GUID.", nameof(value));

        Value = value;
    }

    public Guid Value { get; }

    public static BookId New()
    {
        return new BookId(Guid.NewGuid());
    }

    public static BookId From(Guid value)
    {
        return new BookId(value);
    }

    public static bool TryParse(string input, out BookId? bookId)
    {
        bookId = null;
        if (!Guid.TryParse(input, out var guid) || guid == Guid.Empty) return false;
        bookId = new BookId(guid);
        return true;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}