using Microsoft.EntityFrameworkCore;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class ApplicationDbContextTests
{
    [TestMethod]
    public void OnModelCreating_StavkeNarudzbe_KonfiguriseKaskadnoBrisanje()
    {
        // Arrange - priprema
        using var db = TestContextFactory.Create();

        // Act - čitanje konfiguracije veze
        var tipStavke = db.Model.FindEntityType(typeof(StavkaNarudzbe));

        var veza = tipStavke?.FindNavigation(
            nameof(StavkaNarudzbe.Narudzba));

        // Assert - provjera konfiguracije
        Assert.IsNotNull(veza);
        Assert.AreEqual(
            DeleteBehavior.Cascade,
            veza.ForeignKey.DeleteBehavior);
    }
}