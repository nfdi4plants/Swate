dotnet pack src/Shared/Swate.Components.Core.fsproj --configuration Release --output nupkgs /p:GitTag=1.0.0-test.%*

dotnet pack src/Components/src/Swate.Components.fsproj --configuration Release --output nupkgs /p:GitTag=1.0.0-test.%*