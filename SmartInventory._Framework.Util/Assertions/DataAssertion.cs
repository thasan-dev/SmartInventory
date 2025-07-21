
using InvalidDataException = SmartInventory._Framework.Util.Exceptions.BusinessException.InvalidDataException;

namespace SmartInventory._Framework.Util.Assertions;

public static class DataAssertion
{
    public static void IsTrue(
        bool condition,
        string? userErrorMessage = null,
        string? memberName = null,
        string? filePath = null,
        int lineNumber = 0)
    {
        if (!condition)
            throw new InvalidDataException($"Condition is false at {memberName}, {filePath}:{lineNumber} : {userErrorMessage}", userErrorMessage);
    }
            
}