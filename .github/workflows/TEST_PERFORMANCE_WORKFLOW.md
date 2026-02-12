# Test Performance Comparison Workflow

## Overview

This GitHub Actions workflow automatically compares test execution performance between:
- **Baseline**: Tests run on the `dev` branch without `.runsettings` (sequential collection execution)
- **Improved**: Tests run on your branch with `.runsettings` (parallel collection execution)

## How to Use

### Automatic Triggering

The workflow runs automatically when you:
1. Open or update a Pull Request that modifies:
   - `.csproj` files
   - `.runsettings`
   - `xunit.runner.json`
   - `Directory.Build.props`
   - The workflow file itself

### Manual Triggering

You can manually run the comparison from the GitHub Actions tab:

1. Go to **Actions** → **Test Performance Comparison**
2. Click **Run workflow**
3. Configure options:
   - **Baseline branch**: Branch to compare against (default: `dev`)
   - **Test runs**: Number of runs to average (default: `3`)
4. Click **Run workflow**

## What It Does

### Step 1: Baseline Tests
- Checks out the baseline branch (e.g., `dev`)
- Builds the project
- Runs tests **without** `.runsettings` (3 times by default)
- Records execution times

### Step 2: Improved Tests
- Checks out your branch with the improvements
- Verifies `.runsettings` and `xunit.runner.json` exist
- Builds the project
- Runs tests **with** `.runsettings` (3 times by default)
- Records execution times

### Step 3: Compare Results
- Calculates average execution times
- Computes improvement percentage and speedup factor
- Generates a detailed comparison report
- Adds a comment to the PR (if triggered by PR)

## Example Output

The workflow produces a summary table like this:

```
📊 Test Performance Comparison Results

| Metric       | Baseline (dev) | Improved (PR) | Difference        |
|--------------|----------------|---------------|-------------------|
| Average Time | 45s            | 30s           | 33.33% faster     |
| Speedup      | 1.00x          | 1.50x         | -                 |

✅ Significant performance improvement detected!
```

## Understanding Results

### Significant Improvement (>10%)
✅ The parallel test configuration is working well. Your test suite benefits from parallelization.

### Modest Improvement (0-10%)
⚠️ Small improvements may indicate:
- Limited number of test collections (xUnit already parallelizes within collections)
- Fast-running tests where parallelization overhead exceeds benefits
- I/O-bound tests that don't benefit much from CPU parallelization

### No Improvement (0% or negative)
❌ No performance gains may indicate:
- Test suite has only one collection (xUnit already parallelizes those)
- Tests are too fast (parallelization overhead exceeds benefits)
- CI environment has limited CPU cores
- Tests have shared state preventing parallelization

## Interpreting CI Results

### Important Notes

1. **GitHub Actions runners** have 2 CPU cores by default
2. **Improvement scales with**:
   - Number of test collections
   - Number of tests per collection
   - Test execution time
   
3. **Best results** when:
   - Multiple test collections exist
   - Tests are CPU-bound
   - Tests take >1 second each

## Troubleshooting

### Workflow Fails on Baseline Branch

If the baseline branch (e.g., `dev`) doesn't have the test project configured:
- The baseline tests will fail
- This is expected if `dev` doesn't have tests yet
- You can change the baseline branch when triggering manually

### No Artifacts Generated

If tests fail, artifacts won't be uploaded. Check the test logs for errors.

### Comparison Shows No Improvement

This is normal if:
1. Your test suite only has one collection
2. Tests run very quickly (<1s total)
3. Tests are I/O bound

See [TEST_PERFORMANCE.md](../../TEST_PERFORMANCE.md) for optimization strategies.

## Configuration

### Change Number of Test Runs

When running manually, adjust the "Test runs" input to:
- `1`: Quick comparison (less accurate)
- `3`: Default (balanced)
- `5`: More accurate (takes longer)

### Change Baseline Branch

When running manually, set "Baseline branch" to:
- `dev`: Compare against development
- `master`: Compare against production
- Any other branch name

## Files Monitored

The workflow automatically runs when these files change:
- `**.csproj` - Project files
- `.runsettings` - VSTest settings
- `xunit.runner.json` - xUnit configuration
- `Directory.Build.props` - MSBuild properties
- `.github/workflows/test-performance-comparison.yml` - This workflow

## Local Testing

To replicate the comparison locally:

```bash
# Baseline (without .runsettings)
time dotnet test --no-build --configuration Debug

# Improved (with .runsettings)
time dotnet test --no-build --configuration Debug --settings .runsettings
```

Run each command 3 times and average the results for accuracy.

## Related Documentation

- [TEST_PERFORMANCE.md](../../TEST_PERFORMANCE.md) - Detailed performance guide
- [TEST_VERIFICATION.md](../../TEST_VERIFICATION.md) - Verification steps
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
