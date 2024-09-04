using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ProcessImageLambda
{
    public class Function
    {
        private readonly IAmazonS3 _s3Client;

        public Function()
        {
            var credentials = new BasicAWSCredentials(Constants.AwsAccessKey, Constants.AwsSecretKey);
            _s3Client = new AmazonS3Client(credentials, new AmazonS3Config
            {
                ServiceURL = Constants.AwsServiceUrl,
                ForcePathStyle = true
            });
        }

        public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
        {
            foreach (var record in dynamoEvent.Records)
            {
                if (record.EventName == "INSERT")
                {
                    var newImage = record.Dynamodb.NewImage;
                    if (newImage.ContainsKey("ImageId"))
                    {
                        await ProcessImage(newImage["ImageId"].S, context);
                    }
                }
            }
        }

        private async Task ProcessImage(string imageKey, ILambdaContext context)
        {
            try
            {
                context.Logger.LogInformation($"Starting processing image {imageKey}");

                using var response = await _s3Client.GetObjectAsync(Constants.OriginalBucketName, imageKey);
                using var responseStream = response.ResponseStream;
                using var image = Image.Load(responseStream);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(600, 600),
                    Mode = ResizeMode.Crop
                }));
                using var memoryStream = new MemoryStream();
                await image.SaveAsJpegAsync(memoryStream);
                memoryStream.Position = 0;

                var putRequest = new PutObjectRequest
                {
                    BucketName = Constants.ProcessedBucketName,
                    Key = imageKey,
                    InputStream = memoryStream,
                    ContentType = "image/jpeg",
                    CannedACL = S3CannedACL.PublicRead
                };

                await _s3Client.PutObjectAsync(putRequest);

                context.Logger.LogInformation($"Successfully processed and resized image: {imageKey}");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Failed to process image: {imageKey}. Error: {ex.Message}");
                throw;
            }
        }
    }
}
