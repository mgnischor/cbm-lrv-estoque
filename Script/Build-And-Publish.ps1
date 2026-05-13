Clear-Host
Set-Location ..

dotnet clean -c Debug -r x86
dotnet clean -c Debug -r x64
dotnet clean -c Release -r x86
dotnet clean -c Release -r x64

dotnet build -c Debug -p:Platform=x86
dotnet build -c Debug -p:Platform=x64
dotnet build -c Release -p:Platform=x86
dotnet build -c Release -p:Platform=x64

dotnet publish -c Debug -p:Platform=x86
dotnet publish -c Debug -p:Platform=x64
dotnet publish -c Release -p:Platform=x86
dotnet publish -c Release -p:Platform=x64

makensis Installer.nsi