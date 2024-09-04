using Amazon.DynamoDBv2.DataModel;

namespace Common.Models.Entities
{
    [DynamoDBTable("Posts")]
    public class CommentEntity
    {
        [DynamoDBHashKey("PK")]
        public string PK { get; set; }

        [DynamoDBRangeKey("SK")]
        public string SK { get; set; }

        [DynamoDBProperty("Id")]
        public Guid Id { get; set; }

        [DynamoDBProperty("PostId")]
        public Guid PostId { get; set; }

        [DynamoDBProperty("Content")]
        public string Content { get; set; }

        [DynamoDBProperty("Creator")]
        public Guid Creator { get; set; }

        [DynamoDBProperty("CreatedAt")]
        public DateTime CreatedAt { get; set; }
    }
}