.PHONY: up down build run test

up:
	docker compose up -d

down:
	docker compose down

build:
	dotnet build E-Commerce.sln

run:
	dotnet run --project src/E-Commerce.API

test:
	dotnet test E-Commerce.sln
