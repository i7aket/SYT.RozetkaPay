# SYT.RozetkaPay

`SYT.RozetkaPay` is a .NET SDK for RozetkaPay API integration.

## Package

- Package ID: `SYT.RozetkaPay`
- Target frameworks: `net9.0`, `net10.0`
- License: `MIT`

## Repository Structure

- SDK source: `src/SYT.RozetkaPay`
- Tests: `tests/SYT.RozetkaPay.Tests`
- NuGet publish workflow: `.github/workflows/publish-nuget.yml`

## NuGet Publish Flow

Push to `main` triggers GitHub Actions workflow that:
1. Restores and builds solution.
2. Runs tests.
3. Packs NuGet package.
4. Publishes to nuget.org when `NUGET_API_KEY` is configured in repository secrets.

## Maintainer

Maintained by **Anatoliy Yermakov** for RozetkaPay integrators.
