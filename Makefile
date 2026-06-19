BUILD=dotnet run --project src/UrlShortener.Api

server:
	$(BUILD)

dev:
	$(BUILD) --launch-profile Development

db-update:
	dotnet ef database update \
	--project src/UrlShortener.Infrastructure \
	--startup-project src/UrlShortener.Api

test:
	dotnet test

.PHONY: server dev test