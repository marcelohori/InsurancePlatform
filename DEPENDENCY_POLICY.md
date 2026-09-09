# Dependency Update Policy

## Overview

This document defines how dependencies are managed and updated in the InsurancePlatformV01 project. The goal is to balance security, stability, and feature availability.

## Automated Updates

**GitHub Dependabot** automatically checks for updates weekly (Mondays 03:00 UTC) and creates pull requests for:

### NuGet Packages
- **Schedule**: Weekly, Monday 03:00 UTC
- **Limit**: Max 5 open PRs at a time
- **Auto-rebase**: Enabled to keep PRs fresh
- **Labels**: `dependencies`, `nuget`
- **Reviewers**: Primary maintainers

### GitHub Actions
- **Schedule**: Weekly, Monday 03:30 UTC (staggered after NuGet)
- **Labels**: `dependencies`, `github-actions`
- **Commit prefix**: `ci(actions):`

## Pinned Versions

### MassTransit (v8.5.10)
- **Reason**: v9+ requires commercial license; v8 is Apache 2.0
- **Decision**: Remains on v8 line indefinitely
- **Review date**: Revisit if licensing changes or open-source alternative emerges

### .NET Runtime (net10.0)
- **Current**: .NET 10.0
- **Policy**: Track LTS releases (8, 10, 12, ...) for production code
- **Upgrade timing**: ~3 months after LTS release (after patch cycles stabilize)

## Approval Process

### Patch & Minor Updates
- Dependabot PRs for patches (1.0.0 → 1.0.1) and minor versions (1.0.0 → 1.1.0) in non-critical packages are auto-approved if:
  - CI passes (tests, build, architecture rules)
  - No breaking changes in changelog
  
### Major Updates
- Require manual review and testing
- Check CHANGELOG for breaking changes
- Run integration tests locally before merge
- Updates to core dependencies (EF Core, ASP.NET Core, etc.) need extra attention

## Security Policy

- **Critical vulnerabilities**: Merged immediately regardless of version bump
- **High vulnerabilities**: Merged within 1 week
- **Medium vulnerabilities**: Bundled into next release cycle
- **Low vulnerabilities**: Reviewed in regular dependency audit

Run security audit manually:
```bash
dotnet list package --vulnerable
```

## Local Development

When working locally:

1. **Before starting**: `dotnet restore` to sync with latest compatible versions
2. **Before committing**: Run full build and tests
3. **Breaking change**: Add note to PR description with migration steps

## Documentation

- Dependencies and versions are centrally managed in `Directory.Packages.props`
- LangVersion and analysis settings in `Directory.Build.props`
- CI/CD configuration in `.github/workflows/` (future)

## Exceptions

Packages that deviate from auto-update policy:
- **MassTransit** (v8.5.10) — license constraint
- **StyleCop.Analyzers** — dev-only, low priority

## Feedback

To suggest changes to this policy:
1. Open an issue describing the change and rationale
2. Reference this policy document
3. Include any relevant CVE or security concerns
