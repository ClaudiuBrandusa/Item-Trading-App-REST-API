namespace Application.Models.Common;

public class Result<T>
{
    public bool IsSuccess { get; init; }

    public string? Error { get; init; }

    public T? Content { get; init; }

    public Result(T content)
    {
        Content = content;
        IsSuccess = true;
    }

    public Result(string error)
    {
        Error = error;
        IsSuccess = false;
    }

    public static Result<T> Success(T value) => new(value);
    
    public static Result<T> Failure(string error) => new(error);
}

public class Result : Result<bool?>
{
    public Result() : base(false)
    {
    }

    public Result(string error) : base(error)
    {
    }

    public static Result Success() => new();

    public static new Result Failure(string error) => new(error);
}
