namespace ACARSServer.Exceptions;

public class MessageParseException(string message, Exception innerException)
    : Exception(message, innerException);