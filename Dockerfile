ARG PLATFORM=linux/arm64
# TODO: figure out why x64 causes L21 to hang
#ARG PLATFORM=linux/amd64

FROM --platform=${PLATFORM} mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG RUNTIME=linux-arm64
#ARG RUNTIME=linux-x64

RUN apt-get update && apt-get install -y --no-install-recommends clang zlib1g-dev

WORKDIR /source

ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

COPY ./Examples.sln ./

COPY ./src/MinimalApi/MinimalApi.csproj ./src/MinimalApi/

RUN dotnet restore -r ${RUNTIME}

COPY ./src ./src

RUN dotnet publish src/MinimalApi/MinimalApi.csproj -c Release -r ${RUNTIME} -o publish --self-contained /p:EnableTrimAnalyzer=false
