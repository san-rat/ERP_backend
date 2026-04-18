using Microsoft.EntityFrameworkCore;
using ProductService.Data;

namespace ProductService.Tests.Helpers;

/// <summary>
/// Creates a fresh, isolated in-memory <see cref="ProductDbContext"/> for every test.
/// Each call returns a context backed by a uniquely named database so that tests
/// never share state.
/// </summary>
internal static class DbContextFactory
{
    public static ProductDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new ProductDbContext(options);
    }
}
