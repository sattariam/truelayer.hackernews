#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR "/src"
COPY ["TrueLayer.HackerNews/TrueLayer.HackerNews.csproj", "TrueLayer.HackerNews/"]
RUN dotnet restore "TrueLayer.HackerNews/TrueLayer.HackerNews.csproj"
WORKDIR "/src/TrueLayer.HackerNews"
COPY TrueLayer.HackerNews .
RUN dotnet build "TrueLayer.HackerNews.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TrueLayer.HackerNews.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TrueLayer.HackerNews.dll", "--posts", "20"]