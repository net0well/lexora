FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json .editorconfig Directory.Build.props Directory.Packages.props Lexora.slnx ./
COPY src/ src/
RUN dotnet restore src/Hosts/Lexora.Worker/Lexora.Worker.csproj
RUN ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Hosts/Lexora.Worker -- codegen write
RUN dotnet publish src/Hosts/Lexora.Worker/Lexora.Worker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lexora.Worker.dll"]
