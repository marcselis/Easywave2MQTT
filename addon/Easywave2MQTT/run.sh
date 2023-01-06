#!/usr/bin/with-contenv bashio
cp /data/options.json /app/
cd /app
dotnet Easywave2Mqtt.dll
