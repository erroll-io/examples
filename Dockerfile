ARG PLATFORM=linux/arm64
# TODO: figure out why x64 causes L21 to hang
#ARG PLATFORM=linux/amd64

FROM --platform=${PLATFORM} mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG RUNTIME=linux-arm64
#ARG RUNTIME=linux-x64

WORKDIR /source

ENV DOTNET_NOLOGO=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=true

RUN apt-get update && apt-get install -y --no-install-recommends clang zlib1g-dev gcc

RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y
RUN ~/.cargo/bin/cargo install uniffi-bindgen-cs --git https://github.com/NordSecurity/uniffi-bindgen-cs --tag v0.8.0+v0.25.0

COPY ./cedar-sharp ./cedar-sharp
RUN cd cedar-sharp && ~/.cargo/bin/cargo build

# TODO: fix lib versioning issue
RUN find cedar-sharp/bindings/ -iregex '.*cedarsharp.cs' -exec sed -i.bak 's/24 != scaffolding_contract_version/25 != scaffolding_contract_version/g' {} \;

COPY ./Examples.sln ./

COPY ./src/MinimalApi/MinimalApi.csproj ./src/MinimalApi/

RUN dotnet restore -r ${RUNTIME}

COPY ./src ./src

RUN dotnet publish src/MinimalApi/MinimalApi.csproj -c Release -r ${RUNTIME} -o publish --self-contained /p:EnableTrimAnalyzer=false
