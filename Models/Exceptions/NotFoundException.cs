namespace LMS_Backend.Models.Exceptions;

/// <summary>
/// Represents a resource that could not be found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}

