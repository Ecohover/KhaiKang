# Verification Context

Read this file before reporting a completed code change.

## Backend Commands

Run the commands relevant to the change:

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile NuGet.config
dotnet build backend/KhaiKang.Backend.slnx --no-restore --disable-build-servers -m:1
dotnet test backend/KhaiKang.Backend.slnx --no-build --disable-build-servers -m:1
```

Frontend commands will be added after the frontend workspace is scaffolded.

## Verification Rules

- Tests must validate public behavior and contracts, not private details.
- Unit tests must not depend on live infrastructure, current time, arbitrary
  sleeps, or external APIs.
- Use integration tests for HTTP, authorization, persistence, migrations, and
  infrastructure boundaries.
- Generated output, migrations, and contract artifacts must be reviewed.
- CI, compiler, analyzers, and test output are evidence. AI reasoning alone is
  not verification.

## Completion Report

Report these four items concisely:

1. Changed: files and behavior changed.
2. Verified: commands actually run and their results.
3. Not verified: checks that could not be run and why.
4. Risks: known limitations, compatibility concerns, or follow-up work.
