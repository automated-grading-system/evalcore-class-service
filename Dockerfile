FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ClassService.sln ./
COPY src/Class.Domain/Class.Domain.csproj src/Class.Domain/
COPY src/Class.Application/Class.Application.csproj src/Class.Application/
COPY src/Class.Infrastructure/Class.Infrastructure.csproj src/Class.Infrastructure/
COPY src/Class.Api/Class.Api.csproj src/Class.Api/
COPY tests/Class.Tests/Class.Tests.csproj tests/Class.Tests/
RUN dotnet restore ClassService.sln

COPY . .
RUN dotnet publish src/Class.Api/Class.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN adduser --disabled-password --gecos "" --uid 10001 appuser
COPY --from=build /app/publish .
USER appuser
ENTRYPOINT ["dotnet", "Class.Api.dll"]
