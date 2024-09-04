# Use the .NET SDK image for building and running
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Set the working directory inside the container
WORKDIR /app

# Copy the project files into the container
COPY ./src/ProcessPostsLambda/ProcessPostsLambda.csproj .

# Restore the project dependencies
RUN dotnet restore

# Copy the rest of the application files into the container
COPY ./src/ProcessPostsLambda/ .

# Publish the application to a temporary output folder
RUN dotnet publish -c Release -o /tmp/lambda

# Install zip utility and create the zip file directly in /tmp/lambda
RUN apt-get update && apt-get install -y zip && cd /tmp/lambda && zip -r function.zip *

# Add a test file for verification
RUN echo "This is a test file for verification." > /tmp/lambda/test.txt

# List contents of the /tmp/lambda directory for verification
RUN echo "Listing contents of /tmp/lambda:" && ls -la /tmp/lambda

# Set the entrypoint to show the contents of the /tmp/lambda directory
CMD ["sh", "-c", "echo Lambda build complete; ls -la /tmp/lambda"]
