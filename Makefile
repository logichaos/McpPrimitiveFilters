.PHONY: build build-release format format-check lint test test-unit test-integration test-ci test-unit-ci test-integration-ci coverage coverage-lcov run run-sdk clean restore all help

help: ## Show this help
	@echo "Available targets:"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| sort \
		| awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-22s\033[0m %s\n", $$1, $$2}'

build: ## Build the solution
	dotnet build

build-release: ## Build in Release configuration
	dotnet build --configuration Release

restore: ## Restore NuGet packages
	dotnet restore

clean: ## Remove build artifacts
	dotnet clean
	rm -rf artifacts/

format: ## Apply C# code formatting
	dotnet format --verbosity normal

format-check: ## Check formatting without changing files (CI)
	dotnet format --verify-no-changes --verbosity diagnostic

lint: ## Build with warnings as errors
	dotnet build --warnaserror

test: ## Run all tests (unit + integration)
test: test-unit test-integration

test-unit: ## Run unit tests only
	dotnet test tests/McpServer.Unit.Tests

test-integration: ## Run integration tests only
	dotnet test tests/McpServer.Integration.Tests

test-ci: ## Run all tests with coverage + TRX (for CI)
test-ci: test-unit-ci test-integration-ci

test-unit-ci: ## Unit tests with coverage + TRX
	dotnet test tests/McpServer.Unit.Tests --configuration Release --no-build --results-directory ./TestResults -- --coverage --coverage-output-format cobertura --report-trx

test-integration-ci: ## Integration tests with coverage + TRX
	dotnet test tests/McpServer.Integration.Tests --configuration Release --no-build --results-directory ./TestResults -- --coverage --coverage-output-format cobertura --report-trx

coverage: ## Collect coverage and generate HTML report
	./scripts/coverage.sh

coverage-lcov: ## Convert cobertura coverage to LCOV
	dotnet tool install --global dotnet-reportgenerator-globaltool 2>/dev/null; \
	reportgenerator "-reports:./TestResults/*.cobertura.xml" -targetdir:./TestResults/lcov -reporttypes:lcov

run: ## Start the McpServer
	dotnet run --project src/McpServer

all: ## Format, build, and test everything
all: format build test
	@echo "✓ format + build + test passed"
