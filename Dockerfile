# ============================================
# Build Stage - Compile ứng dụng
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies (tận dụng Docker layer caching)
COPY ["src/Alfred.Identity.Domain/Alfred.Identity.Domain.csproj", "src/Alfred.Identity.Domain/"]
COPY ["src/Alfred.Identity.Application/Alfred.Identity.Application.csproj", "src/Alfred.Identity.Application/"]
COPY ["src/Alfred.Identity.Infrastructure/Alfred.Identity.Infrastructure.csproj", "src/Alfred.Identity.Infrastructure/"]
COPY ["src/Alfred.Identity.WebApi/Alfred.Identity.WebApi.csproj", "src/Alfred.Identity.WebApi/"]
COPY ["src/Alfred.Identity.Cli/Alfred.Identity.Cli.csproj", "src/Alfred.Identity.Cli/"]

# Restore dependencies for each project (skip test projects)
RUN dotnet restore "src/Alfred.Identity.WebApi/Alfred.Identity.WebApi.csproj"
RUN dotnet restore "src/Alfred.Identity.Cli/Alfred.Identity.Cli.csproj"

# Copy toàn bộ source code (excluding tests via .dockerignore)
COPY . .

# Build ứng dụng
WORKDIR "/src/src/Alfred.Identity.WebApi"
RUN dotnet build "Alfred.Identity.WebApi.csproj" -c Release -o /app/build

# Build CLI tool
WORKDIR "/src/src/Alfred.Identity.Cli"
RUN dotnet build "Alfred.Identity.Cli.csproj" -c Release -o /app/cli

# ============================================
# Publish Stage - Tạo artifact để deploy
# ============================================
FROM build AS publish
WORKDIR "/src/src/Alfred.Identity.WebApi"
RUN dotnet publish "Alfred.Identity.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

WORKDIR "/src/src/Alfred.Identity.Cli"
RUN dotnet publish "Alfred.Identity.Cli.csproj" -c Release -o /app/publish/cli /p:UseAppHost=false

# ============================================
# Final Stage - Image runtime siêu nhẹ
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Tạo non-root user để bảo mật
RUN addgroup --system --gid 1001 alfred && \
    adduser --system --uid 1001 --ingroup alfred alfred

# Copy artifact từ publish stage
COPY --from=publish /app/publish .
COPY --from=publish /app/publish/cli ./cli

# Đổi ownership cho user alfred
RUN chown -R alfred:alfred /app

# Switch sang user alfred (không dùng root)
USER alfred

# Expose port
EXPOSE 8100

# Health check using wget (available by default in aspnet image)
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8100/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Alfred.Identity.WebApi.dll"]
