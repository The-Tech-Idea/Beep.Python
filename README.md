# Beep.Python

Beep.Python is The Tech Idea's enterprise-grade toolkit for embedding Python inside the BEEP platform and other .NET applications. The solution orchestrates Python runtimes, isolates virtual environments, manages packages, automates data workflows, and exposes multimodal AI pipelines through a consistent set of services, view-models, and UI integrations.

## Highlights

- Unified runtime hosting built on Python.NET with session, environment, and execution managers for multi-user workloads.
- Domain libraries for data management (pandas automation), machine learning templates, and transformer pipelines (Hugging Face, OpenAI, Azure, local models, and multimodal orchestration).
- Package and environment management services that keep Python dependencies aligned with .NET deployments.
- Visual extensions, nodes, and WinForms tooling that plug into the BEEP designer to manage Python assets without leaving the platform.
- Extensive documentation suite (60+ HTML pages) covering API usage, workflows, and enterprise scenarios.

## Solution Map

| Project | Purpose |
| --- | --- |
| `Beep.Python.Model` | Contracts shared across the suite (runtime, ML, data, package, workflow interfaces). |
| `Beep.Python.Runtime` | Python.NET runtime host, session manager, execution and diagnostics helpers. |
| `Beep.Python.PackageManagement` | Requirements parsing, package categories, and session-aware installers. |
| `Beep.Python.DataManagement` | Pandas-driven workflows and helpers for dataset ingest, cleansing, and analysis. |
| `Beep.Python.ML` | Template-based ML engine with 100+ Python scripts and assistant classes. |
| `Beep.Python.Hugginface` | Transformer and multimodal pipelines (being renamed to `Beep.Python.AI.Transformers`). |
| `Beep.Python.Extensions` | BEEP extension entry points and UI commands. |
| `Beep.Python.Nodes` | Visual tree nodes and command surfaces for the designer. |
| `Beep.Python.Winform*` | Desktop shells for runtime management experiences. |
| `Beep.Python.Services` | Dependency injection helpers to register Python components. |
| `Beep.Python.Suite.Docs` and docs in each project | Published documentation assets referenced throughout the suite. |
| `Beep.Python.DocumentAI` | Document ingestion, OCR, and enrichment pipeline coordinator for downstream AI workflows. |

## Getting Started

1. Install `.NET 8 SDK` (the solution targets multiple frameworks, but .NET 8 is the working baseline).
2. Install `Python 3.8+` and ensure the Python executable is on your PATH.
3. Clone the repository and open `Beep.Python.sln` in Visual Studio 2022 or JetBrains Rider.
4. Restore NuGet packages with `dotnet restore` and build the solution with `dotnet build`.
5. Configure the runtime service in your host application:
   ```csharp
   services.RegisterPythonServices(@"C:\Path\To\Python");
   PythonServices.ConfigureServiceProvider(serviceProvider);
   ```
6. Explore feature documentation:
   - `Beep.Python.DataManagement/docs/index.html` for pandas workflows.
   - `Beep.Python.ML/docs/index.html` for ML automation.
   - `Beep.Python.PackageManagement/docs/index.html` for dependency flows.
   - `Beep.Python.Hugginface/README_MultimodalPipeline.md` for transformer pipelines.

## Working With the Runtime

- Use `IPythonRunTimeManager` to spin up isolated sessions per user or workload.
- Manage environments through `IPythonVirtualEnvManager` and keep dependencies in sync with `IPythonPackageManager`.
- Execute Python scripts via `IPythonCodeExecuteManager` or the ML/data helpers that wrap it.
- Visual integrations (`Beep.Python.Extensions`, `Beep.Python.Nodes`) surface these services in the BEEP UI.

## Documentation

A full list of available guides and coverage metrics lives in `DOCUMENTATION_COMPLETION_SUMMARY.md`. Each project exposes HTML documentation (60+ pages) with search and consistent styling. Start with the summary, then jump into the project-specific `docs/` folders.

## Roadmap

See `plan.md` for the actionable roadmap that accompanies this README. Immediate items include finalizing the `Beep.Python.AI.Transformers` rename, tightening automated tests, and publishing NuGet packages for the core components.

## Contributing

1. Fork the repository and create feature branches per change.
2. Run solution builds and any available tests before submitting pull requests.
3. Update or extend documentation when new features ship.
4. Follow the naming and architecture conventions laid out in `DOCUMENTATION_COMPLETION_SUMMARY.md` to keep the suite consistent.

## License

Distributed under the MIT License. See `LICENSE.txt` for details.
