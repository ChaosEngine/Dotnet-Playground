---
title: Ensure EF Core Will Work for Your Application
author: rowanmiller
ms.author: rowmil
manager: rowanmiller
ms.date: 10/27/2016
ms.topic: article
ms.assetid: d3b66f3c-9d10-4974-a090-8ad093c9a53d
ms.prod: entity-framework
uid: efcore-and-ef6/porting/ensure-requirements
---
# Ensure EF Core Will Work for Your Application

Before you start the porting process it is important to validate that EF Core meets the data access requirements for your application.

## Missing features

Make sure that EF Core has all the features you need to use in your application. See [Feature Comparison](../features.md) for a detailed comparison of how the feature set in EF Core compares to EF6.x. If any required features are missing, ensure that you can compensate for the lack of these features before porting to EF Core.

## Behavior changes

This is a non-exhaustive list of some changes in behavior between EF6.x and EF Core. It is important to keep these in mind as your port your application as they may change the way your application behaves, but will not show up as compilation errors after swapping to EF Core.

### DbSet.Add/Attach and graph behavior

In EF6.x, calling `DbSet.Add()` on an entity results in a recursive search for all entities referenced in its navigation properties. Any entities that are found, and are not already tracked by the context, are also be marked as added. `DbSet.Attach()` behaves the same, except all entities are marked as unchanged.

**EF Core performs a similar recursive search, but with some slightly different rules.**

*  The root entity is always in the requested state (added for `DbSet.Add` and unchanged for `DbSet.Attach`).

*  **For entities that are found during the recursive search of navigation properties:**

    *  **If the primary key of the entity is store generated**

        * If the primary key is not set to a value, the state is set to added. The primary key value is considered "not set" if it is assigned the CLR default value for the property type (i.e. `0` for `int`, `null` for `string`, etc.).

        * If the primary key is set to a value, the state is set to unchanged.

    *  If the primary key is not database generated, the entity is put in the same state as the root.

### Code First database initialization

**EF6.x has a significant amount of magic it performs around selecting the database connection and initializing the database. Some of these rules include:**

* If no configuration is performed, EF6.x will select a database on SQL Express or LocalDb.

* If a connection string with the same name as the context is in the applications `App/Web.config` file, this connection will be used.

* If the database does not exist, it is created.

* If none of the tables from the model exist in the database, the schema for the current model is added to the database. If migrations are enabled, then they are used to create the database.

* If the database exists and EF6.x had previously created the schema, then the schema is checked for compatibility with the current model. An exception is thrown if the model has changed since the schema was created.

**EF Core does not perform any of this magic.**

* The database connection must be explicitly configured in code.

* No initialization is performed. You must use `DbContext.Database.Migrate()` to apply migrations (or `DbContext.Database.EnsureCreated()` and `EnsureDeleted()` to create/delete the database without using migrations).

### Code First table naming convention

EF6.x runs the entity class name through a pluralization service to calculate the default table name that the entity is mapped to.

EF Core uses the name of the `DbSet` property that the entity is exposed in on the derived context. If the entity does not have a `DbSet` property, then the class name is used.
