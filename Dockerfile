FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Easywave2MQTT/src/ .
RUN dotnet publish Easywave2Mqtt/Easywave2Mqtt.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
# Default config baked in; override by mounting your own at /app/appsettings.json
COPY Easywave2MQTT/src/Easywave2Mqtt/appsettings.json .
CMD ["dotnet", "Easywave2Mqtt.dll"]
