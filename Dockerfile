FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN apt-get update && apt-get install -y --no-install-recommends clang zlib1g-dev

WORKDIR /source

ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

COPY ./Examples.sln ./

COPY ./src/MinimalApi/MinimalApi.csproj ./src/MinimalApi/

RUN dotnet restore -r linux-arm64

COPY ./src ./src

RUN dotnet publish src/MinimalApi/MinimalApi.csproj -c Release -r linux-arm64 -o publish --self-contained
