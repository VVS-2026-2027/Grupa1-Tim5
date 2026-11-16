using System.Globalization;
using Moq;
using OCULIS.Models;
using OCULIS.Services.Popust;

namespace OCULIS.Tests;

[TestClass]
public class PopustTests
{
    public static IEnumerable<object[]> Kolicine()
    {
        return File.ReadLines(Path.Combine(AppContext.BaseDirectory, "TestData", "kolicine.csv"))
            .Skip(1)
            .Select(line => line.Split(','))
            .Select(values => new object[]
            {
                int.Parse(values[0], CultureInfo.InvariantCulture),
                double.Parse(values[1], CultureInfo.InvariantCulture)
            });
    }

    [TestMethod]
    [DynamicData(nameof(Kolicine), DynamicDataSourceType.Method)]
    public void IzracunajPopustPostotak_GraniceKolicine_VracaOdgovarajuciPopust(int kolicina, double ocekivani)
    {
        var strategija = new KolicinaPopustStrategija();
        StavkaKorpe[] stavke = kolicina == 0 ? [] : [new() { Kolicina = kolicina }];

        var rezultat = strategija.IzracunajPopustPostotak(new Korisnik(), stavke, []);

        Assert.AreEqual(ocekivani, rezultat);
    }

    [TestMethod]
    [DataRow(1, 199, 0d)]
    [DataRow(2, 0, 5d)]
    [DataRow(4, 200, 10d)]
    [DataRow(5, 0, 10d)]
    [DataRow(9, 500, 15d)]
    [DataRow(10, 0, 15d)]
    public void IzracunajPopustPostotak_PragoviLojalnosti_VracaOdgovarajuciPopust(int narudzbe, int bodovi, double ocekivani)
    {
        var strategija = new LojalnostPopustStrategija();
        var korisnik = new Korisnik { BrojNarudzbi = narudzbe, LojalnostBodovi = bodovi };

        var rezultat = strategija.IzracunajPopustPostotak(korisnik, [], []);

        Assert.AreEqual(ocekivani, rezultat);
    }

    [TestMethod]
    [DataRow(false, 0d)]
    [DataRow(true, 15d)]
    public void IzracunajPopustPostotak_AktivneIstekleITudjeAkcije_BiraNajvecuPrimjenjivu(bool aktivna, double ocekivani)
    {
        var sada = DateTime.Now;
        var strategija = new AkcijaPopustStrategija();
        Akcija[] akcije =
        [
            new() { Aktivna = aktivna, DatumPocetka = sada.AddDays(-1), DatumZavrsetka = sada.AddDays(1), PopustPostotak = 10 },
            new() { Aktivna = aktivna, DatumPocetka = sada.AddDays(-1), DatumZavrsetka = sada.AddDays(1), PopustPostotak = 15, IdProizvod = 1 },
            new() { Aktivna = aktivna, DatumPocetka = sada.AddDays(-1), DatumZavrsetka = sada.AddDays(1), PopustPostotak = 70, IdProizvod = 2 },
            new() { Aktivna = true, DatumPocetka = sada.AddDays(-2), DatumZavrsetka = sada.AddDays(-1), PopustPostotak = 90 }
        ];

        var rezultat = strategija.IzracunajPopustPostotak(new Korisnik(), [new() { IdProizvod = 1 }], akcije);

        Assert.AreEqual(ocekivani, rezultat);
    }

    [TestMethod]
    public async Task IzracunajPopustAsync_PraznaKorpa_IzvrsavaStrategijeIVracaNulu()
    {
        await using var db = TestContextFactory.Create();
        var korisnik = new Korisnik { Id = "kupac" };
        db.Users.Add(korisnik);
        await db.SaveChangesAsync();
        var strategija = new Mock<IPopustStrategija>(MockBehavior.Strict);
        strategija.Setup(s => s.IzracunajPopustPostotak(korisnik,
            It.Is<IEnumerable<StavkaKorpe>>(stavke => !stavke.Any()),
            It.Is<IEnumerable<Akcija>>(akcije => !akcije.Any()))).Returns(0);
        var servis = new PopustKalkulatorService(db, [strategija.Object]);

        var rezultat = await servis.IzracunajPopustAsync("kupac", 3);

        Assert.AreEqual(0d, rezultat.UkupnaCijena);
        Assert.AreEqual(0, rezultat.PrimijenjeniPopusti.Count);
        strategija.VerifyAll();
    }

    [TestMethod]
    public async Task IzracunajPopustAsync_NepostojeciKorisnik_BacaIzuzetak()
    {
        await using var db = TestContextFactory.Create();
        var servis = new PopustKalkulatorService(db, []);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => servis.IzracunajPopustAsync("nema", 1));
    }

    [TestMethod]
    public async Task IzracunajPopustAsync_VisePrimjenjivihPopusta_BiraNajveciIZaokruzujeIznos()
    {
        await using var db = TestContextFactory.Create();
        var kupac = new Korisnik { Id = "kupac", BrojNarudzbi = 10 };
        var korpa = new Korpa { Id = 3, Korisnik = kupac };
        var drugaKorpa = new Korpa { Id = 4, Korisnik = kupac };
        var proizvod = new Proizvod { Id = 8, Naziv = "Okvir" };
        db.StavkaKorpe.AddRange(
            new StavkaKorpe { Korpa = korpa, Proizvod = proizvod, Kolicina = 3, Cijena = 19.99 },
            new StavkaKorpe { Korpa = drugaKorpa, Proizvod = proizvod, Kolicina = 4, Cijena = 100 });
        var sada = DateTime.Now;
        db.Akcija.Add(new Akcija
        {
            Id = 12, Naziv = "Popust za okvir", Proizvod = proizvod, PopustPostotak = 12,
            DatumPocetka = sada.AddDays(-1), DatumZavrsetka = sada.AddDays(1)
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var servis = new PopustKalkulatorService(db,
            [new KolicinaPopustStrategija(), new AkcijaPopustStrategija(), new LojalnostPopustStrategija()]);

        var rezultat = await servis.IzracunajPopustAsync(kupac.Id, 3);

        Assert.AreEqual(59.97d, rezultat.OsnovnaCijena, 0.00001);
        Assert.AreEqual(15d, rezultat.PopustPostotak);
        Assert.AreEqual(9d, rezultat.PopustIznos);
        Assert.AreEqual(50.97d, rezultat.UkupnaCijena);
        CollectionAssert.AreEqual(new[] { "Količinski popust: 7%", "Akcijski popust: 12%", "Lojalnost korisnika: 15%" },
            rezultat.PrimijenjeniPopusti.ToArray());
    }

    [TestMethod]
    public void IzracunajPopustPostotak_AktivnaAkcijaZaDrugiProizvod_VracaNulu()
    {
        var sada = DateTime.Now;
        var akcija = new Akcija { IdProizvod = 2, PopustPostotak = 20,
            DatumPocetka = sada.AddDays(-1), DatumZavrsetka = sada.AddDays(1) };

        var rezultat = new AkcijaPopustStrategija().IzracunajPopustPostotak(
            new Korisnik(), [new StavkaKorpe { IdProizvod = 1 }], [akcija]);

        Assert.AreEqual(0d, rezultat);
    }
}
