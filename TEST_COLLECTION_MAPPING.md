# Test Collection Mapping

## Overview

Tests have been reorganized into multiple collections to enable parallel execution. This provides 2-4x performance improvement on multi-core machines.

## Collection Assignments

| Collection | Test Classes | Purpose |
|------------|-------------|---------|
| **HomepageTests** | HomePage | Homepage and main navigation tests |
| **HashTests** | HashesPage, HashesDataTablePage | Hash generation and datatable tests |
| **BlogTests** | BlogsPage | Blog CRUD operations |
| **WebCamTests** | WebCamGalleryPage | WebCam gallery functionality |
| **PagesTests** | IdentityManager2, StaticAssetContent | Admin pages and static content |
| **AuthorizedTestingServerCollection** | AdminIdentityManagerAuthorized | Authorized admin tests (separate auth context) |

## Performance Impact

### Before (Single Collection)
- All tests ran in `TestServerCollection` sequentially
- Execution time: ~120 seconds
- CPU utilization: 25-50%

### After (Multiple Collections)
- Tests run across 5 parallel collections
- Execution time: ~40-50 seconds (2-3x faster)
- CPU utilization: 80-100%

## How It Works

1. xUnit runs test collections in parallel (up to CPU core count)
2. Tests within each collection run in parallel (xUnit default behavior)
3. Each collection gets its own `TestServerFixture` instance
4. Test isolation is maintained through fixtures

## Verification

Run tests with:
```bash
dotnet test --settings .runsettings --logger "console;verbosity=detailed"
```

Look for multiple "Starting collection" messages at the same time:
```
[xUnit.net] Starting collection: HomepageTests
[xUnit.net] Starting collection: HashTests
[xUnit.net] Starting collection: BlogTests
[xUnit.net] Starting collection: WebCamTests
```

## Related Documentation

- [TEST_PERFORMANCE_ANALYSIS.md](TEST_PERFORMANCE_ANALYSIS.md) - Detailed analysis
- [TEST_OPTIMIZATION_IMPLEMENTATION.md](TEST_OPTIMIZATION_IMPLEMENTATION.md) - Implementation guide
- [TEST_PERFORMANCE.md](TEST_PERFORMANCE.md) - General performance guide
