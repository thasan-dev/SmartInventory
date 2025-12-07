namespace SmartInventory._Framework.Util.Exceptions.BusinessExceptions;

/// <summary>
/// Base class for all BusinessExceptions in the system.
/// </summary>
public abstract class BusinessException : ApplicationException
{
    private const string DefaultUserErrorMessage = "Business data/logic failed.";

    /// <summary>
    /// The error message which can be returned to the client
    /// and displayed to a user.
    /// </summary>
    public string UserErrorMessage { get; }

    /// <summary>
    /// New instance.
    /// </summary>
    protected BusinessException()
        : base("Business exception")
    {
        UserErrorMessage = DefaultUserErrorMessage;
    }

    /// <summary>
    /// New instance.
    /// </summary>
    /// <param name="logErrorMessage">The error message which is logged.</param>
    protected BusinessException(string logErrorMessage)
        : base(logErrorMessage)
    {
        UserErrorMessage = DefaultUserErrorMessage;
    }

    /// <summary>
    /// New instance.
    /// </summary>
    /// <param name="logErrorMessage">The error message which is logged.</param>
    /// <param name="userErrorMessage">The error message which can be returned to a client/end user.</param>
    protected BusinessException(string logErrorMessage, string userErrorMessage)
        : base(logErrorMessage)
    {
        UserErrorMessage = userErrorMessage;
    }

    /// <summary>
    /// New instance
    /// </summary>
    /// <param name="logErrorMessage">The error message which is logged.</param>
    /// <param name="innerException">The exception to be logged.</param>
    protected BusinessException(string logErrorMessage, Exception innerException)
        : base(logErrorMessage, innerException)
    {
        UserErrorMessage = DefaultUserErrorMessage;
    }

    /// <summary>
    /// New instance
    /// </summary>
    /// <param name="logErrorMessage">The error message which is logged.</param>
    /// <param name="userErrorMessage">The error message which can be returned to a client/end user.</param>
    /// <param name="innerException">The exception to be logged.</param>
    protected BusinessException(string logErrorMessage, string userErrorMessage, Exception innerException)
        : base(logErrorMessage, innerException)
    {
        UserErrorMessage = userErrorMessage;
    }
}