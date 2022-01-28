#!/usr/bin/with-contenv bashio
uname -a
cat /etc/os-release
cd /app
dotnet Easywave2Mqtt.dll
