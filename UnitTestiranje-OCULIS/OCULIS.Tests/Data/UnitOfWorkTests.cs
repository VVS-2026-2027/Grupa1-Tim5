using OCULIS.Data.Repositories;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class UnitOfWorkTests
{
    [TestMethod]
    public void Konstruktor_ValidanKontekst_KreiraRepozitorije()
    {
        // Arrange - priprema
        using var db = TestContextFactory.Create();

        // Act - kreiranje objekta
        using var jedinica = new UnitOfWork(db);

        // Assert - provjera rezultata
        Assert.IsNotNull(jedinica.Proizvodi);
        Assert.IsNotNull(jedinica.Korpe);
        Assert.IsNotNull(jedinica.StavkeKorpe);
        Assert.IsNotNull(jedinica.Narudzbe);
        Assert.IsNotNull(jedinica.Placanja);
        Assert.IsNotNull(jedinica.Poslovnice);
        Assert.IsNotNull(jedinica.Termini);
        Assert.IsNotNull(jedinica.Kartoni);
        Assert.IsNotNull(jedinica.Pregledi);
        Assert.IsNotNull(jedinica.Obavijesti);
        Assert.IsNotNull(jedinica.Reklamacije);
        Assert.IsNotNull(jedinica.Akcije);

        Assert.AreSame(db, jedinica.Context);
    }
}