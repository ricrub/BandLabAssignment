using Common.Constants;

namespace Common.Helpers;

public static class DynamoDBHelper
{
    public static string GetPK(Guid postId) => $"{DynamoDBConstants.PostPrefix}#{postId}";
    public static string GetSK(Guid commentId, DateTime createdAt) => $"{DynamoDBConstants.CommentPrefix}#{createdAt:yyyy-MM-ddTHH:mm:ss.fffZ}#{commentId}";
    public static string GetGSIPk() => $"{DynamoDBConstants.GSIPrefix}#";
}