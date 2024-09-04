FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

COPY ./src/ProcessImageLambda/ProcessImageLambda.csproj .

RUN dotnet restore

COPY ./src/ProcessImageLambda/ .

RUN dotnet publish -c Release -o /tmp/lambda

RUN apt-get update && apt-get install -y zip && cd /tmp/lambda && zip -r function.zip *

CMD ["sh", "-c", "echo Lambda build complete; ls -la /tmp/lambda"]
