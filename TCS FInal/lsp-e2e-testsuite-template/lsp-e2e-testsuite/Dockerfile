# set runtime and app directory
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base

# restore packages and build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /lsp-e2e-testsuite

COPY nuget.config nuget.config
COPY lsp-e2e-testsuite/lsp-e2e-testsuite.csproj lsp-e2e-testsuite/

RUN dotnet restore lsp-e2e-testsuite/lsp-e2e-testsuite.csproj

COPY lsp-e2e-testsuite/ lsp-e2e-testsuite/

RUN dotnet build lsp-e2e-testsuite/lsp-e2e-testsuite.csproj -c Release --no-restore

FROM build AS publish

RUN dotnet publish lsp-e2e-testsuite/lsp-e2e-testsuite.csproj -c Release -o /publish

# launch
FROM base AS final
RUN useradd -u 10500 myuser
WORKDIR /app/lsp-e2e-testsuite/
RUN chown -R 10500 .
COPY --from=publish /publish .
USER 10500
CMD ["dotnet", "lsp-e2e-testsuite.dll"]