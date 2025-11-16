# Quick Start Guide - Python Bootstrap System

## Installation

The bootstrap system is part of `Beep.Python.Runtime`. No additional packages required.

## Simplest Usage (One Line)

```csharp
using Beep.Python.RuntimeEngine.Infrastructure;
using Beep.Python.RuntimeEngine.Templates;

// Create bootstrap manager (inject via DI or create manually)
var bootstrapManager = new PythonBootstrapManager(
    provisioner, registry, packageManager, venvManager, beepService);

// Setup Python environment in one call
var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.DataScience);

if (result.IsSuccessful)
{
    Console.WriteLine($"✅ Ready! Environment at: {result.EnvironmentPath}");
}
```

## What Just Happened?

The single call above:
1. ✅ Downloaded Python 3.11.9 embedded (~15MB) if not present
2. ✅ Extracted and configured Python with pip support
3. ✅ Created a virtual environment at `~/.beep-python/venvs/data-science`
4. ✅ Installed packages: pip, setuptools, wheel, numpy, pandas, matplotlib, scipy, scikit-learn
5. ✅ Verified the installation
6. ✅ Saved configuration to `~/.beep-python/` for future reuse

**Next run**: Takes only ~2 seconds (uses cached environment)

## Available Templates

```csharp
// Minimal - just Python and pip
EnvironmentTemplates.Minimal

// Data Science - numpy, pandas, matplotlib, scipy, scikit-learn
EnvironmentTemplates.DataScience

// Machine Learning - PyTorch, Transformers, etc.
EnvironmentTemplates.MachineLearning

// Web Development - Flask, Requests, BeautifulSoup4
EnvironmentTemplates.WebDevelopment

// Full Stack - Everything combined
EnvironmentTemplates.FullStack
```

## Custom Configuration

```csharp
var options = new BootstrapOptions
{
    EnvironmentName = "my-project",
    PackageProfiles = new List<string> { "base", "web" },
    VirtualEnvironmentPath = @"C:\MyApp\python-env"
};

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(options);
```

## Progress Tracking

```csharp
var progress = new Progress<BootstrapProgress>(p =>
{
    Console.WriteLine($"[{p.PercentComplete}%] {p.Message}");
});

var result = await bootstrapManager.EnsurePythonEnvironmentAsync(
    EnvironmentTemplates.MachineLearning,
    progress);
```

## Next Steps

- See **BOOTSTRAP_EXAMPLES.md** for 14 detailed examples
- See **IMPLEMENTATION_SUMMARY.md** for complete technical details
- See **PLAN.md** for the full enhancement roadmap

## Troubleshooting

**Q: Where are files stored?**  
A: All data goes to `~/.beep-python/` (C:\Users\{username}\.beep-python\)

**Q: How do I use an existing Python installation?**  
A: Set `EnsureEmbeddedPython = false` in BootstrapOptions

**Q: Can I add custom packages?**  
A: Yes! Edit `~/.beep-python/package-requirements.json` or create custom profiles via PackageRequirementsManager

**Q: What if download fails?**  
A: Enable progress tracking to see detailed error messages. Check network connectivity.

## Support

For issues or questions, refer to the implementation documentation or check the existing codebase examples.
