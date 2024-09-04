FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["Common/Common.csproj", "Common/"]
COPY ["Services/Services.csproj", "Services/"]

COPY ["Api/Api.csproj", "Api/"]

RUN dotnet restore "Api/Api.csproj"

COPY . .
WORKDIR /src/Api
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]

