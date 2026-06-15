BUILD=dotnet run --project src/UrlShortener.Api

server:
	$(BUILD)

dev:
	$(BUILD) --launch-profile Development

test:
	dotnet test

.PHONY: server dev test