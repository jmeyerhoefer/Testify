# Bachelor Thesis Workspace

This repository contains the implementation and analysis artifacts for Jakob Meyerhoefer's bachelor
thesis around beginner-friendly F# testing, build/test failure analysis, and supporting tooling.

## Main Projects

- [`Testify`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify)
  Main thesis project. Contains the `Testify` F# testing library, API tests, the `GdP23` replay and
  comparison pipeline, and the current DSL sketches.
- [`error-pattern`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/error-pattern)
  Earlier build/test-analysis pipeline used as reference during the thesis work.
- [`assertify`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/assertify)
  Related earlier experimentation around assertion APIs and reporting.
- [`latex`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/latex)
  Thesis sources and supporting LaTeX material.
- [`evaluation`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/evaluation)
  Evaluation-related notebooks, scripts, or intermediate analysis artifacts.

## Status

- `Testify` is currently treated as frozen at source version `0.1.0`.
- The final selected `GdP23` comparison run was regenerated cleanly and currently yields `0` diffs
  across all paired rewritten tasks in the selected sample.
- Generated result exports under `Testify/Testify/GdP23/DockerResults/` are intentionally kept as
  local analysis artifacts and are no longer versioned in git.

## Working With Testify

From [`Testify`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify), the main commands are:

```powershell
dotnet build .\Testify\Testify\Testify.fsproj --no-restore
dotnet test .\Testify\Testify\Testify.ApiTests\Testify.ApiTests.fsproj --no-build --no-restore
dotnet build .\Testify\Testify\GdP23\GdP23.fsproj --no-restore
```

The main usage guide lives in:

- [`Testify/README.md`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/README.md)

That README contains:

- installation/build instructions
- a minimal `Testify` example
- Assert DSL operator examples
- Check DSL operator examples
- named `Check.should*` helper examples
- notes about replay/comparison work in `GdP23`

## Thesis-Relevant Local Outputs

The most useful current local files for analysis are:

- [`selected-comparisons.csv`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/DockerResults/selected-comparisons.csv)
- [`selected-failures-only.csv`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/DockerResults/selected-failures-only.csv)
- [`comparisons.jsonl`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/DockerResults/comparisons.jsonl)
- [`TestifyDslSketch.tex`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/docs/TestifyDslSketch.tex)
- [`TestifyDslSketch.md`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/docs/TestifyDslSketch.md)

## License

This workspace is licensed under the GNU General Public License v3.0. See
[`LICENSE`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/LICENSE).
