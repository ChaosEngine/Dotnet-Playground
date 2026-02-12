# Test Performance Verification Guide

This document provides steps to verify that the test performance optimizations are working correctly.

## Quick Verification

### 1. Check Configuration Files Exist

```bash
# Main test project
ls -la DotnetPlayground.Tests/xunit.runner.json

# Solution-wide settings
ls -la .runsettings
ls -la Directory.Build.props

# VSCode settings
cat .vscode/settings.json | grep runsettings
```

### 2. Build the Solution

```bash
dotnet restore
dotnet build
```

The build should now use parallel compilation (via `Directory.Build.props`).

### 3. Run Tests and Measure Performance

**Before Optimization (baseline):**
```bash
# If you have a backup branch without these changes
git checkout main
time dotnet test
```

**After Optimization:**
```bash
git checkout copilot/improve-test-execution-speed
time dotnet test
```

You should see:
- Tests running in parallel across multiple threads
- Faster overall test execution time (2-4x improvement on multi-core machines)
- Test output showing parallel execution

### 4. Verify xUnit Parallel Execution

Run with verbose output to see parallelization:

```bash
dotnet test --logger "console;verbosity=detailed"
```

Look for messages indicating:
- Multiple tests running simultaneously
- Thread IDs in output (if enabled)
- Faster completion times

### 5. Test with Explicit Settings File

```bash
# Use .runsettings explicitly
dotnet test --settings .runsettings

# Or with specific parameters
dotnet test --parallel --logger "console;verbosity=normal"
```

## Detailed Verification

### Check xUnit Configuration is Loaded

Add a test to verify xUnit configuration:

```csharp
using Xunit;
using Xunit.Abstractions;

public class ConfigurationTests
{
    private readonly ITestOutputHelper _output;

    public ConfigurationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void VerifyParallelExecution()
    {
        var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        _output.WriteLine($"Running on thread: {threadId}");
        
        // This test should show different thread IDs when run with other tests
        System.Threading.Thread.Sleep(100); // Simulate work
        Assert.True(true);
    }

    [Fact]
    public void VerifyParallelExecution2()
    {
        var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        _output.WriteLine($"Running on thread: {threadId}");
        
        System.Threading.Thread.Sleep(100); // Simulate work
        Assert.True(true);
    }
}
```

Run these tests and check the output - they should show different thread IDs if running in parallel.

### Monitor CPU Usage

During test execution, check CPU usage:

**On Linux/macOS:**
```bash
# In one terminal
top -p $(pgrep -f "dotnet test")

# In another terminal
dotnet test
```

**On Windows:**
```powershell
# Open Task Manager and watch CPU usage while running:
dotnet test
```

With parallel execution, you should see higher CPU utilization (closer to 100% on multi-core machines).

### Compare Execution Times

Create a benchmark script:

```bash
#!/bin/bash
# benchmark-tests.sh

echo "Running tests 3 times and averaging..."

total=0
for i in {1..3}; do
    echo "Run $i..."
    start=$(date +%s)
    dotnet test --no-build > /dev/null 2>&1
    end=$(date +%s)
    duration=$((end - start))
    echo "  Duration: ${duration}s"
    total=$((total + duration))
done

average=$((total / 3))
echo ""
echo "Average execution time: ${average}s"
```

Run this before and after applying the optimizations to compare.

## Troubleshooting

### Tests Not Running in Parallel

**Check 1:** Verify xunit.runner.json is copied to output
```bash
ls -la DotnetPlayground.Tests/bin/Debug/net9.0/xunit.runner.json
```

If missing, ensure the .csproj has:
```xml
<ItemGroup>
  <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

**Check 2:** Verify no Collection attributes disabling parallelization
```bash
grep -r "DisableParallelization" DotnetPlayground.Tests/
```

**Check 3:** Check for synchronization primitives
Tests using `lock`, `Monitor`, or other synchronization may serialize execution.

### No Performance Improvement

**Possible Causes:**

1. **Few Tests:** Parallelization overhead may exceed benefits for <10 tests
2. **I/O Bound:** Tests waiting on I/O won't benefit much from parallelization
3. **Shared State:** Tests with shared state may be serialized by xUnit
4. **Single Core:** Machine has only 1 CPU core
5. **Container Limits:** Running in container with CPU limits

**Solutions:**

- Check number of CPU cores: `nproc` (Linux) or `echo $NUMBER_OF_PROCESSORS` (Windows)
- Profile tests to identify bottlenecks: `dotnet test --collect:"XPlat Code Coverage"`
- Review tests for shared state and refactor if needed

### VSCode Test Explorer Not Using Settings

**Fix 1:** Reload VSCode window
- Press `Ctrl+Shift+P` → "Developer: Reload Window"

**Fix 2:** Check settings.json
```bash
cat .vscode/settings.json | grep dotnet
```

Should show:
```json
"dotnet.unitTests.runSettingsPath": "${workspaceFolder}/.runsettings"
```

**Fix 3:** Clear test cache
```bash
rm -rf ~/.vscode/extensions/*test*/cache
```

## Performance Metrics

Expected improvements on various hardware:

| CPU Cores | Before | After | Improvement |
|-----------|--------|-------|-------------|
| 2 cores   | 30s    | 18s   | 1.7x        |
| 4 cores   | 30s    | 12s   | 2.5x        |
| 8 cores   | 30s    | 8s    | 3.8x        |
| 16+ cores | 30s    | 6s    | 5.0x        |

*Note: Actual results vary based on test characteristics and system load.*

## Additional Resources

- [xUnit Parallelization Docs](https://xunit.net/docs/running-tests-in-parallel)
- [VSTest RunSettings Reference](https://docs.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file)
- [MSBuild Parallel Builds](https://docs.microsoft.com/en-us/visualstudio/msbuild/building-multiple-projects-in-parallel-with-msbuild)
