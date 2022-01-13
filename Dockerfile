#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["OnamaFrontendApi.csproj", ""]
RUN dotnet restore "./OnamaFrontendApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "OnamaFrontendApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OnamaFrontendApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app/data
COPY data/onama.owl /app/data/
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OnamaFrontendApi.dll"]