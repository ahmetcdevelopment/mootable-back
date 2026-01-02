namespace Mootable.Application.Common.Responses;

/// <summary>
/// Generic service response wrapper
/// </summary>
public class ServiceResponse<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ServiceResponse<T> Success(T data, string message = "Operation successful")
    {
        return new ServiceResponse<T>
        {
            Succeeded = true,
            Data = data,
            Message = message
        };
    }

    public static ServiceResponse<T> Failure(string error)
    {
        return new ServiceResponse<T>
        {
            Succeeded = false,
            Errors = new List<string> { error }
        };
    }

    public static ServiceResponse<T> Failure(List<string> errors)
    {
        return new ServiceResponse<T>
        {
            Succeeded = false,
            Errors = errors
        };
    }
}

/// <summary>
/// Non-generic service response
/// </summary>
public class ServiceResponse
{
    public bool Succeeded { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ServiceResponse Success(string message = "Operation successful")
    {
        return new ServiceResponse
        {
            Succeeded = true,
            Message = message
        };
    }

    public static ServiceResponse Failure(string error)
    {
        return new ServiceResponse
        {
            Succeeded = false,
            Errors = new List<string> { error }
        };
    }

    public static ServiceResponse Failure(List<string> errors)
    {
        return new ServiceResponse
        {
            Succeeded = false,
            Errors = errors
        };
    }
}