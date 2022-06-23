# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /source
RUN mkdir -p ./Myamtech.Terraria.DiscordBot/

# copy csproj and restore as distinct layers
COPY *.sln .
COPY Myamtech.Terraria.DiscordBot/*.csproj ./Myamtech.Terraria.DiscordBot/

RUN dotnet restore -r linux-musl-x64 /p:PublishReadyToRun=true

# copy everything else and build app
COPY Myamtech.Terraria.DiscordBot/. ./Myamtech.Terraria.DiscordBot/
WORKDIR /source/Myamtech.Terraria.DiscordBot
# /p:PublishReadyToRun=true is not working
# /p:PublishTrimmed=true leave it off for now since we use JSON types
RUN dotnet publish -c release -o /app -r linux-musl-x64 --self-contained true --no-restore /p:PublishSingleFile=true

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine-amd64
WORKDIR /app
COPY --from=build /app ./

ENTRYPOINT ["./Myamtech.Terraria.DiscordBot"]