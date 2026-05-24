# Atlas of You

Atlas of You is a Turkish-first Blazor WebAssembly app that compares a user's physical profile with public reference distributions.

The app is intentionally local and privacy-light: the form runs in the browser, no account is required, and personal inputs are not sent to a backend.

Current scope is `v0.3.0`: height, BMI reference position, approximate reference-weight visualization, eye color, natural hair color, hair + eye combination rarity, hand preference, and blood group prevalence with confidence labels.

## Stack

- C# / .NET 9 Blazor WebAssembly for the app UI and calculations.
- C# class library for testable comparison logic.
- Python 3 standard-library pipeline for downloading and shaping NCD-RisC reference data.
- GitHub Pages deployment at `https://gnrvrm.github.io/Atlas_of_You/`.

## Data Pipeline

Run the pipeline from the repository root:

```powershell
& 'C:\msys64\ucrt64\bin\python.exe' scripts\build_reference_data.py
& 'C:\msys64\ucrt64\bin\python.exe' scripts\build_fun_traits.py
```

It downloads raw NCD-RisC files into `data/raw/` and writes the app payload to:

```text
src/AtlasOfYou.App/wwwroot/data/atlas-reference.json
src/AtlasOfYou.App/wwwroot/data/fun-traits.json
```

The `fun-traits.json` payload is intentionally approximate. It is used for prevalence and rarity language, not medical, genetic, or strict percentile claims.

## Local Development

```powershell
dotnet test AtlasOfYou.sln
dotnet run --project src\AtlasOfYou.App\AtlasOfYou.App.csproj
```

The app is a static web app. GitHub Pages serves the built HTML, CSS, JavaScript, WebAssembly, and JSON data files.
