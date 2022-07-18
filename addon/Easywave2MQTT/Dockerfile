FROM mcr.microsoft.com/dotnet/runtime:6.0
# Copy data for add-on
ADD app.tar.gz /app

WORKDIR /app

ENTRYPOINT [ "dotnet", "Easywave2Mqtt.dll" ]
