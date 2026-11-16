using Microsoft.EntityFrameworkCore;
using OCULIS.Data;

namespace OCULIS.Tests;

internal static class TestContextFactory
{
    internal static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }
}
