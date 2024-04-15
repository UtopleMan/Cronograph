dotnet build src/Cronograph.Shared/Cronograph.Shared.csproj --configuration Release
dotnet build src/Cronograph/Cronograph.csproj --configuration Release
dotnet publish src/Cronograph.Client/Cronograph.Client.csproj --configuration Release --output src/blazor
dotnet build src/Cronograph.UI/Cronograph.UI.csproj --configuration Release
REM dotnet pack src/Cronograph.Shared/Cronograph.Shared.csproj -o packages --configuration Release --no-build --verbosity normal -p:PackageVersion=1.0.${{ github.run_number }}
REM dotnet pack src/Cronograph/Cronograph.csproj -o packages --configuration Release --no-build --verbosity normal -p:PackageVersion=1.0.${{ github.run_number }}
REM dotnet pack src/Cronograph.UI/Cronograph.UI.csproj -o packages --configuration Release --no-build --verbosity normal -p:PackageVersion=1.0.${{ github.run_number }}
