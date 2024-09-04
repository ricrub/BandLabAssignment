using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.DynamoDBv2.Model;
using StackExchange.Redis;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ProcessPostsLambda
{
    public class Function
    {
        private readonly IDatabase _redis;

        public Function()
        {
            // Connect to Redis running on the Docker container
            var redis = ConnectionMultiplexer.Connect("redis:6379");
            _redis = redis.GetDatabase();
        }

        public async Task FunctionHandler(DynamoDBEvent dynamoDbEvent, ILambdaContext context)
        {
            foreach (var record in dynamoDbEvent.Records)
            {
                if (record.EventName == "INSERT")
                {
                    //var post = DeserializePost(record.Dynamodb.NewImage);
                    //await UpdateRedisAsync(post);
                }
            }
        }

        private Post DeserializePost(Dictionary<string, AttributeValue> newImage)
        {
            return new Post
            {
                PostId = newImage["Id"].S,
                Caption = newImage["Caption"].S,
                Creator = Guid.Parse(newImage["Creator"].S),
                CreatedAt = DateTime.Parse(newImage["CreatedAt"].S),
                ImageUrl = newImage["ImageUrl"].S,
                CommentCount = int.Parse(newImage["CommentCount"].N),
                LastTwoComments = JsonSerializer.Deserialize<List<string>>(newImage["LastTwoComments"].S)
            };
        }

        private async Task UpdateRedisAsync(Post post)
        {
            var postData = new
            {
                post.Caption,
                post.Creator,
                post.CreatedAt,
                post.ImageUrl,
                post.CommentCount,
                LastTwoComments = JsonSerializer.Serialize(post.LastTwoComments)
            };

            // Save the post data to Redis using HSET command
            await _redis.HashSetAsync($"post:{post.PostId}", postData.ToHashEntries());
        }
    }

    public class Post
    {
        public string PostId { get; set; }
        public string Caption { get; set; }
        public Guid Creator { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ImageUrl { get; set; }
        public int CommentCount { get; set; }
        public List<string> LastTwoComments { get; set; }
    }

    public static class Extensions
    {
        public static HashEntry[] ToHashEntries(this object obj)
        {
            var properties = obj.GetType().GetProperties();
            return properties.Select(p => new HashEntry(p.Name, p.GetValue(obj)?.ToString())).ToArray();
        }
    }
}
