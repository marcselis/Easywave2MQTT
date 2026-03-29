pushd src
dotnet publish Easywave2Mqtt/Easywave2Mqtt.csproj -c Release -p:GenerateRuntimeConfigurationFiles=true -o artifacts
popd