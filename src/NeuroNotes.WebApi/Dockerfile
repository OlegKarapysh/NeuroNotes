FROM curlimages/curl:8.10.1 AS whisper-model
ARG WHISPER_MODEL_URL=https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
WORKDIR /model
RUN curl -fsSL --retry 3 -o ggml-base.bin "$WHISPER_MODEL_URL"

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/NeuroNotes.WebApi/NeuroNotes.WebApi.csproj", "src/NeuroNotes.WebApi/"]
RUN dotnet restore "src/NeuroNotes.WebApi/NeuroNotes.WebApi.csproj"
COPY . .
WORKDIR "/src/src/NeuroNotes.WebApi"
RUN dotnet build "./NeuroNotes.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NeuroNotes.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final

WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=publish /app/publish .
COPY --from=whisper-model /model/ggml-base.bin ./ggml-base.bin

RUN apt-get update \
    && apt-get install -y ffmpeg \
    && rm -rf /var/lib/apt/lists/* \

EXPOSE 8080    
ENTRYPOINT ["dotnet", "NeuroNotes.WebApi.dll"]