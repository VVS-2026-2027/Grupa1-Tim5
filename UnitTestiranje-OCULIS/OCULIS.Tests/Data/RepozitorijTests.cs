using Microsoft.EntityFrameworkCore;
using OCULIS.Data.Repositories;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
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

    // kada trazimo proizvod koji ne postoji, repository vraca null, a ne baca exception
    [TestMethod]
    public async Task GetByIdAsync_NepostojeciId_VracaNull()
    {
        // Arrange - priprema
        await using var db = TestContextFactory.Create();
        var repo = new Repository<Proizvod>(db);

        // Act - poziv metode
        var rezultat = await repo.GetByIdAsync(999);

        // Assert - provjera rezultata
        Assert.IsNull(rezultat);
    }

    //prazna baza, repository vraca praznu listu, a ne null
    [TestMethod]
    public async Task GetAllAsync_PraznaBaza_VracaPraznuListu()
    {
        // Arrange - priprema
        await using var db = TestContextFactory.Create();
        var repo = new Repository<Proizvod>(db);
        // Act - poziv metode
        var rezultat = await repo.GetAllAsync();
        // Assert - provjera rezultata
        Assert.IsNotNull(rezultat);
        Assert.AreEqual(0, rezultat.Count());
    }

    //filtriranje kada nijedan proizvod ne zadovoljava uslov, repository vraca praznu listu, a ne null
    [TestMethod]
    public async Task FindAsync_NemaPodudaranja_VracaPraznuKolekciju()
    {
        // Arrange - priprema
        await using var db = TestContextFactory.Create();
        var repo = new Repository<Proizvod>(db);

        var proizvod = new Proizvod { Naziv = "Okvir", Cijena = 50 };
        await repo.AddAsync(proizvod);
        await repo.SaveChangesAsync();

        // Act - poziv metode
        var rezultat = await repo.FindAsync(p => p.Cijena > 100);

        // Assert - provjera rezultata
        Assert.AreEqual(0, rezultat.Count());
    }

    //dohvatanje korisnika po tekstualnom id-u, repository vraca odgovarajuci entitet
    [TestMethod]
    public async Task GetByIdAsync_TekstualniId_VracaKorisnika()
    {
        // Arrange - priprema
        await using var db = TestContextFactory.Create();
        var repo = new Repository<Korisnik>(db);

        var korisnik = new Korisnik
        {
            Id = "korisnik-1",
            UserName = "amila",
            Ime = "Amila",
            Prezime = "Test"
        };

        await repo.AddAsync(korisnik);
        await repo.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Act - poziv metode
        var rezultat = await repo.GetByIdAsync("korisnik-1");

        // Assert - provjera rezultata
        Assert.IsNotNull(rezultat);
        Assert.AreEqual("korisnik-1", rezultat.Id);
        Assert.AreEqual("amila", rezultat.UserName);
    }

    public static IEnumerable<object[]> FilterPodaciIzCsv()
    {
        var putanja = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "FilterProizvoda.csv");

        foreach (var red in File.ReadLines(putanja).Skip(1))
        {
            var polja = red.Split(',');

            yield return new object[]
            {
            int.Parse(polja[0]),
            int.Parse(polja[1])
            };
        }
    }

    [TestMethod]
    [DynamicData(nameof(FilterPodaciIzCsv), DynamicDataSourceType.Method)]
    public async Task FindAsync_CsvGranice_VracaOcekivaniBroj(
        int granica, int ocekivaniBroj)
    {
        // Arrange - priprema
        await using var db = TestContextFactory.Create();
        var repo = new Repository<Proizvod>(db);

        await repo.AddAsync(new Proizvod { Naziv = "Staklo", Cijena = 20 });
        await repo.AddAsync(new Proizvod { Naziv = "Okvir", Cijena = 50 });
        await repo.AddAsync(new Proizvod { Naziv = "Naocale", Cijena = 100 });
        await repo.SaveChangesAsync();

        // Act - poziv metode
        var rezultat = await repo.FindAsync(p => p.Cijena >= granica);

        // Assert - provjera rezultata
        Assert.AreEqual(ocekivaniBroj, rezultat.Count());
        Assert.IsTrue(rezultat.All(p => p.Cijena >= granica));
    }
}
