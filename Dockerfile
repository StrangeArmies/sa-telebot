FROM docker pull mcr.microsoft.com/dotnet/sdk AS build
WORKDIR /app

COPY sa-telebot.csproj .
RUN dotnet restore sa-telebot.csproj

COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime AS runtime
WORKDIR /app
COPY --from=build /app/out ./

ENTRYPOINT ["SA.Telebot.exe"]
