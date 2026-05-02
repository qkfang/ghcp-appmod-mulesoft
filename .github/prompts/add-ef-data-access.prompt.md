---
mode: agent
description: Add or extend EF Core data access in MovieFunctionApp.
---

Add Entity Framework Core data access to the function app.

Requirements:
- Use the existing `MovieDbContext` in `src/MovieFunctionApp/Data/`
- Target SQL Server via `Microsoft.EntityFrameworkCore.SqlServer`
- Read connection string from `IConfiguration` key `SqlConnectionString`
- Register `DbContext` in `Program.cs` using `AddDbContext` with scoped lifetime
- For new entities: create a class in `Models/`, add a `DbSet<T>` to `MovieDbContext`, and configure keys/indexes via `OnModelCreating`
- Use **async** EF methods (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`)
- Prefer **AsNoTracking** for read-only queries
- For Azure SQL, prefer Managed Identity auth (`Authentication=Active Directory Default`) over passwords

After changes, run `dotnet build` and report errors.
