---
title: Basic Save
author: rowanmiller
ms.author: rowmil
manager: rowanmiller
ms.date: 10/27/2016
ms.topic: article
ms.assetid: 850d842e-3fad-4ef2-be17-053768e97b9e
ms.prod: entity-framework
uid: core/saving/basic
---
# Basic Save

> [!NOTE]
> This documentation is for EF Core. For EF6.x, see [Entity Framework 6](../../ef6/index.md).

Learn how to add, modify, and remove data using your context and entity classes.

> [!TIP]
> You can view this article's [sample](https://github.com/aspnet/EntityFramework.Docs/tree/master/samples/core/Saving/Saving/Basics/) on GitHub.

## Adding Data

Use the *DbSet.Add* method to add new instances of your entity classes. The data will be inserted in the database when you call *SaveChanges*.

<!-- [!code-csharp[Main](samples/core/Saving/Saving/Basics/Sample.cs)] -->
````csharp
        using (var db = new BloggingContext())
        {
            var blog = new Blog { Url = "http://sample.com" };
            db.Blogs.Add(blog);
            db.SaveChanges();

            Console.WriteLine(blog.BlogId + ": " +  blog.Url);
        }
````

## Updating Data

EF will automatically detect changes made to an existing entity that is tracked by the context. This includes entities that you load/query from the database, and entities that were previously added and saved to the database.

Simply modify the values assigned to properties and then call *SaveChanges*.

<!-- [!code-csharp[Main](samples/core/Saving/Saving/Basics/Sample.cs)] -->
````csharp
        using (var db = new BloggingContext())
        {
            var blog = db.Blogs.First();
            blog.Url = "http://sample.com/blog";
            db.SaveChanges();
        }
````

## Deleting Data

Use the *DbSet.Remove* method to delete instances of you entity classes.

If the entity already exists in the database, it will be deleted during *SaveChanges*. If the entity has not yet been saved to the database (i.e. it is tracked as added) then it will be removed from the context and will no longer be inserted when *SaveChanges* is called.

<!-- [!code-csharp[Main](samples/core/Saving/Saving/Basics/Sample.cs)] -->
````csharp
        using (var db = new BloggingContext())
        {
            var blog = db.Blogs.First();
            db.Blogs.Remove(blog);
            db.SaveChanges();
        }
````

## Multiple Operations in a single SaveChanges

You can combine multiple Add/Update/Remove operations into a single call to *SaveChanges*.

> [!NOTE]
> For most database providers, *SaveChanges* is transactional. This means  all the operations will either succeed or fail and the operations will never be left partially applied.

<!-- [!code-csharp[Main](samples/core/Saving/Saving/Basics/Sample.cs)] -->
````csharp
        using (var db = new BloggingContext())
        {
            db.Blogs.Add(new Blog { Url = "http://sample.com/blog_one" });
            db.Blogs.Add(new Blog { Url = "http://sample.com/blog_two" });

            var firstBlog = db.Blogs.First();
            firstBlog.Url = "";

            var lastBlog = db.Blogs.Last();
            db.Blogs.Remove(lastBlog);

            db.SaveChanges();
        }
````
