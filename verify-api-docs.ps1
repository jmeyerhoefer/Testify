Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Set-Location $PSScriptRoot

$xmlPath = Join-Path $PSScriptRoot "src\Testify\bin\Debug\net10.0\Testify.xml"

if (-not (Test-Path $xmlPath)) {
    throw "API docs coverage check failed:`nMissing XML documentation file '$xmlPath'. Build the library first."
}

[xml]$xml = Get-Content -Path $xmlPath
$members = @($xml.doc.members.member)
$missing = New-Object System.Collections.Generic.List[string]

$requiredPages = @(
    "site-docs\index.md",
    "site-docs\getting-started.md",
    "site-docs\dsl-and-mental-model.md",
    "site-docs\assertions.md",
    "site-docs\properties.md",
    "site-docs\expectations.md",
    "site-docs\configuration.md",
    "site-docs\global-configuration.md",
    "site-docs\hints.md",
    "site-docs\results-reporting.md",
    "site-docs\operator-cheat-sheet.md",
    "site-docs\integrations.md",
    "site-docs\examples.md",
    "site-docs\power-showcase.md",
    "site-docs\migration.md",
    "site-docs\_template.html",
    "site-docs\assets\testify-mental-model.svg",
    "site-docs\assets\result-vs-should.svg",
    "site-docs\assets\property-check-pipeline.svg"
)

foreach ($relativePath in $requiredPages) {
    $fullPath = Join-Path $PSScriptRoot $relativePath
    if (-not (Test-Path $fullPath)) {
        $missing.Add("Missing required docs artifact '$relativePath'.")
    }
}

$docsToScan = @(
    Join-Path $PSScriptRoot "README.md"
    Join-Path $PSScriptRoot "site-docs"
)

$forbiddenPublicDocPatterns = @(
    "Assert\.check\b",
    "Check\.That\b",
    "(?-i:\bShould\.[A-Z][A-Za-z0-9_]*)",
    "equalToReferenceBy\b",
    "equalToReferenceWith\b"
)

foreach ($target in $docsToScan) {
    $files =
        if ((Get-Item $target) -is [System.IO.DirectoryInfo]) {
            Get-ChildItem -Path $target -Recurse -File
        } else {
            Get-Item $target
        }

    foreach ($file in @($files)) {
        $matchInfos = Select-String -Path $file.FullName -Pattern $forbiddenPublicDocPatterns
        foreach ($matchInfo in @($matchInfos)) {
            $missing.Add("Public docs still mention removed API surface: $($matchInfo.Path):$($matchInfo.LineNumber): $($matchInfo.Line.Trim())")
        }
    }
}

function Get-MemberXml {
    param(
        [string]$Label,
        [string]$Pattern
    )

    $matches = @($members | Where-Object { $_.name -match $Pattern })

    if ($matches.Count -eq 0) {
        $missing.Add("Missing documented XML member for '$Label' matching '$Pattern'.")
        return $null
    }

    if ($matches.Count -gt 1) {
        $exact = $matches | Select-Object -First 1
        return $exact
    }

    return $matches[0]
}

function Require-Tag {
    param(
        [System.Xml.XmlElement]$Member,
        [string]$Label,
        [string]$TagName
    )

    if ($null -eq $Member) {
        return
    }

    if ($Member.InnerXml -notmatch "<$TagName(\s|>)") {
        $missing.Add("Missing <$TagName> for '$Label'.")
    }
}

function Require-Params {
    param(
        [System.Xml.XmlElement]$Member,
        [string]$Label,
        [string[]]$Params
    )

    if ($null -eq $Member) {
        return
    }

    foreach ($parameter in $Params) {
        if ($Member.InnerXml -notmatch "<param\s+name=`"$([regex]::Escape($parameter))`"") {
            $missing.Add("Missing <param name=`"$parameter`"> for '$Label'.")
        }
    }
}

function Require-FileContains {
    param(
        [string]$RelativePath,
        [string[]]$Patterns
    )

    $fullPath = Join-Path $PSScriptRoot $RelativePath

    if (-not (Test-Path $fullPath)) {
        $missing.Add("Missing required docs artifact '$RelativePath'.")
        return
    }

    $text = Get-Content -Path $fullPath -Raw

    foreach ($pattern in $Patterns) {
        if ($text -notmatch $pattern) {
            $missing.Add("Docs page '$RelativePath' is missing expected content matching '$pattern'.")
        }
    }
}

Require-FileContains -RelativePath "site-docs\hints.md" -Patterns @(
    '\bGenericHints\b',
    '\bStringHints\b',
    '\bPropertyHints\b',
    '\bMiniHints\b',
    '\bBuiltInHintPacks\b',
    '\bHintInference\b',
    '\bTestifyHintRule\b',
    '\bTestifyHintPack\b',
    '\bHintTextField\b',
    'What Is Mappable',
    'Diff\.tryDescribe'
)

Require-FileContains -RelativePath "site-docs\dsl-and-mental-model.md" -Patterns @(
    'embedded testing DSL',
    'One Idea, Three Syntax Layers',
    'result` vs `should',
    'Why Quotations Matter',
    'Property Checks As “Generated Differential Testing”'
)

Require-FileContains -RelativePath "site-docs\global-configuration.md" -Patterns @(
    'Testify\.configure',
    'currentConfiguration',
    'resetConfiguration',
    'override the defaults for that call'
)

Require-FileContains -RelativePath "site-docs\operator-cheat-sheet.md" -Patterns @(
    'Apply one reusable expectation',
    'chain-friendly property syntax',
    'Callback-built fail-fast property',
    'Logical OR of expectations',
    'Logical AND of expectations',
    'Chainable'
)

Require-FileContains -RelativePath "site-docs\examples.md" -Patterns @(
    'equalBy',
    'equalByKey',
    'equalWith',
    'Check\.shouldBy',
    'TryGetReplayConfig',
    'Testify\.configure',
    'Generators\.',
    'Arbitraries\.'
)

Require-FileContains -RelativePath "site-docs\results-reporting.md" -Patterns @(
    'TryGetReplayConfig',
    '\bHints\b',
    'toFailureReport',
    'Collect\.assertAll',
    'Diffs And Hints Work Together'
)

Require-FileContains -RelativePath "site-docs\power-showcase.md" -Patterns @(
    'equalBy',
    'equalByKey',
    'equalWith',
    'Check\.should',
    'Check\.shouldBy',
    'TryGetReplayConfig',
    'Testify\.configure'
)

$requirements = @(
    @{ Label = "Assert.result"; Pattern = '^M:Testify\.Assert\.result``1\('; Params = @('expectation', 'actual'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Assert.resultNamed"; Pattern = '^M:Testify\.Assert\.resultNamed``1\('; Params = @('test', 'expectation', 'actual'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Assert.resultAsync"; Pattern = '^M:Testify\.Assert\.resultAsync``1\('; Params = @('expectation', 'actual'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Assert.should"; Pattern = '^M:Testify\.Assert\.should``1\('; Params = @('expectation', 'actual'); Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Assert.shouldAsync"; Pattern = '^M:Testify\.Assert\.shouldAsync``1\('; Params = @('expectation', 'actual'); Returns = $true; Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Assert.assertPassed"; Pattern = '^M:Testify\.Assert\.assertPassed'; Params = @('result'); Exception = $true; SeeAlso = $true },
    @{ Label = "AssertExpectation.equalTo"; Pattern = '^M:Testify\.AssertExpectation\.equalTo``1\('; Params = @('expected'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "AssertExpectation.equalBy"; Pattern = '^M:Testify\.AssertExpectation\.equalBy``2\('; Params = @('projection', 'expected'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "AssertExpectation.equalByKey"; Pattern = '^M:Testify\.AssertExpectation\.equalByKey``2\('; Params = @('projection', 'expectedKey'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "AssertExpectation.equalWith"; Pattern = '^M:Testify\.AssertExpectation\.equalWith``1\('; Params = @('comparer', 'expected'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "AssertExpectation.throws"; Pattern = '^M:Testify\.AssertExpectation\.throws``2$'; Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "AssertOperators.(|>?)"; Pattern = '^M:Testify\.AssertOperators\.op_BarGreaterQmark'; Params = @('expr', 'expectation'); Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "AssertOperators.(>>?)"; Pattern = '^M:Testify\.AssertOperators\.op_GreaterGreaterQmark'; Params = @('expr', 'expectation'); Returns = $true; Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "AssertOperators.(=?)"; Pattern = '^M:Testify\.AssertOperators\.op_EqualsQmark'; Params = @('expr', 'value'); Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "AssertOperators.(||?)"; Pattern = '^M:Testify\.AssertOperators\.op_BarBarQmark'; Params = @('expr', 'expectations'); Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "AssertOperators.(&&?)"; Pattern = '^M:Testify\.AssertOperators\.op_AmpAmpQmark'; Params = @('expr', 'expectations'); Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Check.result"; Pattern = '^M:Testify\.Check\.result``3\('; Params = @('expectation', 'reference', 'actual', 'config', 'arbitrary'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Check.should"; Pattern = '^M:Testify\.Check\.should``3\('; Params = @('expectation', 'reference', 'actual', 'config', 'arbitrary'); Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Check.resultBy"; Pattern = '^M:Testify\.Check\.resultBy``3\('; Params = @('buildProperty', 'expectation', 'reference', 'actual', 'config'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Check.shouldBy"; Pattern = '^M:Testify\.Check\.shouldBy``3\('; Params = @('buildProperty', 'expectation', 'reference', 'actual', 'config'); Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "CheckExpectation.equalToReference"; Pattern = '^M:Testify\.CheckExpectation\.equalToReference``2$'; Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "CheckExpectation.equalBy"; Pattern = '^M:Testify\.CheckExpectation\.equalBy``3\('; Params = @('projection'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "CheckExpectation.equalByKey"; Pattern = '^M:Testify\.CheckExpectation\.equalByKey``3\('; Params = @('projection', 'expectedKey'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "CheckExpectation.equalWith"; Pattern = '^M:Testify\.CheckExpectation\.equalWith``2\('; Params = @('comparer'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "CheckExpectation.throwsSameExceptionType"; Pattern = '^M:Testify\.CheckExpectation\.throwsSameExceptionType'; Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "CheckOperators.(|=>)"; Pattern = '^M:Testify\.CheckOperators\.op_BarEqualsGreater'; Params = @('expr', 'reference'); Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "CheckOperators.(|=>>)"; Pattern = '^M:Testify\.CheckOperators\.op_BarEqualsGreaterGreater'; Params = @('expr', 'reference'); Returns = $true; Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "CheckOperators.(|?>)"; Pattern = '^M:Testify\.CheckOperators\.op_BarQmarkGreater'; Params = @('expr', 'buildProperty'); Exception = $true; Example = $true; SeeAlso = $true },
    @{ Label = "CheckConfig.defaultConfig"; Pattern = '^P:Testify\.CheckConfig\.defaultConfig$'; Example = $true },
    @{ Label = "CheckConfig.thorough"; Pattern = '^P:Testify\.CheckConfig\.thorough$'; Example = $true },
    @{ Label = "CheckConfig.withMaxTest"; Pattern = '^M:Testify\.CheckConfig\.withMaxTest'; Params = @('count'); Returns = $true; Example = $true },
    @{ Label = "CheckConfig.withReplayString"; Pattern = '^M:Testify\.CheckConfig\.withReplayString'; Params = @('text'); Returns = $true; Example = $true },
    @{ Label = "Generators.from"; Pattern = '^M:Testify\.Generators\.from``1$'; Returns = $true; Example = $true },
    @{ Label = "Generators.elements"; Pattern = '^M:Testify\.Generators\.elements'; Params = @('values'); Returns = $true; Example = $true },
    @{ Label = "Arbitraries.from"; Pattern = '^M:Testify\.Arbitraries\.from``1$'; Returns = $true; Example = $true },
    @{ Label = "Arbitraries.fromGen"; Pattern = '^M:Testify\.Arbitraries\.fromGen``1\('; Params = @('generator'); Returns = $true; Example = $true },
    @{ Label = "Arbitraries.tuple2"; Pattern = '^M:Testify\.Arbitraries\.tuple2'; Params = @('arbitrary1', 'arbitrary2'); Returns = $true; Example = $true },
    @{ Label = "Testify.configure"; Pattern = '^M:Testify\.Testify\.configure'; Params = @('config'); Example = $true; SeeAlso = $true },
    @{ Label = "Testify.currentConfiguration"; Pattern = '^M:Testify\.Testify\.currentConfiguration'; Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "Testify.resetConfiguration"; Pattern = '^M:Testify\.Testify\.resetConfiguration'; Example = $true; SeeAlso = $true },
    @{ Label = "TestifyConfig.withOutputFormat"; Pattern = '^M:Testify\.TestifyConfigModule\.withOutputFormat'; Params = @('outputFormat', 'config'); Returns = $true; Example = $true },
    @{ Label = "TestifyConfig.withHints"; Pattern = '^M:Testify\.TestifyConfigModule\.withHints'; Params = @('rules', 'config'); Returns = $true; Example = $true },
    @{ Label = "TestifyConfig.withHintPacks"; Pattern = '^M:Testify\.TestifyConfigModule\.withHintPacks'; Params = @('packs', 'config'); Returns = $true; Example = $true },
    @{ Label = "TestifyConfig.addCheckConfigTransformer"; Pattern = '^M:Testify\.TestifyConfigModule\.addCheckConfigTransformer'; Params = @('transformer', 'config'); Returns = $true; Example = $true },
    @{ Label = "TestifyHintRule.create"; Pattern = '^M:Testify\.TestifyHintRuleModule\.create'; Params = @('name', 'tryInfer'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "TestifyHintRule.onFieldRegexPattern"; Pattern = '^M:Testify\.TestifyHintRuleModule\.onFieldRegexPattern'; Params = @('name', 'field', 'pattern', 'buildHint'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "TestifyHintPack.create"; Pattern = '^M:Testify\.TestifyHintPackModule\.create'; Params = @('name', 'rules'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "HintInference.inferHints"; Pattern = '^M:Testify\.HintInference\.inferHints'; Params = @('report'); Returns = $true; Example = $true; SeeAlso = $true },
    @{ Label = "BuiltInHintPacks.beginner"; Pattern = '^P:Testify\.BuiltInHintPacks\.beginner$'; Example = $true },
    @{ Label = "Diff.defaultOptions"; Pattern = '^P:Testify\.Diff\.defaultOptions$'; Example = $true },
    @{ Label = "Diff.tryDescribe"; Pattern = '^M:Testify\.Diff\.tryDescribe'; Params = @('expected', 'actual'); Returns = $true; Example = $true }
)

foreach ($requirement in $requirements) {
    $member = Get-MemberXml -Label $requirement.Label -Pattern $requirement.Pattern
    if ($null -eq $member) {
        continue
    }

    Require-Tag -Member $member -Label $requirement.Label -TagName 'summary'

    if ($requirement.ContainsKey('Params')) {
        Require-Params -Member $member -Label $requirement.Label -Params $requirement.Params
    }

    if ($requirement.ContainsKey('Returns') -and $requirement.Returns) {
        Require-Tag -Member $member -Label $requirement.Label -TagName 'returns'
    }

    if ($requirement.ContainsKey('Exception') -and $requirement.Exception) {
        Require-Tag -Member $member -Label $requirement.Label -TagName 'exception'
    }

    if ($requirement.ContainsKey('Example') -and $requirement.Example) {
        Require-Tag -Member $member -Label $requirement.Label -TagName 'example'
    }

    if ($requirement.ContainsKey('SeeAlso') -and $requirement.SeeAlso) {
        Require-Tag -Member $member -Label $requirement.Label -TagName 'seealso'
    }
}

if ($missing.Count -gt 0) {
    $message = $missing -join [Environment]::NewLine
    throw "API docs coverage check failed:`n$message"
}

Write-Host "API docs coverage check passed."
