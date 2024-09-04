namespace Common.Exceptions;

public class AwsS3PutObjectException : Exception
{
    public AwsS3PutObjectException(string message) : base(message) { }
    public AwsS3PutObjectException(string message, Exception innerException) : base(message, innerException) { }
}