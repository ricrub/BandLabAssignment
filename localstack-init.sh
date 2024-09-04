#!/bin/bash

echo "Creating DynamoDB table..."
awslocal dynamodb create-table --table-name Posts \
    --attribute-definitions AttributeName=PK,AttributeType=S AttributeName=SK,AttributeType=S \
    --key-schema AttributeName=PK,KeyType=HASH AttributeName=SK,KeyType=RANGE \
    --billing-mode PAY_PER_REQUEST
   
echo "Creating DynamoDB GSI..." 
awslocal dynamodb update-table --table-name Posts \
    --attribute-definitions \
        AttributeName=GSI_PK,AttributeType=S \
        AttributeName=CommentCount,AttributeType=N \
    --global-secondary-index-updates \
        "[{\"Create\": {\"IndexName\": \"GSI_CommentCount\", \"KeySchema\":[{\"AttributeName\":\"GSI_PK\",\"KeyType\":\"HASH\"}, {\"AttributeName\":\"CommentCount\",\"KeyType\":\"RANGE\"}], \"Projection\":{\"ProjectionType\":\"ALL\"}}}]"

echo "Creating S3 buckets..."
awslocal s3 mb s3://post-images
awslocal s3 mb s3://post-images-final

echo "Creating process image lambda function..."
awslocal lambda create-function \
    --function-name ProcessImageLambda \
    --runtime dotnet6 \
    --role arn:aws:iam::000000000000:role/dummy \
    --handler ProcessImageLambda::ProcessImageLambda.Function::FunctionHandler \
    --zip-file fileb:///image_lambda/function.zip \
    --timeout 60

echo "Enabling DynamoDB Streams..."
awslocal dynamodb update-table --table-name Posts \
    --stream-specification StreamEnabled=true,StreamViewType=NEW_IMAGE

echo "Fetching Stream ARN..."
stream_arn=$(awslocal dynamodbstreams list-streams --table-name Posts --query "Streams[0].StreamArn" --output text)

echo "Creating Event Source Mapping for ProcessImageLambda..."
awslocal lambda create-event-source-mapping \
    --function-name ProcessImageLambda \
    --event-source-arn "$stream_arn" \
    --batch-size 100 \
    --starting-position LATEST

echo "LocalStack initialization completed."
