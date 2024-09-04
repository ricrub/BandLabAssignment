using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using Amazon.S3;
using Api.Helpers;
using Api.Middlewares;
using Common.Models.Configuration;
using Services.Concrete;
using Services.Concrete.Aws;
using Services.Interfaces;
using Services.Interfaces.Aws;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilesToInclude = new List<string>
    {
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
    };
    xmlFilesToInclude.ForEach(x =>
    {
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, x), includeControllerXmlComments: true);
    });

    options.EnableAnnotations();
});

var accessKey = builder.Configuration.GetValue<string>("AWS:AccessKey");
var secretKey = builder.Configuration.GetValue<string>("AWS:SecretKey");
var serviceUrl = builder.Configuration.GetValue<string>("AWS:ServiceUrl");

var awsOptions = builder.Configuration.GetAWSOptions();
awsOptions.Credentials = new BasicAWSCredentials(accessKey, secretKey);
builder.Services.AddDefaultAWSOptions(awsOptions);

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var s3Config = new AmazonS3Config
    {
        ServiceURL = serviceUrl,
        ForcePathStyle = true
    };

    var credentials = new BasicAWSCredentials(accessKey, secretKey);

    return new AmazonS3Client(credentials, s3Config);
});

builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddSingleton<IDynamoDBContext, DynamoDBContext>();
builder.Services.AddScoped<IDynamoDBService, DynamoDBService>();
builder.Services.AddScoped<IDynamoDbRepository, DynamoDbRepository>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IS3Service, S3Service>();

builder.Services.AddAutoMapper(typeof(MappingProfiles));

builder.Services.AddControllers();

builder.Services.Configure<PostSettings>(builder.Configuration.GetSection("PostSettings"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();

