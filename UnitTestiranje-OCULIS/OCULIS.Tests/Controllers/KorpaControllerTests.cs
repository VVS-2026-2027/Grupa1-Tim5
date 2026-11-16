using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCULIS.Controllers;
using OCULIS.Data;
using OCULIS.Models;
using OCULIS.Models.ViewModels;
using OCULIS.Services.Popust;

namespace OCULIS.Tests;

[TestClass]
public class KorpaControllerTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    public async Task Dodaj_KolicinaIzvanZalihe_OstavljaKorpuPraznom(int kolicina)
    {
        await using var db = TestContextFactory.Create();
        db.Proizvod.Add(new Proizvod { Id = 1, Naziv = "Okvir", DostupnaKolicina = 4, Cijena = 20 });
        await db.SaveChangesAsync();
        var controller = Controller(db, new Korisnik { Id = "kupac" }, Mock.Of<IPopustKalkulatorService>());

        var rezultat = await controller.Dodaj(1, kolicina);

        Assert.AreEqual(nameof(KorpaController.Index), ((RedirectToActionResult)rezultat).ActionName);
        Assert.IsTrue(controller.TempData.ContainsKey("Error"));
        Assert.IsFalse(await db.StavkaKorpe.AnyAsync());
    }

    [TestMethod]
    public async Task Dodaj_PostojecaStavka_PovecavaKolicinuIAzuriraJedinicnuCijenu()
    {
        await using var db = TestContextFactory.Create();
        var kupac = new Korisnik { Id = "kupac" };
        var proizvod = new Proizvod { Id = 1, Naziv = "Okvir", DostupnaKolicina = 5, Cijena = 20 };
        var korpa = new Korpa { Id = 2, Korisnik = kupac };
        db.Add(new StavkaKorpe { Korpa = korpa, Proizvod = proizvod, Kolicina = 1, Cijena = 19 });
        await db.SaveChangesAsync();
        var controller = Controller(db, kupac, Mock.Of<IPopustKalkulatorService>());

        var rezultat = await controller.Dodaj(1, 2);

        Assert.AreEqual("Proizvod", ((RedirectToActionResult)rezultat).ControllerName);
        var stavka = await db.StavkaKorpe.SingleAsync();
        Assert.AreEqual(3, stavka.Kolicina);
        Assert.AreEqual(20d, stavka.Cijena);

    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Ukloni_VlasnistvoStavke_BriseSamoStavkuIzVlastiteKorpe(bool vlasnik)
    {
        await using var db = TestContextFactory.Create();
        var kupac = new Korisnik { Id = "kupac" };
        var drugi = new Korisnik { Id = "drugi" };
        var prva = new Korpa { Korisnik = kupac, UkupnaCijena = vlasnik ? 25 : 0 };
        var druga = new Korpa { Korisnik = drugi };
        var stavka = new StavkaKorpe
        {
            Id = 8, Korpa = vlasnik ? prva : druga, Proizvod = new Proizvod { Naziv = "Okvir" },
            Cijena = 25, Kolicina = 1
        };
        db.AddRange(prva, druga, stavka);
        await db.SaveChangesAsync();
        var controller = Controller(db, kupac, Mock.Of<IPopustKalkulatorService>());

        var rezultat = await controller.Ukloni(8);

        Assert.AreEqual(nameof(KorpaController.Index), ((RedirectToActionResult)rezultat).ActionName);
        Assert.AreEqual(!vlasnik, await db.StavkaKorpe.AnyAsync(s => s.Id == 8));
        Assert.AreEqual(0d, prva.UkupnaCijena);
    }

    [TestMethod]
    public async Task Checkout_PraznaKorpa_PreusmjeravaBezPozivaKalkulatora()
    {
        await using var db = TestContextFactory.Create();
        var kupac = new Korisnik { Id = "kupac" };
        var popust = new Mock<IPopustKalkulatorService>(MockBehavior.Strict);
        var controller = Controller(db, kupac, popust.Object);

        var rezultat = await controller.Checkout();

        Assert.AreEqual(nameof(KorpaController.Index), ((RedirectToActionResult)rezultat).ActionName);
        Assert.AreEqual(kupac.Id, (await db.Korpa.SingleAsync()).IdKorisnik);
        popust.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Checkout_PopunjenaKorpa_VracaStavkeIPopustZaVlasnika()
    {
        await using var db = TestContextFactory.Create();
        var kupac = new Korisnik { Id = "kupac" };
        var korpa = new Korpa { Id = 2, Korisnik = kupac };
        db.Add(new StavkaKorpe
        {
            Korpa = korpa, Proizvod = new Proizvod { Naziv = "Okvir" }, Kolicina = 2, Cijena = 20
        });
        await db.SaveChangesAsync();
        var cijene = new PopustRezultatViewModel { OsnovnaCijena = 40, UkupnaCijena = 36 };
        var popust = new Mock<IPopustKalkulatorService>(MockBehavior.Strict);
        popust.Setup(s => s.IzracunajPopustAsync(kupac.Id, 2)).ReturnsAsync(cijene);
        var controller = Controller(db, kupac, popust.Object);

        var rezultat = (ViewResult)await controller.Checkout();

        var model = (NarudzbaCheckoutViewModel)rezultat.Model!;
        Assert.AreEqual(2, model.KorpaId);
        Assert.AreSame(cijene, model.Popust);
        Assert.AreEqual(40d, model.Korpa.UkupnaCijena);
        Assert.AreEqual(1, model.Korpa.Stavke.Count);
        popust.Verify(s => s.IzracunajPopustAsync(kupac.Id, 2), Times.AtLeastOnce);
    }

    private static KorpaController Controller(ApplicationDbContext db, Korisnik korisnik, IPopustKalkulatorService popust)
    {
        var users = new Mock<UserManager<Korisnik>>(Mock.Of<IUserStore<Korisnik>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        var context = new DefaultHttpContext();
        users.Setup(u => u.GetUserAsync(context.User)).ReturnsAsync(korisnik);
        return new KorpaController(db, users.Object, popust)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
    }
}
