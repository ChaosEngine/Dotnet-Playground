# Test Performance Comparison Workflow - Visual Guide

## Workflow Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     WORKFLOW TRIGGER                             │
│  • Pull Request to master/dev                                   │
│  • Manual workflow dispatch                                     │
│  • Changes to: .csproj, .runsettings, xunit.runner.json, etc.  │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
              ┌────────────────────────────────┐
              │   Parallel Execution (Jobs)    │
              └────────────────────────────────┘
                               │
                ┌──────────────┴──────────────┐
                │                             │
                ▼                             ▼
┌───────────────────────────┐   ┌───────────────────────────┐
│  Job 1: BASELINE TESTS    │   │  Job 2: IMPROVED TESTS    │
│  Branch: dev              │   │  Branch: PR branch        │
└───────────────────────────┘   └───────────────────────────┘
│                             │   │                             │
│ 1. Checkout dev branch      │   │ 1. Checkout PR branch       │
│ 2. Setup .NET 9.0           │   │ 2. Setup .NET 9.0           │
│ 3. Restore dependencies     │   │ 3. Restore dependencies     │
│ 4. Build (Debug)            │   │ 4. Build (Debug)            │
│ 5. Run tests 3x             │   │ 5. Verify .runsettings      │
│    WITHOUT .runsettings     │   │ 6. Run tests 3x             │
│    (sequential collections) │   │    WITH .runsettings        │
│                             │   │    (parallel collections)   │
│ 6. Calculate avg time       │   │ 7. Calculate avg time       │
│ 7. Save to artifact         │   │ 8. Save to artifact         │
└─────────────┬───────────────┘   └─────────────┬───────────────┘
              │                                 │
              └────────────┬────────────────────┘
                           ▼
        ┌──────────────────────────────────────────┐
        │    Job 3: COMPARE RESULTS                │
        │    Depends on: baseline-tests,           │
        │                improved-tests             │
        └──────────────────────────────────────────┘
                           │
        1. Download artifacts from both jobs
        2. Calculate improvement %
        3. Calculate speedup factor
        4. Generate comparison table
        5. Create workflow summary
        6. Comment on PR (if applicable)
                           │
                           ▼
        ┌──────────────────────────────────────────┐
        │         OUTPUT FORMATS                   │
        ├──────────────────────────────────────────┤
        │  📊 Workflow Summary (GitHub UI)         │
        │  💬 PR Comment (if PR trigger)           │
        │  📁 Artifacts (baseline/improved results)│
        └──────────────────────────────────────────┘
```

## Example Workflow Execution

### Scenario: Pull Request with .runsettings optimization

```
Timeline:
─────────────────────────────────────────────────────────────

⏰ 0:00 - Workflow triggered by PR
          Event: Pull request opened/updated
          Files changed: .runsettings, Directory.Build.props

⏰ 0:01 - Jobs start in parallel
          
          [Job 1: Baseline]        [Job 2: Improved]
          └─ Checkout dev          └─ Checkout PR branch
          └─ Setup .NET            └─ Setup .NET
          └─ Restore (2 min)       └─ Restore (2 min)
          └─ Build (1 min)         └─ Build (1 min)
          
⏰ 0:04 - Test execution begins

          [Job 1: Baseline]        [Job 2: Improved]
          Run 1: 45s               Run 1: 30s
          Run 2: 44s               Run 2: 29s  
          Run 3: 46s               Run 3: 31s
          ─────────                ─────────
          Avg: 45s                 Avg: 30s
          
⏰ 0:07 - Jobs complete, results saved

⏰ 0:08 - Comparison job starts
          └─ Download artifacts
          └─ Calculate metrics
             • Improvement: 33.33%
             • Speedup: 1.50x
          └─ Generate report
          └─ Post PR comment
          
⏰ 0:09 - Workflow complete ✅

Total Duration: ~9 minutes
```

## Example Output

### Workflow Summary View

```
📊 Test Performance Comparison Results

## Baseline (dev branch - without .runsettings)
  Branch: dev
  Total time: 135s
  Average time: 45s
  Runs: 3

## Improved (with .runsettings)
  Branch: copilot/improve-test-execution-speed
  Total time: 90s
  Average time: 30s
  Runs: 3

## Performance Comparison

| Metric       | Baseline | Improved | Difference    |
|--------------|----------|----------|---------------|
| Average Time | 45s      | 30s      | 33.33% faster |
| Total Time   | 135s     | 90s      | 45s saved     |
| Speedup      | 1.00x    | 1.50x    | -             |

### ✅ Result: Significant Performance Improvement

The new test configuration shows a **33.33%** improvement (1.50x speedup).

## Configuration Details

- **Baseline**: Tests run without `.runsettings` file (sequential collection execution)
- **Improved**: Tests run with `.runsettings` file (parallel collection execution)
- **Test Runs**: 3 runs averaged for each configuration
- **CI Environment**: `ubuntu-latest` (GitHub Actions)
```

### PR Comment View

```
📊 Test Performance Comparison Results

| Metric       | Baseline (dev) | Improved (PR) | Difference      |
|--------------|----------------|---------------|-----------------|
| Average Time | 45s            | 30s           | **33.33% faster** |
| Speedup      | 1.00x          | **1.50x**     | -               |

✅ Significant performance improvement detected!

See the [workflow run](https://github.com/ChaosEngine/Dotnet-Playground/actions/runs/12345) for detailed results.
```

## Manual Trigger Options

When manually triggering the workflow from the Actions tab:

```
┌─────────────────────────────────────────┐
│  Run workflow                           │
├─────────────────────────────────────────┤
│  Branch: copilot/improve-test-execution │
│                                         │
│  Baseline branch to compare against:   │
│  [dev              ▼]                  │
│                                         │
│  Number of test runs to average:       │
│  [3                ▼]                  │
│                                         │
│  [Run workflow]                         │
└─────────────────────────────────────────┘

Options explained:
• Baseline branch: Which branch to compare against (dev, master, etc.)
• Test runs: How many times to run tests (more = more accurate, longer time)
```

## CPU Core Utilization

```
Without .runsettings (Baseline):
┌────────────────────────────────┐
│ Test Collection 1              │
│ ├─ Test A1 │ Test A2 │ (CPU 1)│  Collection runs sequentially
│ └─ Test A3 │ Test A4 │ (CPU 2)│  Tests within collection parallel
└────────────────────────────────┘
│ Test Collection 2              │  ← Waits for Collection 1
│ ├─ Test B1 │ Test B2 │ (CPU 1)│
│ └─ Test B3 │ Test B4 │ (CPU 2)│
└────────────────────────────────┘

With .runsettings (Improved):
┌────────────────────────────────┐
│ Test Collection 1              │
│ ├─ Test A1 │ Test A2 │ (CPU 1)│  ← Both collections run
│ └─ Test A3 │ Test A4 │        │     simultaneously
├────────────────────────────────┤
│ Test Collection 2              │  ← Parallel with Collection 1
│ ├─ Test B1 │ Test B2 │ (CPU 2)│
│ └─ Test B3 │ Test B4 │        │
└────────────────────────────────┘

Result: 1.5-3x faster with multiple collections
```

## Workflow Files Structure

```
.github/
├── workflows/
│   ├── test-performance-comparison.yml  ← Main workflow
│   ├── TEST_PERFORMANCE_WORKFLOW.md     ← Usage documentation
│   ├── dotnetcore.yml                   ← Standard CI/CD
│   └── ...other workflows...
│
├── (project files)
├── .runsettings                         ← Test settings
├── Directory.Build.props                 ← MSBuild properties
├── DotnetPlayground.Tests/
│   └── xunit.runner.json                ← xUnit configuration
│
└── Documentation
    ├── TEST_PERFORMANCE.md              ← Performance guide
    └── TEST_VERIFICATION.md             ← Verification steps
```

## Key Benefits

✅ **Automated**: Runs on every PR affecting test configuration  
✅ **Reproducible**: Same environment (ubuntu-latest) every time  
✅ **Statistical**: Multiple runs averaged for accuracy  
✅ **Visual**: Clear comparison tables and summaries  
✅ **Documented**: Comprehensive explanations of results  
✅ **Flexible**: Manual trigger with custom parameters  
✅ **Transparent**: Full logs and artifacts available  

## Next Steps

1. **Open a PR** with test configuration changes → workflow runs automatically
2. **Or trigger manually** from Actions tab → customize baseline and runs
3. **Review results** in workflow summary and PR comment
4. **Iterate** based on feedback and performance data
