using Microsoft.EntityFrameworkCore;
using OCULIS.Data.Repositories;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
public class RepozitorijTests
{
    [TestMethod]
    public async Task SaveChangesAsync_DvaRepozitorija_KomitujeObaEntiteta()
    {
        await using var db = TestContextFactory.Create();
        using var jedinica = new UnitOfWork(db);
        var proizvod = new Proizvod { Naziv = "Okvir" };
        var poslovnica = new Poslovnica { Naziv = "Centar" };
        await jedinica.Proizvodi.AddAsync(proizvod);
        await jedinica.Poslovnice.AddAsync(poslovnica);

        var brojPromjena = await jedinica.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.AreEqual(2, brojPromjena);
        Assert.AreEqual("Okvir", (await jedinica.Proizvodi.GetByIdAsync(proizvod.Id))?.Naziv);
        Assert.AreEqual("Centar", (await jedinica.Poslovnice.GetByIdAsync(poslovnica.Id))?.Naziv);
    }

    [TestMethod]
    public async Task SaveChangesAsync_IzmjenaIBrisanje_UpisujeIzmijenjeniZapis()
    {
        await using var db = TestContextFactory.Create();
        var repo = new Repository<Proizvod>(db);
        var okvir = new Proizvod { Naziv = "Okvir", Cijena = 50 };
        var staklo = new Proizvod { Naziv = "Staklo", Cijena = 20 };
        await repo.AddAsync(okvir);
        await repo.AddAsync(staklo);
        await repo.SaveChangesAsync();

        okvir.Cijena = 45;
        repo.Update(okvir);
        repo.Remove(staklo);
        await repo.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.IsNull(await repo.GetByIdAsync(staklo.Id));
        Assert.AreEqual(45d, (await repo.GetAllAsync()).Single().Cijena);
        Assert.AreEqual(okvir.Id, (await repo.FindAsync(p => p.Cijena >= 40)).Single().Id);
        Assert.AreEqual(1, await repo.Query().CountAsync());
    }
}
