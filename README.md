# Confidra — ConfidraAPI

ASP.NET Core 8 care API with server-side sessions, consent, RBAC and test-only payment reconciliation.

## Local validation

```text
dotnet restore tests/ConfidraApi.Tests.csproj
dotnet test tests/ConfidraApi.Tests.csproj
```

## Release status

Feature branch only. Do not deploy to production. See `docs/RELEASE.md` for integration limitations and approval gates. No credentials or real patient data belong in this repository.
