---
title: Porting a Code-Based Model (Code First & Code First to Existing Database)
author: rowanmiller
ms.author: rowmil
manager: rowanmiller
ms.date: 10/27/2016
ms.topic: article
ms.assetid: 2dce1a50-7d84-4856-abf6-2763dd9be99d
ms.prod: entity-framework
uid: efcore-and-ef6/porting/port-code
---
# Porting a Code-Based Model (Code First & Code First to Existing Database)

If you've read all the caveats and you are ready to port, then here are some guidelines to help you get started.

## Install EF Core NuGet packages

To use EF Core, you install the NuGet package for the database provider you want to use. For example, when targeting SQL Server, you would install `Microsoft.EntityFrameworkCore.SqlServer`. See [Database Providers](../../core/providers/index.md) for details.

If you are planning to use migrations, then you should also install the `Microsoft.EntityFrameworkCore.Tools` package.

It is fine to leave the EF6.x NuGet package (EntityFramework) installed, as EF Core and EF6.x can be used side-by-side in the same application. However, if you aren't intending to use EF6.x in any areas of your application, then uninstalling the package will help give compile errors on pieces of code that need attention.

## Swap namespaces

Most APIs that you use in EF6.x are in the `System.Data.Entity` namespace (and related sub-namespaces). The first code change is to swap to the `Microsoft.EntityFrameworkCore` namespace. You would typically start with your derived context code file and then work out from there, addressing compilation errors as they occur.

## Context configuration (connection etc.)

As described in [Ensure EF Core Will Work for Your Application](ensure-requirements.md), EF Core has less magic around detecting the database to connect to. You will need to override the `OnConfiguring` method on your derived context, and use the database provider specific API to setup the connection to the database.

Most EF6.x applications store the connection string in the applications `App/Web.config` file. In EF Core, you read this connection string using the `ConfigurationManager` API. You may need to add a reference to the `System.Configuration` framework assembly to be able to use this API.

<!-- literal_block"ids  "classes  "xml:space": "preserve", "backrefs  "linenos": true, "dupnames  : "csharp", highlight_args}, "names": [] -->
````csharp
public class BloggingContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlServer(ConfigurationManager.ConnectionStrings["BloggingDatabase"].ConnectionString);
    }
}
````

## Update your code

At this point, it's a matter of addressing compilation errors and reviewing code to see if the behavior changes will impact you.

## Existing migrations

There isn't really a feasible way to port existing EF6.x migrations to EF Core.

If possible, it is best to assume that all previous migrations from EF6.x have been applied to the database and then start migrating the schema from that point using EF Core. To do this, you would use the `Add-Migration` command to add a migration once the model is ported to EF Core. You would then remove all code from the `Up` and `Down` methods of the scaffolded migration. Subsequent migrations will compare to the model when that initial migration was scaffolded.

## Test the port

Just because your application compiles, does not mean it is successfully ported to EF Core. You will need to test all areas of your application to ensure that none of the behavior changes have adversely impacted your application.
