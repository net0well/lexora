FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json .editorconfig Directory.Build.props Directory.Packages.props Lexora.slnx ./
COPY src/ src/
RUN dotnet restore src/Hosts/Lexora.Api/Lexora.Api.csproj
RUN ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Hosts/Lexora.Api -- codegen write
RUN dotnet publish src/Hosts/Lexora.Api/Lexora.Api.csproj -c Release -o /app/publish

FROM build AS bundle
ARG TARGETARCH
RUN dotnet tool install --global dotnet-ef --version 10.0.15
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-arm64 || echo linux-x64) \
 && dotnet ef migrations bundle \
      --project src/Infrastructure/Lexora.Infrastructure.Persistence \
      --startup-project src/Hosts/Lexora.Api \
      --context LexoraDbContext \
      --self-contained -r "$RID" \
      --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=bundle /app/efbundle ./efbundle
ENTRYPOINT ["/bin/sh", "-c", "set -e; echo 'Applying database migrations'; ./efbundle --connection \"$ConnectionStrings__Lexora\"; for schema in wolverine_api wolverine_worker; do echo \"Setting up messaging resources for $schema\"; Persistence__WolverineSchema=\"$schema\" dotnet Lexora.Api.dll resources setup; done; echo 'Seeding admin user'; dotnet Lexora.Api.dll seed-admin"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lexora.Api.dll"]
