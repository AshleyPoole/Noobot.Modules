## NuGet checklist
- Set csproj properties
- Run `msbuild /t:pack /p:Configuration=Release` to build the class library
- Run `nuget push .\src\Noobot.Modules.Dns\bin\Release\Noobot.Modules.DNS.1.0.0.nupkg -Source https://www.nuget.org/api/v2/package` to push the package. Example for DNS module.

Alternatively, just run the 'BuildAndPublishModules.ps1' script after updating csproj properties.