dotnet new sln -o CodeStats
dotnet new classlib -lang "F#" -o src/CodeStats
dotnet sln add src/CodeStats/CodeStats.fsproj
dotnet add src/CodeStats/CodeStats.fsproj package Microsoft.Extensions.FileSystemGlobbing --version 3.1.4

dotnet new console -lang "F#" -o src/App
dotnet sln add src/App/App.fsproj
dotnet add src/App/App.fsproj reference src/CodeStats/CodeStats.fsproj

cd src/CodeStats.Tests
dotnet new xunit -lang "F#"
dotnet add reference ../CodeStats/CodeStats.fsproj
