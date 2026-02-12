# Quick Reference: Test Performance Comparison Workflow

## 🚀 Quick Start

### Run the Comparison
```bash
# Option 1: Automatic (via PR)
# Just open a PR with changes to test config files

# Option 2: Manual
# Go to: Actions → Test Performance Comparison → Run workflow
```

## 📋 What Gets Compared

| Aspect | Baseline (dev) | Improved (PR) |
|--------|----------------|---------------|
| Configuration | No .runsettings | With .runsettings |
| Execution | Sequential collections | Parallel collections |
| Command | `dotnet test` | `dotnet test --settings .runsettings` |

## 📊 Reading Results

### Result Indicators

| Icon | Meaning | Performance |
|------|---------|-------------|
| ✅ | Significant | >10% improvement |
| ⚠️ | Modest | 0-10% improvement |
| ❌ | None | ≤0% improvement |

### Common Scenarios

**✅ 30%+ improvement**
- Multiple test collections
- CPU-bound tests
- Tests >1s each
- **Action**: Great! Keep the changes.

**⚠️ 5-10% improvement**
- Few test collections
- Mix of CPU/IO tests
- **Action**: Consider adding more test collections or optimizing test structure.

**❌ 0% improvement**
- Single test collection
- Very fast tests (<100ms each)
- IO-bound tests
- **Action**: Review [TEST_PERFORMANCE.md](../../TEST_PERFORMANCE.md) for optimization strategies.

## 🎯 Triggers

### Automatic Triggers
Workflow runs when PR modifies:
- `**.csproj`
- `.runsettings`
- `xunit.runner.json`
- `Directory.Build.props`

### Manual Trigger
1. Go to **Actions** tab
2. Select **Test Performance Comparison**
3. Click **Run workflow**
4. Configure:
   - Baseline branch (default: `dev`)
   - Number of runs (default: `3`)

## 📈 Interpreting Output

### Workflow Summary
- Detailed comparison table
- Performance metrics
- Configuration details
- Interpretation and recommendations

### PR Comment
- Quick comparison table
- Overall result indicator
- Link to full workflow run

### Artifacts
- `baseline-results.txt` - Baseline test times
- `improved-results.txt` - Improved test times

## ⚙️ Configuration Options

### Number of Test Runs
- **1 run**: Quick (~3 min total) - Less accurate
- **3 runs**: Default (~9 min total) - Balanced
- **5 runs**: Thorough (~15 min total) - Most accurate

### Baseline Branch
- **dev**: Development baseline (default)
- **master**: Production baseline
- **custom**: Any branch name

## 🔧 Troubleshooting

### No Performance Improvement?

Check:
1. ✅ Using `.runsettings`? (Required!)
2. ✅ Multiple test collections?
3. ✅ Tests take >1s each?
4. ✅ Tests are CPU-bound?

### Workflow Failed?

Common issues:
- Baseline branch doesn't have tests ➜ Change baseline branch
- Build errors ➜ Check build logs
- Test failures ➜ Fix failing tests first

## 📚 Documentation Links

- [Full Workflow Documentation](TEST_PERFORMANCE_WORKFLOW.md)
- [Visual Guide](WORKFLOW_VISUAL_GUIDE.md)
- [Performance Optimization Guide](../../TEST_PERFORMANCE.md)
- [Verification Steps](../../TEST_VERIFICATION.md)

## 💡 Pro Tips

1. **Run before merging**: Always check performance impact before merging test config changes
2. **Compare branches**: Use manual trigger to compare any two branches
3. **Track improvements**: Check workflow history to track performance over time
4. **CI environment**: Remember GitHub Actions uses 2 CPU cores - results scale on more powerful hardware

## 🎬 Example Results

```
📊 Test Performance Comparison Results

| Metric       | Baseline | Improved | Difference      |
|--------------|----------|----------|-----------------|
| Average Time | 45s      | 30s      | 33.33% faster   |
| Speedup      | 1.00x    | 1.50x    | -               |

✅ Significant performance improvement detected!
```

## 🔍 Local Testing

Want to test locally before running workflow?

```bash
# Baseline
time dotnet test --no-build

# Improved
time dotnet test --no-build --settings .runsettings

# Compare the times
```

## ⏱️ Typical Workflow Duration

- **Build & Setup**: ~3-4 minutes per job
- **Test Execution**: ~1-2 minutes per run (×3 = 3-6 minutes)
- **Comparison**: ~1 minute
- **Total**: ~8-12 minutes

---

**Need Help?** See [TEST_PERFORMANCE_WORKFLOW.md](TEST_PERFORMANCE_WORKFLOW.md) for complete documentation.
