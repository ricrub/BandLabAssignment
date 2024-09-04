using Microsoft.AspNetCore.Http;

namespace Services.Interfaces.Aws;

public interface IS3Service
{
    Task UploadImageToS3Async(IFormFile image, Guid s3Key);
}