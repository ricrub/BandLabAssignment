using Amazon.DynamoDBv2.DataModel;

namespace Common.Models.Entities;


[DynamoDBTable("Posts")]
public class PostEntity
{
    [DynamoDBHashKey("PK")]
    public string PK { get; set; }

    [DynamoDBRangeKey("SK")]
    public string SK { get; set; }
    
    [DynamoDBProperty("Id")]
    public Guid Id { get; set; }

    [DynamoDBProperty("Caption")]
    public string Caption { get; set; }

    [DynamoDBProperty("Creator")]
    public Guid Creator { get; set; }

    [DynamoDBProperty("ImageId")]
    public Guid ImageId { get; set; }

    [DynamoDBProperty("CreatedAt")]
    public DateTime CreatedAt { get; set; }
    
    [DynamoDBProperty("GSI_PK")]
    public string GsiPk { get; set; } = "GLOBAL";

    [DynamoDBProperty("CommentCount")]
    public int CommentCount { get; set; }
    
    [DynamoDBProperty("LastComment")]
    public CommentEntity LastComment { get; set; }

    [DynamoDBProperty("SecondLastComment")]
    public CommentEntity SecondLastComment { get; set; }
}