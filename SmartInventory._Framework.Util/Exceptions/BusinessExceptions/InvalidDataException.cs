namespace SmartInventory._Framework.Util.Exceptions.BusinessExceptions;

public class InvalidDataException : BusinessException
{
    private const string DefaultUserErrorMessage = "Input data validation failed.";

    /// <summary>
    /// New instance.
    /// </summary>
    public InvalidDataException()
        : base(DefaultUserErrorMessage, DefaultUserErrorMessage)
    {
    }

    /// <summary>
    /// New instance.
    /// </summary>
    /// <param name="logErrorMessage">The error message to be logged.</param>
    /// <param name="userErrorMessage">The error message to be returned to client/end user.</param>
    public InvalidDataException(string logErrorMessage, string? userErrorMessage = null)
        : base(logErrorMessage, userErrorMessage ?? DefaultUserErrorMessage)
    {
    }
}