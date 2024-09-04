# BandLab Test Assignment

After executing `docker compose up`, you can access the Swagger UI to start using the API by navigating to [http://localhost:5175/swagger/index.html](http://localhost:5175/swagger/index.html).

Uploaded images are accessible at: [http://localhost:4566/post-images/{imageId}](http://localhost:4566/post-images/{imageId})

Resized images are accessible at: [http://localhost:4566/post-images-final/{imageId}](http://localhost:4566/post-images-final/{imageId})

## Architecture

In the local environment, I'm using the Localstack image to simulate AWS resources.

The system consists of the following components:

1. **Backend API:**  
   A .NET Core web API that handles all public-facing endpoints necessary to meet the functional requirements:
    - `POST /api/Posts`: Create a new post
    - `GET /api/Posts`: List all posts
    - `POST /api/Comments`: Create a new comment
    - `DELETE /api/Comments`: Delete a comment

2. **AWS S3:**  
   Used to store original images in the `post-images` bucket and processed images in the `post-images-final` bucket.

3. **DynamoDB:**  
   Serves as the database for storing user posts and comments.

4. **AWS Lambda:**  
   Monitors DynamoDB streams. When a new post is added, it retrieves the original image, resizes it, and saves the resized version to the `post-images-final` bucket.

### DynamoDB Data Model

- **Posts Table:**
    - When creating a post:
        - The primary key (PK) and sort key (SK) are formatted as `POST#{postId}`. For example, `PK: POST#4db6dc8c-fbbe-4769-aee4-b35303a9e423` and `SK: POST#4db6dc8c-fbbe-4769-aee4-b35303a9e423`.
        - In addition to standard attributes, the last two comments are stored in a denormalized form using the `LastComment` and `SecondLastComment` attributes.
    - When creating a comment:
        - The PK is in the format `POST#{postId}`, and the SK is `COMMENT#{createdAt}#{commentId}`. For example, `PK: POST#4db6dc8c-fbbe-4769-aee4-b35303a9e423` and `SK: COMMENT#2024-09-04T09:00:15.724Z#66998233-eea5-48b3-bd0b-9326d6160c28`.

- **Global Secondary Index (GSI):**
    - A GSI is used with `PK: GSI#` (the same value for all posts) and `SK: CommentCount`. This index is used to sort posts by the number of comments in descending order, improving the efficiency of retrieving posts.

### Post Creation Logic

1. Generate an `imageId` and upload the image to S3.
2. Insert a new item into DynamoDB, setting attributes such as PK, SK, GSI_PK, Id, ImageId, etc.... The ImageId will be used by the Lambda function to download and resize the original image.

### Retrieving Posts

- Utilize the GSI to apply pagination and fetch the top posts ordered by comment count in descending order. The API provides the next cursor value for pagination.

### Comment Creation Logic

- New comments are inserted into the Posts table, and the last and second-last comments are updated. The following changes occur in the same transaction within the same partition:
    - Insert the comment.
    - Update the post to increase the comment count.
    - Update the post to set `SecondLastComment` to the current `LastComment` and `LastComment` to the new comment.

### Comment Deletion Logic

- Comments are deleted from the Posts table, and the last and second-last comments are updated. To set proper values, the last three comments are queried in advance. If the last comment is deleted, the following changes occur within the same transaction and partition:
    - Retrieve the last three comments (using SK in `COMMENT#{createdAt}#{commentId}` format for ordering).
    - Delete the comment.
    - Update the post to decrease the comment count.
    - Update the post to set `LastComment` to `SecondLastComment` and `SecondLastComment` to `ThirdLastComment`.

**Note:** The comment format `COMMENT#{createdAt}#{commentId}` requires the `createdAt` timestamp, Post ID, and Comment ID in the deletion request to be passed.

## Future Considerations

Due to time constraints I was not able to deliver several interesting features, certain aspects could be improved:

- **Security:**  
  Implement authentication and authorization mechanisms. Currently, all endpoints are public and expect a `creatorId`. A JWT-based authentication system could be used to identify users securely.

- **Image Upload Optimization:**
    - **Upload:** Support for chunked and parallel image uploads to enhance speed, especially for large images.
    - **Download:** Cache images using a CDN to reduce latency and improve access times for end-users.

- **Query Optimization:**
    - Utilize Amazon DynamoDB Accelerator (DAX) for caching, which wasn't implemented due to Localstack limitations.
    - Leverage Redis for querying posts. An additional AWS Lambda function could monitor DynamoDB streams and store data in a Redis cluster. This would allow read requests to be served directly from Redis, reducing load on DynamoDB.
