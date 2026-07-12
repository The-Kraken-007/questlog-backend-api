FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["QuestLog.slnx", "./"]
COPY ["src/QuestLog.API/QuestLog.API.csproj", "src/QuestLog.API/"]
COPY ["src/QuestLog.Application/QuestLog.Application.csproj", "src/QuestLog.Application/"]
COPY ["src/QuestLog.Domain/QuestLog.Domain.csproj", "src/QuestLog.Domain/"]
COPY ["src/QuestLog.Infrastructure/QuestLog.Infrastructure.csproj", "src/QuestLog.Infrastructure/"]

RUN dotnet restore "src/QuestLog.API/QuestLog.API.csproj"
COPY ["src/", "src/"]
WORKDIR "/src/src/QuestLog.API"
RUN dotnet build "QuestLog.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "QuestLog.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QuestLog.API.dll"]
