FROM microsoft/dotnet:3.1-sdk AS build
WORKDIR /app

COPY sa-telebot.csproj .
RUN dotnet restore sa-telebot.csproj

COPY . .
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:3.1-aspnetcore-runtime AS runtime
WORKDIR /app
COPY --from=build /app/out ./

ENTRYPOINT ["SA.Telebot.exe"]
