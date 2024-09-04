using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Common.Exceptions;
using Common.Models.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Concrete.Aws;
using Services.Interfaces.Aws;
using Services.Wrappers;
using Tests;

public class DynamoDBServiceTests
{
    private readonly Mock<IDynamoDbRepository> _mockDbRepository;
    private readonly IDynamoDBService _dynamoDBService;

    public DynamoDBServiceTests()
    {
        _mockDbRepository = new Mock<IDynamoDbRepository>();
        _dynamoDBService = new DynamoDBService(_mockDbRepository.Object, Mock.Of<ILogger<DynamoDBService>>());
    }

    [Fact]
    public async Task SavePostAsync_ShouldSavePostSuccessfully()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var postEntity = new PostEntity { Id = postId };
        var expectedPk = $"POST#{postId}";
        var expectedGsiPk = "GSI#";

        // Act
        var result = await _dynamoDBService.SavePostAsync(postEntity);

        // Assert
        _mockDbRepository.Verify(x => x.SaveAsync(postEntity), Times.Once);
        Assert.Equal(expectedPk, postEntity.PK);
        Assert.Equal(expectedPk, postEntity.SK);
        Assert.Equal(expectedGsiPk, postEntity.GsiPk);
        Assert.Equal(postEntity, result);
    }

    [Fact]
    public async Task GetPostsAsync_ShouldReturnPostsAndPaginationToken()
    {
        // Arrange
        var posts = new List<PostEntity> { new PostEntity { Id = Guid.NewGuid() } };
        var paginationToken = "paginationToken";

        var mockAsyncSearch = new Mock<AsyncSearch<PostEntity>>();
        mockAsyncSearch.Setup(x => x.GetNextSetAsync(default)).ReturnsAsync(posts);
        mockAsyncSearch.Setup(x => x.PaginationToken).Returns(paginationToken);

        _mockDbRepository.Setup(x => x.QueryAsync<PostEntity>(It.IsAny<QueryOperationConfig>()))
            .Returns(mockAsyncSearch.Object);

        // Act
        var (resultPosts, resultToken) = await _dynamoDBService.GetPostsAsync(null, null);

        // Assert
        Assert.Equal(posts, resultPosts);
        Assert.Equal(Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(paginationToken)), resultToken);
    }

    [Fact]
    public async Task SaveCommentAsync_ShouldThrowEntityNotFoundException_WhenPostDoesNotExist()
    {
        // Arrange
        var commentEntity = new CommentEntity { PostId = Guid.NewGuid() };
        _mockDbRepository.Setup(x =>
                x.LoadAsync<PostEntity>(It.IsAny<object>(), It.IsAny<DynamoDBOperationConfig>()))
            .ReturnsAsync((PostEntity)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _dynamoDBService.SaveCommentAsync(commentEntity));
    }

    [Fact]
    public async Task SaveCommentAsync_ShouldSaveCommentSuccessfully()
    {
        // Arrange
        var newCommentEntity = new CommentEntity
            { PostId = Guid.NewGuid(), Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };

        var existingCommentEntity = new CommentEntity
            { PostId = Guid.NewGuid(), Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
        var postEntity = new PostEntity
        {
            Id = newCommentEntity.PostId,
            LastComment = existingCommentEntity
        };
        var pk = $"POST#{postEntity.Id}";
        var sk = $"COMMENT#{newCommentEntity.CreatedAt:yyyy-MM-ddTHH:mm:ss.fffZ}#{newCommentEntity.Id}";


        var mockPostTransactWrite = new Mock<ITransactWriteWrapper<PostEntity>>();
        var mockCommentTransactWrite = new Mock<ITransactWriteWrapper<CommentEntity>>();


        _mockDbRepository.Setup(x => x.LoadAsync<PostEntity>(pk, pk)).ReturnsAsync(postEntity);
        _mockDbRepository.Setup(x => x.CreateTransactWrite<PostEntity>()).Returns(mockPostTransactWrite.Object);
        _mockDbRepository.Setup(x => x.CreateTransactWrite<CommentEntity>()).Returns(mockCommentTransactWrite.Object);

        // Act
        var result = await _dynamoDBService.SaveCommentAsync(newCommentEntity);

        // Assert
        mockPostTransactWrite.Verify(x => x.AddSaveItem(postEntity), Times.Once);
        mockCommentTransactWrite.Verify(x => x.AddSaveItem(newCommentEntity), Times.Once);
        _mockDbRepository.Verify(
            x => x.ExecuteTransactWriteAsync(mockPostTransactWrite.Object, mockCommentTransactWrite.Object),
            Times.Once);
        Assert.Equal(newCommentEntity, result);
        Assert.Equal(sk, newCommentEntity.SK);
        Assert.Equal(1, postEntity.CommentCount);
        Assert.Equal(existingCommentEntity, postEntity.SecondLastComment);
        Assert.Equal(newCommentEntity, postEntity.LastComment);
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldThrowEntityNotFoundException_WhenPostDoesNotExist()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        _mockDbRepository.Setup(x => x.LoadAsync<PostEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync((PostEntity)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _dynamoDBService.DeleteCommentAsync(commentId, postId, creatorId, createdAt));
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldThrowEntityNotFoundException_WhenCommentDoesNotExist()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var postEntity = new PostEntity { Id = postId };

        _mockDbRepository.Setup(x => x.LoadAsync<PostEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync(postEntity);
        _mockDbRepository.Setup(x => x.LoadAsync<CommentEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync((CommentEntity)null);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _dynamoDBService.DeleteCommentAsync(commentId, postId, creatorId, createdAt));
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldThrowUnauthorizedUserException_WhenUserNotAuthorized()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var postEntity = new PostEntity { Id = postId };
        var commentEntity = new CommentEntity { Creator = Guid.NewGuid() };

        _mockDbRepository.Setup(x => x.LoadAsync<PostEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync(postEntity);
        _mockDbRepository.Setup(x => x.LoadAsync<CommentEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync(commentEntity);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedUserException>(() =>
            _dynamoDBService.DeleteCommentAsync(commentId, postId, creatorId, createdAt));
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldUpdatePostCorrectly_WhenDeletingThirdComment()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastComment = new CommentEntity { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, Creator = creatorId };
        var secondLastComment = new CommentEntity
            { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddMinutes(-1), Creator = creatorId };
        var thirdComment = new CommentEntity
            { Id = commentId, CreatedAt = DateTime.UtcNow.AddMinutes(-2), Creator = creatorId };
        var postEntity = new PostEntity
            { Id = postId, LastComment = lastComment, SecondLastComment = secondLastComment, CommentCount = 3 };

        var mockPostTransactWrite = new Mock<ITransactWriteWrapper<PostEntity>>();
        var mockCommentTransactWrite = new Mock<ITransactWriteWrapper<CommentEntity>>();

        var mockAsyncSearchResponse = new MockAsyncSearch<CommentEntity>(new List<CommentEntity>
        {
            lastComment, secondLastComment, thirdComment
        });

        _mockDbRepository.Setup(x => x.LoadAsync<PostEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync(postEntity);
        _mockDbRepository.Setup(x => x.LoadAsync<CommentEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync(thirdComment);
        _mockDbRepository.Setup(x => x.CreateTransactWrite<PostEntity>()).Returns(mockPostTransactWrite.Object);
        _mockDbRepository.Setup(x => x.CreateTransactWrite<CommentEntity>()).Returns(mockCommentTransactWrite.Object);
        _mockDbRepository.Setup(x => x.QueryAsync<CommentEntity>(It.IsAny<QueryOperationConfig>()))
            .Returns(mockAsyncSearchResponse);

        // Act
        await _dynamoDBService.DeleteCommentAsync(commentId, postId, creatorId, createdAt);

        // Assert
        Assert.Equal(2, postEntity.CommentCount);
        Assert.Equal(lastComment, postEntity.LastComment);
        Assert.Equal(secondLastComment, postEntity.SecondLastComment);
        mockPostTransactWrite.Verify(x => x.AddSaveItem(postEntity), Times.Once);
        mockCommentTransactWrite.Verify(x => x.AddDeleteItem(thirdComment), Times.Once);
        _mockDbRepository.Verify(
            x => x.ExecuteTransactWriteAsync(mockPostTransactWrite.Object, mockCommentTransactWrite.Object),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldUpdatePostCorrectly_WhenDeletingSecondLastComment()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMinutes(-1);
        var lastComment = new CommentEntity { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, Creator = creatorId };
        var secondLastComment = new CommentEntity { Id = commentId, CreatedAt = createdAt, Creator = creatorId };
        var thirdComment = new CommentEntity
            { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddMinutes(-2), Creator = creatorId };
        var postEntity = new PostEntity
            { Id = postId, LastComment = lastComment, SecondLastComment = secondLastComment, CommentCount = 3 };

        var mockPostTransactWrite = new Mock<ITransactWriteWrapper<PostEntity>>();
        var mockCommentTransactWrite = new Mock<ITransactWriteWrapper<CommentEntity>>();

        var mockAsyncSearchResponse = new MockAsyncSearch<CommentEntity>(new List<CommentEntity>
        {
            lastComment, thirdComment, secondLastComment
        });

        _mockDbRepository.Setup(x => x.LoadAsync<PostEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync(postEntity);
        _mockDbRepository.Setup(x => x.LoadAsync<CommentEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync(secondLastComment);
        _mockDbRepository.Setup(x => x.CreateTransactWrite<PostEntity>()).Returns(mockPostTransactWrite.Object);
        _mockDbRepository.Setup(x => x.CreateTransactWrite<CommentEntity>()).Returns(mockCommentTransactWrite.Object);
        _mockDbRepository.Setup(x => x.QueryAsync<CommentEntity>(It.IsAny<QueryOperationConfig>()))
            .Returns(mockAsyncSearchResponse);

        // Act
        await _dynamoDBService.DeleteCommentAsync(commentId, postId, creatorId, createdAt);

        // Assert
        Assert.Equal(2, postEntity.CommentCount);
        Assert.Equal(lastComment, postEntity.LastComment);
        Assert.Equal(thirdComment, postEntity.SecondLastComment);
        mockPostTransactWrite.Verify(x => x.AddSaveItem(postEntity), Times.Once);
        mockCommentTransactWrite.Verify(x => x.AddDeleteItem(secondLastComment), Times.Once);
        _mockDbRepository.Verify(
            x => x.ExecuteTransactWriteAsync(mockPostTransactWrite.Object, mockCommentTransactWrite.Object),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldUpdatePostCorrectly_WhenDeletingLastComment()
    {
        // Arrange
        var commentId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastComment = new CommentEntity { Id = commentId, CreatedAt = createdAt, Creator = creatorId };
        var secondLastComment = new CommentEntity
            { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddMinutes(-1), Creator = creatorId };
        var thirdComment = new CommentEntity
            { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddMinutes(-2), Creator = creatorId };
        var postEntity = new PostEntity
            { Id = postId, LastComment = lastComment, SecondLastComment = secondLastComment, CommentCount = 3 };

        var mockPostTransactWrite = new Mock<ITransactWriteWrapper<PostEntity>>();
        var mockCommentTransactWrite = new Mock<ITransactWriteWrapper<CommentEntity>>();

        var mockAsyncSearchResponse = new MockAsyncSearch<CommentEntity>(new List<CommentEntity>
        {
            secondLastComment, thirdComment, lastComment
        });

        _mockDbRepository.Setup(x => x.LoadAsync<PostEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync(postEntity);
        _mockDbRepository.Setup(x => x.LoadAsync<CommentEntity>(It.IsAny<object>(), It.IsAny<object>()))
            .ReturnsAsync(lastComment);
        _mockDbRepository.Setup(x => x.CreateTransactWrite<PostEntity>()).Returns(mockPostTransactWrite.Object);
        _mockDbRepository.Setup(x => x.CreateTransactWrite<CommentEntity>()).Returns(mockCommentTransactWrite.Object);
        _mockDbRepository.Setup(x => x.QueryAsync<CommentEntity>(It.IsAny<QueryOperationConfig>()))
            .Returns(mockAsyncSearchResponse);

        // Act
        await _dynamoDBService.DeleteCommentAsync(commentId, postId, creatorId, createdAt);

        // Assert
        Assert.Equal(2, postEntity.CommentCount);
        Assert.Equal(secondLastComment, postEntity.LastComment);
        Assert.Equal(thirdComment, postEntity.SecondLastComment);
        mockPostTransactWrite.Verify(x => x.AddSaveItem(postEntity), Times.Once);
        mockCommentTransactWrite.Verify(x => x.AddDeleteItem(lastComment), Times.Once);
        _mockDbRepository.Verify(
            x => x.ExecuteTransactWriteAsync(mockPostTransactWrite.Object, mockCommentTransactWrite.Object),
            Times.Once);
    }
}