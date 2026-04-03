# Bachelor Thesis Workspace

This repository grew over the course of the bachelor thesis, so it is a mix of the actual `Testify`
implementation, older experiments, evaluation code, and thesis material. The center of gravity is
the `Testify` project, but the surrounding folders are still useful for understanding where the final
design came from and how it was evaluated.

## Main Projects

- [`Testify`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify)
  The main project. This is where the `Testify` library lives, together with its API tests, the
  `GdP23` replay/comparison pipeline, and the current DSL notes.
- [`error-pattern`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/error-pattern)
  Earlier build/test-analysis pipeline that served as a reference point during the thesis work.
- [`assertify`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/assertify)
  Older experiments around assertion APIs and reporting.
- [`latex`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/latex)
  Thesis sources and supporting LaTeX material.
- [`evaluation`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/evaluation)
  Evaluation scripts, notes, and intermediate analysis artifacts.

## Current Status

- `Testify` is currently treated as frozen at source version `0.1.0`.
- The final selected `GdP23` comparison run was regenerated cleanly and currently yields `0` diffs
  across all paired rewritten tasks in the selected sample.
- Generated result exports under `Testify/Testify/GdP23/DockerResults/` are intentionally kept as
  local analysis artifacts and are no longer versioned in git.

## Working With Testify

If you want to work on the main project, start in [`Testify`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify).
The commands I ended up using most often are:

```powershell
dotnet build .\Testify\Testify\Testify.fsproj --no-restore
dotnet test .\Testify\Testify\Testify.ApiTests\Testify.ApiTests.fsproj --no-build --no-restore
dotnet build .\Testify\Testify\GdP23\GdP23.fsproj --no-restore
```

The more useful usage guide lives here:

- [`Testify/README.md`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/README.md)

It includes:

- installation/build instructions
- a minimal `Testify` example
- Assert DSL operator examples
- Check DSL operator examples
- named `Check.should*` helper examples
- notes about replay/comparison work in `GdP23`

## Useful Local Outputs

For the thesis work, these are the files I would open first:

- [`selected-comparisons.csv`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/DockerResults/selected-comparisons.csv)
- [`selected-failures-only.csv`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/DockerResults/selected-failures-only.csv)
- [`comparisons.jsonl`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/DockerResults/comparisons.jsonl)
- [`TestifyDslSketch.tex`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/docs/TestifyDslSketch.tex)
- [`TestifyDslSketch.md`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/docs/TestifyDslSketch.md)

## License

This workspace is licensed under the GNU General Public License v3.0. See
[`LICENSE`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/LICENSE).
