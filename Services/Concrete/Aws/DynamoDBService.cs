using System.Text.Json;
using Amazon.DynamoDBv2.DocumentModel;
using Common.Constants;
using Common.Exceptions;
using Common.Helpers;
using Common.Models.Entities;
using Microsoft.Extensions.Logging;
using Services.Interfaces.Aws;

namespace Services.Concrete.Aws
{
    public class DynamoDBService : IDynamoDBService
    {
        private readonly ILogger<DynamoDBService> _logger;
        private readonly IDynamoDbRepository _dynamoDbRepository;
        public DynamoDBService(IDynamoDbRepository dynamoDbRepository, ILogger<DynamoDBService> logger)
        {
            _logger = logger;
            _dynamoDbRepository = dynamoDbRepository;
        }

        public async Task<PostEntity> SavePostAsync(PostEntity postEntity)
        {
            postEntity.PK = DynamoDBHelper.GetPK(postEntity.Id);
            postEntity.SK = DynamoDBHelper.GetPK(postEntity.Id);
            postEntity.GsiPk = DynamoDBHelper.GetGSIPk();

            await _dynamoDbRepository.SaveAsync(postEntity);
            _logger.LogInformation($"Successfully saved post with ID {postEntity.Id} to DynamoDB");
            return postEntity;
        }

        public async Task<(List<PostEntity>, string)> GetPostsAsync(string? cursor, int? limit)
        {
            var expressionAttributeValues = new Dictionary<string, DynamoDBEntry>();
            expressionAttributeValues.Add(":value", DynamoDBHelper.GetGSIPk());

            var queryOperationConfig = new QueryOperationConfig
            {
                IndexName = DynamoDBConstants.GSI_IndexName,
                Limit = limit ?? 10,
                PaginationToken = cursor,
                BackwardSearch = true,
                KeyExpression = new Expression
                {
                    ExpressionStatement = $"{DynamoDBConstants.GSI_PK} = :value",
                    ExpressionAttributeValues = expressionAttributeValues
                }
            };

            if (!string.IsNullOrEmpty(cursor))
            {
                queryOperationConfig.PaginationToken =
                    JsonSerializer.Deserialize<string>(Convert.FromBase64String(cursor));
            }

            var asyncSearchResponse = _dynamoDbRepository.QueryAsync<PostEntity>(queryOperationConfig);

            var posts = await asyncSearchResponse.GetNextSetAsync();

            var paginationToken = asyncSearchResponse.PaginationToken == "{}"
                ? null
                : Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(asyncSearchResponse.PaginationToken));

            return (posts, paginationToken);
        }

        public async Task<CommentEntity> SaveCommentAsync(CommentEntity commentEntity)
        {
            var pk = DynamoDBHelper.GetPK(commentEntity.PostId);
            var sk = DynamoDBHelper.GetSK(commentEntity.Id, commentEntity.CreatedAt);
            var post = await _dynamoDbRepository.LoadAsync<PostEntity>(hashKey: pk, rangeKey: pk);

            if (post == null)
            {
                throw new EntityNotFoundException(nameof(PostEntity), commentEntity.PostId);
            }
            
            post.SecondLastComment = post.LastComment;
            post.LastComment = commentEntity;
            post.CommentCount += 1;

            var postTransactWrite = _dynamoDbRepository.CreateTransactWrite<PostEntity>();
            postTransactWrite.AddSaveItem(post);

            var commentTransactWrite = _dynamoDbRepository.CreateTransactWrite<CommentEntity>();

            commentEntity.PK = pk;
            commentEntity.SK = sk;
            commentTransactWrite.AddSaveItem(commentEntity);

            await _dynamoDbRepository.ExecuteTransactWriteAsync(postTransactWrite, commentTransactWrite);

            return commentEntity;
        }

        public async Task DeleteCommentAsync(Guid commentId, Guid postId, Guid creatorId, DateTime createdAt)
        {
            var pk = DynamoDBHelper.GetPK(postId);
            var sk = DynamoDBHelper.GetSK(commentId, createdAt);
            var post = await _dynamoDbRepository.LoadAsync<PostEntity>(pk, pk);
            if (post == null)
            {
                throw new EntityNotFoundException(nameof(PostEntity), postId);
            }
            var commentToDelete = await _dynamoDbRepository.LoadAsync<CommentEntity>(pk, sk);
            if (commentToDelete == null)
            {
                throw new EntityNotFoundException(nameof(CommentEntity), commentId);
            }

            if (commentToDelete.Creator != creatorId)
            {
                throw new UnauthorizedUserException("User is not authorized to delete a comment");
            }

            var expressionAttributeValues = new Dictionary<string, DynamoDBEntry>();
            expressionAttributeValues.Add(":value", pk);
            expressionAttributeValues.Add(":commentPrefix", $"{DynamoDBConstants.CommentPrefix}#");

            var queryOperationConfig = new QueryOperationConfig
            {
                Limit = 3,
                BackwardSearch = true,
                KeyExpression = new Expression
                {
                    ExpressionStatement = $"PK = :value AND begins_with(SK, :commentPrefix)",
                    ExpressionAttributeValues = expressionAttributeValues
                }
            };

            var asyncSearchResponse = _dynamoDbRepository.QueryAsync<CommentEntity>(queryOperationConfig);

            var lastThreeComments = await asyncSearchResponse.GetRemainingAsync();

            CommentEntity? newLastComment = null;
            CommentEntity? newSecondLastComment = null;

            foreach (var comment in lastThreeComments)
            {
                if (comment.Id == commentToDelete.Id)
                {
                    continue;
                }

                if (newLastComment == null)
                {
                    newLastComment = comment;
                }
                else if (newSecondLastComment == null)
                {
                    newSecondLastComment = comment;
                }

                if (newLastComment != null && newSecondLastComment != null)
                {
                    break;
                }
            }

            post.LastComment = newLastComment!;
            post.SecondLastComment = newSecondLastComment!;
            post.CommentCount -= 1;

            var postTransactWrite = _dynamoDbRepository.CreateTransactWrite<PostEntity>();
            postTransactWrite.AddSaveItem(post);

            var commentTransactWrite = _dynamoDbRepository.CreateTransactWrite<CommentEntity>();

            commentTransactWrite.AddDeleteItem(commentToDelete);
            
            await _dynamoDbRepository.ExecuteTransactWriteAsync(postTransactWrite, commentTransactWrite);
        }
    }
}