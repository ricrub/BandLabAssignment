using Amazon.S3;
using Amazon.S3.Model;
using Common.Constants;
using Common.Exceptions;
using Common.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Services.Interfaces.Aws;

namespace Services.Concrete.Aws;

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly IOptions<PostSettings> _postSettings;

    public S3Service(IAmazonS3 s3Client, IOptions<PostSettings> postSettings)
    {
        _s3Client = s3Client;
        _postSettings = postSettings;
    }

    public async Task UploadImageToS3Async(IFormFile image, Guid s3Key)
    {
        if (image == null || image.Length == 0)
        {
            throw new ArgumentException("Image cannot be null or empty", nameof(image));
        }
        
        if (!IsValidImageFormat(image))
        {
            throw new ArgumentException("Invalid image format. Only .png, .jpg, and .bmp formats are allowed.", nameof(image));
        }

        if (image.Length > _postSettings.Value.MaxImageSizeInBytes)
        {
            throw new ArgumentException($"Image size cannot exceed maximum allowed size", nameof(image));
        }

        await using var stream = image.OpenReadStream();

        var putRequest = new PutObjectRequest
        {
            BucketName = AwsS3Constants.OriginalBucketName,
            Key = s3Key.ToString(),
            InputStream = stream,
            ContentType = image.ContentType,
            CannedACL = S3CannedACL.PublicRead
        };

        var response = await _s3Client.PutObjectAsync(putRequest);

        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new AwsS3PutObjectException($"Failed to upload image to S3. Status code: {response.HttpStatusCode}");
        }
    }
    
    private bool IsValidImageFormat(IFormFile file)
    {
        var allowedContentTypes = new List<string> { "image/png", "image/jpeg", "image/bmp" };
        var allowedExtensions = new List<string> { ".png", ".jpg", ".jpeg", ".bmp" };

        if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            return false;
        }

        return true;
    }
}