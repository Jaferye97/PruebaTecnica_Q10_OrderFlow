namespace Inventory.Domain.Common;

public class Result
{
    private readonly IReadOnlyList<Error> _errors;

    protected Result(IReadOnlyList<Error> errors) => _errors = errors;

    public bool IsSuccess => _errors.Count == 0;
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors => _errors;

    public static Result Success() => new([]);

    public static Result Failure(IEnumerable<Error> errors)
    {
        var list = errors.ToList();
        if (list.Count == 0)
            throw new ArgumentException("Un fallo debe tener al menos un error.", nameof(errors));
        return new Result(list);
    }

    public static Result Failure(Error error) => Failure([error]);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base([]) => _value = value;
    private Result(IReadOnlyList<Error> errors) : base(errors) { }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede leer Value de un Result fallido.");

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(IEnumerable<Error> errors)
    {
        var list = errors.ToList();
        if (list.Count == 0)
            throw new ArgumentException("Un fallo debe tener al menos un error.", nameof(errors));
        return new Result<T>(list);
    }

    public new static Result<T> Failure(Error error) => Failure([error]);
}
