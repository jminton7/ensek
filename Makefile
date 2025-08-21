# Simple Makefile for the Ensek .NET 9 Web API + Tests + Docker
# Use with GNU Make (Git Bash, MSYS2, or WSL). Targets are cross-platform friendly.

# Paths
SLN := Ensek.sln
API_PROJ := Ensek/Ensek.csproj
TEST_PROJ := Ensek.Tests/Ensek.Tests.csproj
RUNSETTINGS := coverlet.runsettings

# Default
.PHONY: help
help:
	@echo "Available targets:"
	@echo "  restore        Restore NuGet packages"
	@echo "  build          Build the solution (Debug)"
	@echo "  build-release  Build the solution (Release)"
	@echo "  test           Run unit tests with coverage"
	@echo "  run            Run the API locally (Debug)"
	@echo "  watch          Run the API with hot reload"
	@echo "  migration name=YourMigration  Add a new EF Core migration"
	@echo "  migrate        Apply EF Core migrations to the configured database"
	@echo "  docker-up      Start Postgres and API via docker compose"
	@echo "  docker-down    Stop and remove docker compose services"
	@echo "  docker-logs    Tail API and DB logs"
	@echo "  clean          Clean build outputs"

.PHONY: restore
restore:
	dotnet restore "$(SLN)"

.PHONY: build
build: restore
	dotnet build "$(SLN)" -c Debug --no-restore

.PHONY: build-release
build-release: restore
	dotnet build "$(SLN)" -c Release --no-restore

.PHONY: test
# Uses runsettings at repo root to collect coverage
test: build
	dotnet test "$(SLN)" -c Debug --no-build \
		--settings "$(RUNSETTINGS)" --collect:"XPlat Code Coverage"

.PHONY: run
run: build
	dotnet run --project "$(API_PROJ)" -c Debug --no-build

.PHONY: watch
watch:
	dotnet watch --project "$(API_PROJ)" run -c Debug

# EF Core migrations
.PHONY: migration
migration:
ifndef name
	$(error Usage: make migration name=DescriptiveName)
endif
	dotnet ef migrations add $(name) -p "$(API_PROJ)" -s "$(API_PROJ)"

.PHONY: migrate
migrate:
	dotnet ef database update -p "$(API_PROJ)" -s "$(API_PROJ)"

# Docker Compose controls
.PHONY: docker-up
docker-up:
	docker compose up -d --build

.PHONY: docker-down
docker-down:
	docker compose down -v

.PHONY: docker-logs
docker-logs:
	-@docker compose logs -f --tail=100 api db

.PHONY: clean
clean:
	dotnet clean "$(SLN)"
	@echo "Removing bin/obj..."
ifeq ($(OS),Windows_NT)
	-@powershell -NoProfile -Command "Get-ChildItem -Recurse -Directory -Filter bin, obj | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }"
else
	-@find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + 2> /dev/null || true
endif
	@echo "Done."
