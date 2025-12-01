dotnet publish -c Release -r win-x64 . --self-contained true /p:PublishSingleFile=true

copy .\\bin\\Release\\net9.0\\win-x64\\publish\\AvalonDevTool.exe ..\\..\\
IF NOT EXIST "..\\..\\appsettings.json" COPY ".\\appsettings.json" "..\\..\\"