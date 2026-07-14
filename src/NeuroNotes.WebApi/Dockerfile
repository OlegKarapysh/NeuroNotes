FROM curlimages/curl:8.10.1 AS whisper-model
ARG WHISPER_MODEL_URL=https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
WORKDIR /model
RUN curl -fsSL --retry 3 -o ggml-base.bin "$WHISPER_MODEL_URL"

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy the full source before restore: the WebApi project references the other
# modules, and Central Package Management adds root-level Directory.*.props that
# `dotnet restore` requires.
COPY . .
RUN dotnet restore "src/NeuroNotes.WebApi/NeuroNotes.WebApi.csproj"
WORKDIR "/src/src/NeuroNotes.WebApi"
RUN dotnet publish "./NeuroNotes.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# Install runtime deps before the volatile COPY steps so this rarely-changing layer
# stays cached across code changes. ffmpeg: audio conversion; curl: the container
# healthcheck (see docker-compose.prod.yml).
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY --from=whisper-model /model/ggml-base.bin ./ggml-base.bin

# The effective listen port is set at runtime via ASPNETCORE_URLS in docker-compose.prod.yml.
# EXPOSE is informational only, but deriving it from APP_PORT keeps it from drifting: the deploy
# workflow passes the same ${APP_PORT} it feeds compose as a --build-arg (defaulting to 8080, the
# base aspnet image's default).
ARG APP_PORT=8080
EXPOSE ${APP_PORT}
ENTRYPOINT ["dotnet", "NeuroNotes.WebApi.dll"]
