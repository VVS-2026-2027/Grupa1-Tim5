using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCULIS.Controllers;
using OCULIS.Models;

namespace OCULIS.Tests.Controllers;

[TestClass]
public class HomeControllerTests
{
    [TestMethod]
    public async Task Index_VracaBrojProizvodaPoslovnicaIIstaknuteAktivneAkcije()
    {
        await using var db = TestContextFactory.Create();
        db.Proizvod.AddRange(Enumerable.Range(1, 5).Select(i => Product($"Proizvod {i}")));
        db.Poslovnica.Add(new Poslovnica { Naziv = "Centar", Adresa = "Ulica 1", Telefon = "123", RadnoVrijeme = "09-17" });
        db.Akcija.AddRange(
            new Akcija { Naziv = "aktivna", Aktivna = true, PopustPostotak = 20, DatumPocetka = DateTime.Today.AddDays(-1), DatumZavrsetka = DateTime.Today.AddDays(1) },
            new Akcija { Naziv = "istekla", Aktivna = true, DatumPocetka = DateTime.Today.AddDays(-2), DatumZavrsetka = DateTime.Today.AddDays(-1) },
            new Akcija { Naziv = "isključena", Aktivna = false, DatumPocetka = DateTime.Today, DatumZavrsetka = DateTime.Today.AddDays(1) });
        await db.SaveChangesAsync();
        var controller = new HomeController(db, Clan4TestContext.CreateUserManager().Object);

        var result = (ViewResult)await controller.Index();

        Assert.AreEqual(5, controller.ViewBag.UkupnoProizvoda);
        Assert.AreEqual(1, controller.ViewBag.UkupnoPoslovnica);
        Assert.AreEqual(4, ((IEnumerable<Proizvod>)controller.ViewBag.IstaknutiProizvodi).Count());
        CollectionAssert.AreEqual(new[] { "aktivna" }, ((IEnumerable<Akcija>)controller.ViewBag.AktivneAkcije).Select(x => x.Naziv).ToArray());
        Assert.IsNull(result.Model);
    }

    [TestMethod]
    public async Task Dashboard_AgregiraStatistikuIPrihodUspjesnihPlacanja()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Id = "u1", UserName = "u1", Ime = "A", Prezime = "B" };
        db.Users.Add(user);
        db.Korpa.Add(new Korpa { Id = 1, IdKorisnik = user.Id, Korisnik = user });
        var order = new Narudzba
        {
            Id = 3, IdKorisnik = user.Id, Korisnik = user, IdKorpa = 1, AdresaIsporuke = "Ulica 1",
            UkupnaCijena = 42, DatumNarudzbe = DateTime.Today, Status = StatusNarudzbe.Zaprimljena
        };
        db.Narudzba.Add(order);
        db.Placanje.AddRange(
            new Placanje { IdNarudzba = order.Id, Narudzba = order, Iznos = 42, StatusPlacanja = StatusPlacanja.Uspjesno },
            new Placanje { IdNarudzba = order.Id, Narudzba = order, Iznos = 10, StatusPlacanja = StatusPlacanja.Odbijeno });
        await db.SaveChangesAsync();
        var users = Clan4TestContext.CreateUserManager();
        users.SetupGet(x => x.Users).Returns(db.Users);
        var controller = new HomeController(db, users.Object);

        var result = (ViewResult)await controller.Dashboard();

        var model = (OCULIS.Models.ViewModels.DashboardViewModel)result.Model!;
        Assert.AreEqual(1, model.UkupnoNarudzbi);
        Assert.AreEqual(1, model.UkupnoKorisnika);
        Assert.AreEqual(42, model.UkupniPrihod);
        Assert.AreEqual("A B", model.NedavneNarudzbe.Single().Korisnik);
        Assert.AreEqual(1, model.NarudzbePoStatusu["Zaprimljena"]);
    }

    [TestMethod]
    public void Privacy_VracaView()
    {
        var controller = new HomeController(TestContextFactory.Create(), Clan4TestContext.CreateUserManager().Object);
        Assert.IsInstanceOfType<ViewResult>(controller.Privacy());
    }

    [TestMethod]
    public void Error_KreiraModelSaTraceIdentifieromKadaNemaActivity()
    {
        var controller = Clan4TestContext.Controller(new HomeController(TestContextFactory.Create(), Clan4TestContext.CreateUserManager().Object));
        controller.HttpContext.TraceIdentifier = "trace-test";
        var model = (ErrorViewModel)((ViewResult)controller.Error()).Model!;
        Assert.AreEqual("trace-test", model.RequestId);
        Assert.IsTrue(model.ShowRequestId);
    }

    private static Proizvod Product(string name) => new()
    {
        Naziv = name, Opis = "opis", Cijena = 5, Kategorija = KategorijaProizvoda.DioptrijskeNaocale,
        Proizvodjac = "Proizvođač", DostupnaKolicina = 1
    };
}
