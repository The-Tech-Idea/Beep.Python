# Beep.Python Roadmap

This plan captures the immediate stabilization work as well as the strategic investments that expand the suite. It complements the top-level README.

## Guiding Objectives

- Deliver a consistent, testable runtime and packaging experience across host applications.
- Keep documentation and developer tooling aligned with current capabilities.
- Expand the library surface to cover adjacent workflows requested by customers (data fabric, AI inference, automation).

## Immediate Priorities (0-1 month)

- Finalize the rename from `Beep.Python.Hugginface` to `Beep.Python.AI.Transformers` across projects, namespaces, packaging, and documentation.
- Publish refreshed NuGet packages for `Beep.Python.Runtime`, `Beep.Python.PackageManagement`, `Beep.Python.DataManagement`, and `Beep.Python.ML`, including release notes.
- Produce solution-level build instructions and automate validation builds with GitHub Actions or Azure Pipelines.
- Add quick-start samples that demonstrate runtime registration, package installation, and dataframe manipulation inside the same host application.
- Introduce smoke tests that exercise `IPythonRunTimeManager`, session lifecycle, and package installation within a controlled environment.

## Near-Term Priorities (1-3 months)

- Stand up automated integration tests that launch the Python runtime, execute ML scripts, and run transformer pipelines with mocked providers.
- Harden environment management: detect missing interpreters, report virtual environment health, and surface diagnostics through the UI.
- Centralize configuration (Python paths, cache directories, credentials) in a structured settings service shared between desktop and server hosts.
- Implement documentation CI to publish the static HTML docs whenever `main` changes.
- Expand WinForms tooling to include dashboards for session health, environment packages, and queued jobs.

## Mid-Term Priorities (3-6 months)

- Layer in telemetry hooks that measure session lifetime, package operations, and runtime errors.
- Provide official Docker and container recipes for server-side hosting.
- Integrate secret management options (Azure Key Vault, AWS Secrets Manager, local secure store) for credentialed transformer providers.
- Establish backward compatibility guidelines and semantic versioning across the NuGet packages.
- Build template solutions that showcase hybrid workloads (data load + ML + transformer inference) for sales demos.

## Potential New Packages (parallel exploration)

| Package | Focus | Rationale |
| --- | --- | --- |
| `Beep.Python.DocumentAI` | Document parsing, OCR, and enrichment pipelines. | Bridges data ingestion with transformer-based understanding for enterprise content. |

## Operational Foundations

- Define consistent logging and error-handling patterns (structured logs, correlation IDs).
- Capture reproducible bug reports by scripting sample datasets and configurations.
- Align versioning and publishing scripts across all projects to avoid drift.

## Risks and Mitigations

- Python dependency drift: automate requirements snapshots and compare on build.
- Environment proliferation: create lifecycle policies and cleanup commands.
- Documentation staleness: tie doc updates to pull request templates and CI checks.

## Success Metrics

- Core NuGet packages published with updated documentation and release notes.
- CI pipeline green across runtime, data, ML, and transformer integration tests.
- At least one new package prototype (`DocumentAI`) with an end-to-end sample.
- BEEP UI surfaces health dashboards for sessions and environments.
