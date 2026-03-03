# ============================================
# Build Stage
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY ["src/Alfred.Identity.Domain/Alfred.Identity.Domain.csproj", "src/Alfred.Identity.Domain/"]
COPY ["src/Alfred.Identity.Application/Alfred.Identity.Application.csproj", "src/Alfred.Identity.Application/"]
COPY ["src/Alfred.Identity.Infrastructure/Alfred.Identity.Infrastructure.csproj", "src/Alfred.Identity.Infrastructure/"]
COPY ["src/Alfred.Identity.WebApi/Alfred.Identity.WebApi.csproj", "src/Alfred.Identity.WebApi/"]
COPY ["src/Alfred.Identity.Cli/Alfred.Identity.Cli.csproj", "src/Alfred.Identity.Cli/"]

# Restore with NuGet cache mount
RUN --mount=type=cache,id=nuget-identity,target=/root/.nuget/packages \
    dotnet restore "src/Alfred.Identity.WebApi/Alfred.Identity.WebApi.csproj" && \
    dotnet restore "src/Alfred.Identity.Cli/Alfred.Identity.Cli.csproj"

# Copy source code (tests excluded via .dockerignore)
COPY . .

# ============================================
# Publish Stage (single RUN = 1 layer, uses cache mount)
# ============================================
FROM build AS publish
RUN --mount=type=cache,id=nuget-identity,target=/root/.nuget/packages \
    dotnet publish "src/Alfred.Identity.WebApi/Alfred.Identity.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false && \
    dotnet publish "src/Alfred.Identity.Cli/Alfred.Identity.Cli.csproj" -c Release -o /app/publish/cli /p:UseAppHost=false

# ============================================
# Final Stage (Alpine = ~100MB smaller than Debian)
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN apk --no-cache add curl libgcc libstdc++ icu-libs

RUN addgroup -S -g 1001 alfred && adduser -S -u 1001 -G alfred -H alfred

COPY --from=publish --chown=alfred:alfred /app/publish .
COPY --from=publish --chown=alfred:alfred /app/publish/cli ./cli

USER alfred

EXPOSE 8100

HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8100/health || exit 1

ENTRYPOINT ["dotnet", "Alfred.Identity.WebApi.dll"]
