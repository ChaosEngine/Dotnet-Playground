# Test Performance Optimization Guide

## Problem Statement

Unit tests were running slower in VSCode (with Test Explorer) and CLI (`dotnet test`) compared to Visual Studio 2022/2026. This was caused by missing test runner configurations and parallelization settings.

## Solutions Implemented

### 1. xUnit Runner Configuration (`xunit.runner.json`)

Added `xunit.runner.json` to all test projects with the following optimizations:

- **`parallelizeAssembly: true`** - Enables parallel test execution within the assembly
- **`parallelizeTestCollections: true`** - Runs test collections in parallel
- **`maxParallelThreads: 0`** - Auto-detects and uses all available CPU cores
- **`preEnumerateTheories: true`** - Pre-enumerates theory tests for faster discovery
- **`shadowCopy: false`** - Disables shadow copying for faster test execution

These files are located in:
- `DotnetPlayground.Tests/xunit.runner.json` ✅ (main repo)
- `InkBall/test/InkBall.Tests/xunit.runner.json` ⚠️ (submodule - apply manually)
- `Caching-MySQL/test/Pomelo.Extensions.Caching.MySql.Tests/xunit.runner.json` ⚠️ (submodule - apply manually)

**Note**: The InkBall and Caching-MySQL projects are Git submodules. The xunit.runner.json configurations have been created for them, but need to be committed to their respective repositories separately.

### 2. Run Settings Configuration (`.runsettings`)

Created `.runsettings` file at the solution root with:

- **`MaxCpuCount: 0`** - Use all available processors for test execution
- **`TargetPlatform: x64`** - 64-bit execution for better performance
- **`DisableAppDomain: false`** - Optimized AppDomain handling
- **Parallel execution settings** for xUnit, MSTest, and NUnit

### 3. MSBuild Configuration (`Directory.Build.props`)

Added global MSBuild properties to optimize builds and test execution:

- **`BuildInParallel: true`** - Enable parallel project builds
- **`VSTestParallel: true`** - Enable parallel test execution in VSTest
- **`Deterministic: true`** - Deterministic builds for better caching
- Test-specific optimizations to reduce build times

### 4. VSCode Settings

Updated `.vscode/settings.json` to reference the `.runsettings` file:

```json
{
  "dotnet.unitTests.runSettingsPath": "${workspaceFolder}/.runsettings"
}
```

## How to Use

### Visual Studio Code

1. **Test Explorer**: Tests will automatically use the new configurations
2. **Command Line**: Run tests with the settings file:
   ```bash
   dotnet test --settings .runsettings
   ```

### Command Line (CLI)

The configurations are automatically picked up by the test runner. You can run:

```bash
# Standard test execution (uses xunit.runner.json automatically)
dotnet test

# With explicit settings file
dotnet test --settings .runsettings

# With parallel execution (now enabled by default)
dotnet test --parallel

# With specific number of processes
dotnet test -m:4
```

### Visual Studio 2022/2026

Visual Studio will automatically detect and use the `.runsettings` file if it's in the solution root. You can also manually configure it:

1. Go to **Test** → **Configure Run Settings** → **Select Solution Wide runsettings File**
2. Choose the `.runsettings` file from the solution root

## Performance Improvements

Expected improvements:

- **2-4x faster** test execution on multi-core machines
- **Reduced test discovery time** with pre-enumeration
- **Better resource utilization** with parallel execution
- **Consistent performance** across VSCode, CLI, and Visual Studio

## Configuration Details

### xUnit Parallelization

By default, xUnit runs:
- Test collections **sequentially** (one at a time)
- Tests within a collection **in parallel** (up to number of CPU cores)

With our configuration:
- Test collections run **in parallel**
- Tests within collections run **in parallel**
- All CPU cores are utilized

### CPU Core Detection

Setting `maxParallelThreads: 0` tells xUnit to auto-detect available cores:
- On a 4-core machine: Uses 4 threads
- On an 8-core machine: Uses 8 threads
- On a 16-core machine: Uses 16 threads

### When Tests Share State

If tests share state and cannot run in parallel, you can:

1. **Disable parallelization for specific collections**:
   ```csharp
   [Collection("Serial")]
   public class MyTests { }
   
   [CollectionDefinition("Serial", DisableParallelization = true)]
   public class SerialCollection { }
   ```

2. **Use collection fixtures** for shared context:
   ```csharp
   [Collection("Database")]
   public class MyTests : IClassFixture<DatabaseFixture> { }
   ```

## Troubleshooting

### Tests Fail When Run in Parallel

If tests fail due to shared state:
1. Identify the shared resource (database, file system, etc.)
2. Use collection fixtures to properly manage the shared state
3. Or disable parallelization for those specific test collections

### Performance Not Improved

Check:
1. Number of tests - Parallelization benefits are more noticeable with more tests
2. Test duration - Very fast tests may not benefit much from parallelization
3. I/O bound tests - Tests waiting on I/O may not benefit as much
4. CPU cores - Ensure your machine has multiple cores available

### VSCode Test Explorer Issues

If tests don't appear or run slowly:
1. Reload the window: **Ctrl+Shift+P** → "Developer: Reload Window"
2. Check that `.runsettings` path is correct in settings.json
3. Ensure test projects have the xunit.runner.json files

## References

- [xUnit Parallelization Documentation](https://xunit.net/docs/running-tests-in-parallel)
- [VSTest Documentation](https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file)
- [MSBuild Parallel Builds](https://docs.microsoft.com/en-us/visualstudio/msbuild/building-multiple-projects-in-parallel-with-msbuild)
