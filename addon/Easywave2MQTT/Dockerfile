FROM mcr.microsoft.com/dotnet/runtime:7.0
# Copy add-on binaries
ADD app /app
# Switch to app folder
WORKDIR /app
# Run the addon
CMD [ "dotnet", "Easywave2Mqtt.dll" ]
