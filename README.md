# Atlas of You

Atlas of You is a Turkish-first Blazor WebAssembly app that compares a user's physical profile with public reference distributions.

The first MVP is intentionally local and privacy-light: the form runs in the browser, no account is required, and personal inputs are not sent to a backend.

## Stack

- C# / .NET 9 Blazor WebAssembly for the app UI and calculations.
- C# class library for testable comparison logic.
- Python 3 standard-library pipeline for downloading and shaping NCD-RisC reference data.
- GitHub Pages deployment at `https://gnrvrm.github.io/Atlas_of_You/`.

## Data Pipeline

Run the pipeline from the repository root:

```powershell
& 'C:\msys64\ucrt64\bin\python.exe' scripts\build_reference_data.py
```

It downloads raw NCD-RisC files into `data/raw/` and writes the app payload to:

```text
src/AtlasOfYou.App/wwwroot/data/atlas-reference.json
```

## Local Development

```powershell
dotnet test AtlasOfYou.sln
dotnet run --project src\AtlasOfYou.App\AtlasOfYou.App.csproj
```

The app is a static web app. GitHub Pages serves the built HTML, CSS, JavaScript, WebAssembly, and JSON data files.
