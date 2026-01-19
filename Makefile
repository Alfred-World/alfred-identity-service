# HSE Backend Makefile - Development & Production

# Configuration
PROJECT=src/HSE.Infrastructure
STARTUP=src/HSE.WebApi
CLI_PROJECT=src/HSE.Cli
OUTPUT_DIR=Migrations

DOCKER_IMAGE=hse-api
DOCKER_TAG=latest
ENV_FILE=.env.production

# Timestamp for Cache Busting
NOW := $(shell date +%s)

# Load environment variables
ifneq (,$(wildcard $(ENV_FILE)))
    include $(ENV_FILE)
    export
endif

# Default DATA_PATH if not set in .env
DATA_PATH ?= ./data

.PHONY: help add remove update list seed seed-force seed-new seed-resync docker-clean
.PHONY: prod-deploy prod-start prod-stop prod-restart prod-logs prod-status prod-health prod-seed prod-db-shell
.PHONY: prod-backup prod-restore

help:
	@echo "======================================"
	@echo "HSE Development & Production Tool"
	@echo "======================================"
	@echo ""
	@echo " Migration (Development):"
	@echo "  make add NAME=<name>   Add new migration"
	@echo "  make remove            Remove last migration"
	@echo "  make update            Update database"
	@echo "  make list              List migrations"
	@echo ""
	@echo "🌱 Seed Data (Development):"
	@echo "  make seed              Seed database"
	@echo "  make seed-force        Force re-run all seeds"
	@echo "  make seed-new NAME=<name> Create new seeder"
	@echo "  make seed-resync       Delete all data and re-seed (IDs start from 1)"
	@echo ""
	@echo "🏗️  Build & Run:"
	@echo "  make build             Build solution"
	@echo "  make run               Run WebApi"
	@echo "  make test              Run tests"
	@echo ""
	@echo "🧹 Docker:"
	@echo "  make docker-clean      Remove old images"
	@echo ""
	@echo "🚀 Production:"
	@echo "  make prod-deploy       Build & deploy (includes migrations)"
	@echo "  make prod-start        Start services"
	@echo "  make prod-stop         Stop services"
	@echo "  make prod-restart      Restart services"
	@echo "  make prod-logs         View live logs"
	@echo "  make prod-status       Check container status"
	@echo "  make prod-health       Health check"
	@echo "  make prod-seed         Seed production database"
	@echo "  make prod-db-shell     Connect to SQL Server"
	@echo ""
	@echo "💾 Data Management:"
	@echo "  make prod-backup       Backup all production data"
	@echo "  make prod-restore      List available backups"
	@echo ""

# ============================================
# Migration Targets (Development)
# ============================================

add:
	@if [ -z "$(NAME)" ]; then echo "❌ Usage: make add NAME=<migration_name>"; exit 1; fi
	@echo "📝 Adding migration: $(NAME)"
	dotnet ef migrations add "$(NAME)" --project "$(PROJECT)" --startup-project "$(STARTUP)" --output-dir "$(OUTPUT_DIR)"
	@echo "✅ Migration added!"

remove:
	@echo "🗑️  Removing last migration..."
	dotnet ef migrations remove --project "$(PROJECT)" --startup-project "$(STARTUP)"
	@echo "✅ Migration removed!"

update:
	@echo "🔄 Updating database..."
	dotnet ef database update --project "$(PROJECT)" --startup-project "$(STARTUP)"
	@echo "✅ Database updated!"

list:
	@echo "📋 Listing migrations..."
	dotnet ef migrations list --project "$(PROJECT)" --startup-project "$(STARTUP)"

# ============================================
# Seed Targets (Development)
# ============================================

seed:
	@echo "🌱 Seeding database..."
	dotnet run --project "$(CLI_PROJECT)" seed
	@echo "✅ Seed complete!"

seed-force:
	@echo "🌱 Force seeding database..."
	dotnet run --project "$(CLI_PROJECT)" seed --force
	@echo "✅ Force seed complete!"

seed-new:
	@echo "🌱 Creating new seed command..."
	@if [ -z "$(NAME)" ]; then echo "❌ Usage: make seed-new NAME=<seeder_name>"; exit 1; fi
	@echo "📝 Creating seeder: $(NAME)"
	@dotnet run --project "$(CLI_PROJECT)" create-seeder "$(NAME)"
	@echo "✅ Seeder created! Add your seed logic to the generated file."

seed-resync:
	@echo "🔄 Resyncing database (delete data and re-seed with ID from 1)..."
	@echo "⚠️  This will delete all existing data!"
	@read -p "Are you sure? (y/n) " -n 1 -r; \
	echo; \
	if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		echo "🗑️  Deleting all data..."; \
		dotnet run --project "$(CLI_PROJECT)" seed --resync; \
		echo "✅ Database resynced and seeded with fresh data (IDs start from 1)!"; \
	else \
		echo "❌ Cancelled."; \
	fi

# ============================================
# Build & Run
# ============================================

build:
	@echo "🔨 Building solution..."
	dotnet build
	@echo "✅ Build complete!"

run:
	@echo "🚀 Running WebApi..."
	dotnet run --project "$(STARTUP)"

watch:
	@echo "👀 Running WebApi with hot reload..."
	dotnet watch --project "$(STARTUP)"

test:
	@echo "🧪 Running tests..."
	dotnet test
	@echo "✅ Tests complete!"

# ============================================
# Docker Cleanup
# ============================================

docker-clean:
	@echo "🧹 Cleaning Docker images..."
	@docker rmi $(DOCKER_IMAGE):$(DOCKER_TAG) 2>/dev/null || true
	@docker system prune -f
	@echo "✅ Cleanup complete!"

# ============================================
# Production Deployment
# ============================================

prod-deploy:
	@echo "🚀 [1/4] Creating data directories at $(DATA_PATH)..."
	@mkdir -p $(DATA_PATH)/sqlserver $(DATA_PATH)/minio $(DATA_PATH)/seq $(DATA_PATH)/redis
	
	@echo "🚀 [2/4] Building Docker image..."
	@docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) build --build-arg CACHEBUST=$(NOW)
	
	@echo "🚀 [3/4] Stopping API and starting services..."
	@docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) stop hse-api 2>/dev/null || true
	@docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) rm -f hse-api 2>/dev/null || true
	@docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) up -d
	
	@echo "🚀 [4/4] Cleaning unused images..."
	@docker image prune -f
	@echo "✅ Deploy complete!"
	@sleep 2
	@make prod-health

prod-start:
	@echo "▶️  Starting services..."
	docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) start
	@echo "✅ Services started!"

prod-stop:
	@echo "⏹️  Stopping services..."
	docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) stop
	@echo "✅ Services stopped!"

prod-restart:
	@echo "🔄 Restarting services..."
	docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) restart
	@echo "✅ Services restarted!"

prod-logs:
	docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) logs -f

prod-status:
	docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) ps

prod-health:
	@echo "🏥 Checking health..."
	@if curl -sf http://localhost:8000/health > /dev/null 2>&1; then \
		echo "  ✅ API is healthy"; \
	else \
		echo "  ⏳ API still starting..."; \
	fi

prod-seed:
	@echo "🌱 Seeding production database..."
	docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) exec -T hse-api dotnet HSE.Cli.dll seed
	@echo "✅ Seed complete!"

prod-db-shell:
	@echo "🗄️  Connecting to SQL Server..."
	docker compose -f docker-compose.prod.yml --env-file $(ENV_FILE) exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "$${DB_PASSWORD}"

# ============================================
# Data Backup & Restore
# ============================================

prod-backup:
	@echo "📦 Backing up production data..."
	@bash scripts/backup-prod-data.sh
	@echo "✅ Backup saved to ./backups/"

prod-restore:
	@echo "📋 Available backups:"
	@ls -lh backups/hse-backup-*.tar.gz 2>/dev/null || echo "  No backups found"
	@echo ""
	@echo "To restore: tar -xzf backups/hse-backup-YYYYMMDD_HHMMSS.tar.gz"

# ============================================
# Quick Setup (Development)
# ============================================

setup:
	@echo "⏳ Waiting for SQL Server to be ready..."
	@sleep 5
	@echo "🔨 Building solution..."
	@dotnet build
	@echo "📦 Creating initial migration..."
	@dotnet ef migrations add Initial --project "$(PROJECT)" --startup-project "$(STARTUP)" --output-dir "$(OUTPUT_DIR)" || true
	@echo "🔄 Updating database..."
	@dotnet ef database update --project "$(PROJECT)" --startup-project "$(STARTUP)"
	@echo "✅ Setup complete! Run 'make run' to start the application."

clean:
	@echo "🧹 Cleaning build artifacts..."
	@dotnet clean
	@find . -type d -name "bin" -o -name "obj" | xargs rm -rf
	@echo "✅ Clean complete!"
