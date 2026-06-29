# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.8] — 2026-06-29

### Added

- CI/CD pipeline via GitLab CI (build, test, publish to internal NuGet, release jobs).
- `GitVersion.yml` — GitVersion configuration following RapidSoft GitFlow conventions.
- `Directory.Build.props` — project-wide MSBuild settings (`LangVersion=12`, `Nullable=enable`, analyzers).
- `nuget.config` — NuGet feed configuration with internal proxy and audit source.
- Dictionary files (`.wecantspell`, `.directory.dic`) for WeCantSpell spell-check configuration.

### Fixed

- Dictionaries declared as Roslyn `AdditionalFiles` are now tracked compilation inputs: editing a
  dictionary takes effect on the next `dotnet build` without restarting the build server
  (fixes RSSL-11794). The existing on-disk discovery is kept as a fallback for consumers that have
  not opted into the new packaging.
- "Add to project dictionary" code fix now clears the squiggle in the IDE within ~1 s, instead of
  the previous up-to-~30 s wait. When the dictionary is a tracked `AdditionalFile`, the fix updates
  only the workspace `AdditionalDocument` (via `WithAdditionalDocumentText`) and lets the IDE host
  apply and persist it. The previous version *also* wrote the file directly, which the IDE saw as a
  competing external change and reconciled only on window focus — masking the in-process update and
  causing the delay (verified in Rider: with the direct write removed, the diagnostic clears in
  ~0.26 s and the host still persists the word to disk). A direct on-disk write is kept only as the
  fallback when the dictionary is not declared as an `AdditionalFile`.
- "Add to project dictionary" no longer writes duplicate entries. The code-fix host invokes the
  change callback more than once (preview + apply), so the previous unconditional append wrote the
  same word several times — the cause of the many duplicate entries seen in project dictionaries.
  Both the in-process `AdditionalDocument` update and the on-disk fallback append now skip words that
  are already present.
- Packaged MSBuild props (`build/WeCantSpell.Roslyn.props`) failed to evaluate in consuming
  projects with `MSB4092` ("unexpected token"): the ancestor-dictionary lookup inlined
  `[MSBuild]::GetPathOfFileAbove('.spelling.dic', …)` — whose single-quoted arguments cannot be
  tokenized inside a single-quoted item `Condition`. Each path is now resolved once into a
  property and the `Condition` only compares that property.

### Changed

- Test and benchmark projects retargeted to `net10.0` (the analyzer stays `netstandard2.0`, as
  required for a Roslyn analyzer); build runs on the .NET 10 SDK.
- Replaced `FluentAssertions` with `AwesomeAssertions` 9.4.0 — an Apache-2.0 drop-in fork; the
  custom `DiagnosticAssertions` was migrated to the v8 `AssertionChain` API and the namespace
  changed from `FluentAssertions` to `AwesomeAssertions`.
- Updated test/build dependencies (Roslyn Workspaces 4.14, xUnit 2.9, `Microsoft.NET.Test.Sdk`
  17.14, `Microsoft.Build.Locator` 1.7.8) and pinned transitive `Microsoft.Build.Tasks.Core`
  (17.14.28) and `System.Security.Cryptography.Xml` (10.0.9) to patched versions, clearing the
  high-severity NuGet audit failures (CVE-2025-26646, CVE-2026-33116, CVE-2026-26171) that broke
  the CI build. Framework-provided transitive packages (`System.Text.Json`, `System.Net.Http`,
  `System.Text.RegularExpressions`) are pruned by the .NET 10 SDK.
- `Directory.Build.targets` updated: version is no longer hard-coded; `IsCI` detection added;
  `TreatWarningsAsErrors` enabled in CI; NuGet Audit enabled (audit warnings are errors only in
  CI); test projects auto-detected and configured with `coverlet.collector` and
  `JunitXml.TestLogger`.

## [2.0.8-beta] — 2025-01-01

### Fixed

- Created global file-system cache for word lists, updated by code fix and refreshed every 100 ms
  (issue [#3](https://github.com/rpsft/WeCantSpell.Roslyn/issues/3)).
- Added compatibility with VS2022 build: `System.Text.Json` replaced with Newtonsoft.Json;
  `Microsoft.CodeAnalysis` downgraded to 3.11.
