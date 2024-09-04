using Amazon.S3;
using Amazon.S3.Model;
using Common.Exceptions;
using Common.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Services.Concrete.Aws;
using Services.Interfaces.Aws;
using System.Net;

public class S3ServiceTests
{
    private readonly Mock<IAmazonS3> _mockS3Client;
    private readonly Mock<IOptions<PostSettings>> _mockPostSettings;
    private readonly IS3Service _s3Service;
    private readonly PostSettings _postSettings;

    public S3ServiceTests()
    {
        _mockS3Client = new Mock<IAmazonS3>();
        _postSettings = new PostSettings { MaxImageSizeInBytes = 1048576 }; // 1 MB
        _mockPostSettings = new Mock<IOptions<PostSettings>>();
        _mockPostSettings.Setup(x => x.Value).Returns(_postSettings);

        _s3Service = new S3Service(_mockS3Client.Object, _mockPostSettings.Object);
    }

    [Fact]
    public async Task UploadImageToS3Async_ImageIsNull_ThrowsArgumentException()
    {
        // Arrange
        IFormFile image = null;
        var s3Key = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _s3Service.UploadImageToS3Async(image, s3Key));
    }

    [Fact]
    public async Task UploadImageToS3Async_ImageIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var image = new Mock<IFormFile>();
        image.Setup(i => i.Length).Returns(0);
        var s3Key = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _s3Service.UploadImageToS3Async(image.Object, s3Key));
    }

    [Fact]
    public async Task UploadImageToS3Async_InvalidImageFormat_ThrowsArgumentException()
    {
        // Arrange
        var image = new Mock<IFormFile>();
        image.Setup(i => i.Length).Returns(1024); // 1 KB
        image.Setup(i => i.ContentType).Returns("application/pdf");
        image.Setup(i => i.FileName).Returns("test.pdf");
        var s3Key = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _s3Service.UploadImageToS3Async(image.Object, s3Key));
    }

    [Fact]
    public async Task UploadImageToS3Async_ImageSizeExceedsLimit_ThrowsArgumentException()
    {
        // Arrange
        var image = new Mock<IFormFile>();
        image.Setup(i => i.Length).Returns(_postSettings.MaxImageSizeInBytes + 1);
        image.Setup(i => i.ContentType).Returns("image/jpeg");
        image.Setup(i => i.FileName).Returns("test.jpg");
        var s3Key = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _s3Service.UploadImageToS3Async(image.Object, s3Key));
    }

    [Fact]
    public async Task UploadImageToS3Async_ValidImage_UploadsSuccessfully()
    {
        // Arrange
        var image = new Mock<IFormFile>();
        image.Setup(i => i.Length).Returns(1024); // 1 KB
        image.Setup(i => i.ContentType).Returns("image/jpeg");
        image.Setup(i => i.FileName).Returns("test.jpg");
        image.Setup(i => i.OpenReadStream()).Returns(new MemoryStream(new byte[1024]));

        var s3Key = Guid.NewGuid();
        _mockS3Client.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        await _s3Service.UploadImageToS3Async(image.Object, s3Key);

        // Assert
        _mockS3Client.Verify(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Once);
    }

    [Fact]
    public async Task UploadImageToS3Async_S3UploadFails_ThrowsAwsS3PutObjectException()
    {
        // Arrange
        var image = new Mock<IFormFile>();
        image.Setup(i => i.Length).Returns(1024); // 1 KB
        image.Setup(i => i.ContentType).Returns("image/jpeg");
        image.Setup(i => i.FileName).Returns("test.jpg");
        image.Setup(i => i.OpenReadStream()).Returns(new MemoryStream(new byte[1024]));

        var s3Key = Guid.NewGuid();
        _mockS3Client.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.InternalServerError });

        // Act & Assert
        await Assert.ThrowsAsync<AwsS3PutObjectException>(() => _s3Service.UploadImageToS3Async(image.Object, s3Key));
    }
}