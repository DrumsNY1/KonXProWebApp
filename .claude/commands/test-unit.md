Run unit test suites for WebApp and Functions without launching Docker containers.
Command:
dotnet test .\KonXProWebApp.Tests\KonXProWebApp.Tests.csproj --nologo
dotnet test .\KonXProWebApp.Functions.Tests\KonXProWebApp.Functions.Tests.csproj --nologo --filter "Category!=Integration"
