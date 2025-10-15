# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one at a time in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade DotnetPlayground.Web\DotnetPlayground.Web.csproj
4. Upgrade InkBall\src\InkBall.Module\InkBall.Module.csproj
5. Upgrade IdentityManager2\src\IdentityManager2\IdentityManager2.csproj
6. Upgrade Caching-MySQL\src\Pomelo.Extensions.Caching.MySql\Pomelo.Extensions.Caching.MySql.csproj
7. Upgrade Caching-MySQL\src\Pomelo.Extensions.Caching.MySqlConfig.Tools\Pomelo.Extensions.Caching.MySqlConfig.Tools.csproj
8. Upgrade DotnetPlayground.Tests\DotnetPlayground.Tests.csproj
9. Upgrade InkBall\test\InkBall.Tests\InkBall.Tests.csproj
10. Upgrade Caching-MySQL\test\Pomelo.Extensions.Caching.MySql.Tests\Pomelo.Extensions.Caching.MySql.Tests.csproj
11. Run unit tests to validate upgrade in the projects listed below:
   - DotnetPlayground.Tests\DotnetPlayground.Tests.csproj
   - InkBall\test\InkBall.Tests\InkBall.Tests.csproj
   - Caching-MySQL\test\Pomelo.Extensions.Caching.MySql.Tests\Pomelo.Extensions.Caching.MySql.Tests.csproj

## Settings

### Excluded projects

| Project name | Description |
|:--------------------------|:---------------------------:|

### Aggregate NuGet packages modifications across all projects

| Package Name | Current Version | New Version | Description |
|:------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Authentication.Facebook | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.AspNetCore.Authentication.Google | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.AspNetCore.Authentication.Twitter | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.AspNetCore.Identity.UI | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.AspNetCore.SignalR.Protocols.MessagePack | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.EntityFrameworkCore | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.EntityFrameworkCore.Relational | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.EntityFrameworkCore.SqlServer | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.EntityFrameworkCore.Sqlite | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.EntityFrameworkCore.Tools | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.Caching.Abstractions | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.Caching.SqlServer | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.Configuration.Json | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.Configuration.UserSecrets | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.DependencyInjection | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.FileProviders.Embedded | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.Hosting.Abstractions | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.Identity.Stores | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.Extensions.Options | 9.0.10 | 10.0.0-rc.2.25502.107 | Recommended for .NET 10.0 |
| Microsoft.AspNetCore.Razor.Language | 6.0.36 | | Package functionality included with new framework reference |

### Project upgrade details

#### DotnetPlayground.Web\DotnetPlayground.Web.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Update all listed NuGet packages to recommended versions for .NET 10.0
  - Remove Microsoft.AspNetCore.Razor.Language (functionality included in framework)

Other changes:
  - Ensure NuGet.config source mappings are up to date

#### InkBall\src\InkBall.Module\InkBall.Module.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Update all listed NuGet packages to recommended versions for .NET 10.0

Other changes:
  - Ensure NuGet.config source mappings are up to date

#### IdentityManager2\src\IdentityManager2\IdentityManager2.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

Other changes:
  - Ensure NuGet.config source mappings are up to date

#### Caching-MySQL\src\Pomelo.Extensions.Caching.MySql\Pomelo.Extensions.Caching.MySql.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Update all listed NuGet packages to recommended versions for .NET 10.0

Other changes:
  - Ensure NuGet.config source mappings are up to date

#### Caching-MySQL\src\Pomelo.Extensions.Caching.MySqlConfig.Tools\Pomelo.Extensions.Caching.MySqlConfig.Tools.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

Other changes:
  - Ensure NuGet.config source mappings are up to date

#### DotnetPlayground.Tests\DotnetPlayground.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Update all listed NuGet packages to recommended versions for .NET 10.0

Other changes:
  - Ensure NuGet.config source mappings are up to date

#### InkBall\test\InkBall.Tests\InkBall.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Update all listed NuGet packages to recommended versions for .NET 10.0

Other changes:
  - Ensure NuGet.config source mappings are up to date

#### Caching-MySQL\test\Pomelo.Extensions.Caching.MySql.Tests\Pomelo.Extensions.Caching.MySql.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Update all listed NuGet packages to recommended versions for .NET 10.0

Other changes:
  - Ensure NuGet.config source mappings are up to date
