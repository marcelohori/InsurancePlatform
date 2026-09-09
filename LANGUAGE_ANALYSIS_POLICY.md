# C# Language & Code Analysis Policy

## Language Version

**Configuration**: `LangVersion=latest` in `Directory.Build.props`

### Rationale
- **Latest**: Leverages newest C# features for safety, performance, and clarity (e.g., nullable reference types, records, init-only properties, pattern matching, required fields)
- **Stability**: .NET 10 ships stable language features; pre-release language features are off by default
- **Team alignment**: All developers use the same language version across the project

### .NET Version
- **Target**: `net10.0` (LTS expected in Nov 2025)
- **SDK minimum**: .NET 10 SDK for building
- **Upgrade cadence**: Every LTS release (~2 years)

## Code Analysis

**Configuration**: `Directory.Build.props:11-12`

```xml
<AnalysisLevel>latest</AnalysisLevel>
<AnalysisMode>Recommended</AnalysisMode>
```

### .NET Analyzers
- **Level**: `latest` — tracks newest rules and diagnostics
- **Mode**: `Recommended` — balances safety and productivity (excludes overly strict rules)
- **Enforcement**: `TreatWarningsAsErrors=true` — build fails on analyzer violations

### StyleCop Analyzers
- **Version**: 1.2.0-beta.556 (pre-release for .NET 10 compatibility)
- **Configuration**: `stylecop.json`
  - Disables XML documentation (CS1591) — pragmatic for internal code
  - Enforces using-directive ordering (OutsideNamespace)
  - Enforces company name metadata

### Architecture Rules
- `Architecture.Tests` enforces:
  - Clean Architecture layering (Domain → Application → Infrastructure → Api)
  - No circular dependencies
  - Domain layer has no external references (no EF Core, HTTP, etc.)

## When to Ignore Warnings

Only suppress warnings via `#pragma warning disable` in **specific cases**:
- **Known false positives**: Analyzer incorrectly flags safe code (rare with `latest`)
- **Intentional design**: Documented why the pattern is needed despite the warning
- **Scope**: Always use `#pragma ... restore` to re-enable at the narrowest scope

**Never suppress without a comment** — future developers must know why.

Example:
```csharp
#pragma warning disable CA1062 // Null-check analyzer doesn't understand DDD invariants
public void ApplyDomainEvent(DomainEvent @event) { ... }
#pragma warning restore CA1062
```

## Future Considerations

- **C# 13 / .NET 11**: Review and adopt new features (expected 2026)
- **Nullable reference types**: Currently enabled globally; perfect for this codebase
- **Record structs**: Adopt once we know target .NET versions better

## Validation

Before committing:
```bash
dotnet build InsurancePlatformV01.slnx  # Fails on any analyzer warning
dotnet format --verify-no-changes       # Code style consistency
```
