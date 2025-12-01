dotnet publish -c Release -r osx-x64 . --self-contained true /p:PublishSingleFile=true
dotnet publish -c Release -r win-x64 . --self-contained true /p:PublishSingleFile=true

cp ./bin/Release/net9.0/osx-x64/publish/AvalonDevTool ../../
cp ./bin/Release/net9.0/win-x64/publish/AvalonDevTool.exe ../../

if [ ! -f ../../appsettings.json ]; then
  cp ./appsettings.json ../../
fi
