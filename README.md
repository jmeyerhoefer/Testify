# Bachelor Thesis Workspace

This repo is the trimmed-down working set for the thesis. At this point it keeps the parts that still
matter day to day: the `Testify` codebase, the LaTeX sources, and the small amount of root-level project
infrastructure around them.

## What Is Kept Here

- [`Testify`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify)
  The implementation work. This includes the `Testify` library, API tests, the `GdP23` comparison
  pipeline, and the DSL notes used for the thesis.
- [`latex`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/latex)
  Thesis sources and supporting LaTeX material.
- [`README.md`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/README.md), [`LICENSE`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/LICENSE), and
  [`.gitlab-ci.yml`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/.gitlab-ci.yml)
  The small amount of root-level documentation and pipeline configuration that still belongs with the repo.

## Current Status

- `Testify` is currently treated as frozen at source version `0.1.0`.
- The final selected `GdP23` comparison run was regenerated cleanly and currently yields `0` diffs
  across all paired rewritten tasks in the selected sample.
- Generated result exports under `Testify/Testify/GdP23/DockerResults/` are intentionally kept as
  local analysis artifacts and are no longer versioned in git.

## Working With Testify

If you want to work on the main project, start in [`Testify`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify).
At the top level, it is probably more useful to show what using the library feels like than to list
build commands:

```fsharp
open Testify
open Testify.AssertOperators

<@ 1 + 2 @> =? 3
```

The more detailed usage guide lives here:

- [`Testify/README.md`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/README.md)

It includes:

- installation/build instructions
- a minimal `Testify` example
- Assert DSL operator examples
- Check DSL operator examples
- named `Check.should*` helper examples
- notes about replay/comparison work in `GdP23`

## Useful Local Outputs

For the thesis work, these are the files I would open first locally:

- [`selected-comparisons.csv`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/DockerResults/selected-comparisons.csv)
- [`selected-failures-only.csv`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/DockerResults/selected-failures-only.csv)
- [`comparisons.jsonl`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/GdP23/DockerResults/comparisons.jsonl)
- [`TestifyDslSketch.tex`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/docs/TestifyDslSketch.tex)
- [`TestifyDslSketch.md`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/Testify/Testify/docs/TestifyDslSketch.md)

## License

This workspace is licensed under the GNU General Public License v3.0. See
[`LICENSE`](/D:/Bachelorarbeit/24-ba-jakob-meyerhoefer/LICENSE).
