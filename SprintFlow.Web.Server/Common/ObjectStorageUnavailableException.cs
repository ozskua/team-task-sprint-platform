namespace SprintFlow.Web.Server.Common;

public class ObjectStorageUnavailableException : Exception
{
    public ObjectStorageUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
