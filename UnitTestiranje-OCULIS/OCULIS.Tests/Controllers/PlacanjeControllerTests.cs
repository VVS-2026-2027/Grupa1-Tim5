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
using OCULIS.Services.Placanje;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan2")]
public class PlacanjeControllerTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Plati_OdgovorAutorizacije_SpremaPlacanjeIPomjeraSamoUspjesnuNarudzbu(bool uspjesno)
    {
        await using var db = TestContextFactory.Create();
        var kupac = new Korisnik { Id = "kupac" };
        var narudzba = new Narudzba { Id = 10, Korisnik = kupac, UkupnaCijena = 60 };
        var pocetniStatus = narudzba.Status;
        db.Narudzba.Add(narudzba);
        await db.SaveChangesAsync();
        var status = uspjesno ? StatusPlacanja.Uspjesno : StatusPlacanja.Odbijeno;
        var strategija = new Mock<IPlacanjeStrategija>(MockBehavior.Strict);
        strategija.Setup(s => s.ObradiPlacanjeAsync(It.Is<Placanje>(p => p.Iznos == 60 && p.IdNarudzba == 10),
            "9999999991234")).ReturnsAsync(new RezultatPlacanja { Uspjesno = uspjesno, Status = status });
        var factory = new Mock<IPlacanjeServisFactory>(MockBehavior.Strict);
        factory.Setup(f => f.Kreiraj(NacinPlacanja.KreditnaKartica)).Returns(strategija.Object);
        var controller = Controller(db, kupac, factory.Object);
        var model = new PlacanjeViewModel
        {
            IdNarudzba = 10, Iznos = 1, NacinPlacanja = NacinPlacanja.KreditnaKartica, BrojKartice = "9999 9999 91234"
        };

        var rezultat = (RedirectToActionResult)await controller.Plati(model);

        Assert.AreEqual("Details", rezultat.ActionName);
        Assert.AreEqual(10, rezultat.RouteValues!["id"]);
        var placanje = await db.Placanje.SingleAsync();
        Assert.AreEqual(status, placanje.StatusPlacanja);
        Assert.AreEqual(60d, placanje.Iznos);
        Assert.AreEqual(uspjesno ? StatusNarudzbe.UObradi : pocetniStatus, narudzba.Status);
        strategija.VerifyAll();
        factory.VerifyAll();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    public async Task Plati_KarticaBezBroja_VracaValidacijskuGreskuBezNaplate(string? brojKartice)
    {
        await using var db = TestContextFactory.Create();
        var kupac = new Korisnik { Id = "kupac" };
        db.Narudzba.Add(new Narudzba { Id = 10, Korisnik = kupac, UkupnaCijena = 60 });
        await db.SaveChangesAsync();
        var factory = new Mock<IPlacanjeServisFactory>(MockBehavior.Strict);
        var controller = Controller(db, kupac, factory.Object);
        var model = new PlacanjeViewModel
        {
            IdNarudzba = 10, NacinPlacanja = NacinPlacanja.KreditnaKartica, BrojKartice = brojKartice
        };

        var rezultat = (ViewResult)await controller.Plati(model);

        Assert.AreSame(model, rezultat.Model);
        Assert.AreEqual(60d, model.Iznos);
        Assert.IsFalse(controller.ModelState.IsValid);
        Assert.IsFalse(await db.Placanje.AnyAsync());
        factory.VerifyNoOtherCalls();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Plati_FormaZaNarudzbu_ProvjeravaVlasnistvo(bool vlasnik)
    {
        await using var db = TestContextFactory.Create();
        var kupac = new Korisnik { Id = "kupac" };
        db.Narudzba.Add(new Narudzba
        {
            Id = 10, Korisnik = vlasnik ? kupac : new Korisnik { Id = "drugi" }, UkupnaCijena = 50
        });
        await db.SaveChangesAsync();
        var controller = Controller(db, kupac, Mock.Of<IPlacanjeServisFactory>());

        var rezultat = await controller.Plati(10);

        if (vlasnik)
        {
            var model = (PlacanjeViewModel)((ViewResult)rezultat).Model!;
            Assert.AreEqual(10, model.IdNarudzba);
            Assert.AreEqual(50d, model.Iznos);
        }
        else
        {
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }
    }

    [TestMethod]
    public async Task Plati_UspjesnoPlacenaNarudzba_PreusmjeravaBezPonovnogPlacanja()
    {
        await using var db = TestContextFactory.Create();
        var kupac = new Korisnik { Id = "kupac" };
        db.Narudzba.Add(new Narudzba { Id = 22, Korisnik = kupac, UkupnaCijena = 20 });
        db.Placanje.Add(new Placanje { IdNarudzba = 22, StatusPlacanja = StatusPlacanja.Uspjesno, Iznos = 20 });
        await db.SaveChangesAsync();
        var factory = new Mock<IPlacanjeServisFactory>(MockBehavior.Strict);
        var controller = Controller(db, kupac, factory.Object);

        var result = (RedirectToActionResult)await controller.Plati(22);

        Assert.AreEqual("Details", result.ActionName);
        Assert.AreEqual("Narudzba", result.ControllerName);
        Assert.AreEqual(22, result.RouteValues!["id"]);
        factory.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task PotvrdiGotovinu_PostojecaUplata_PotvrdiPlacanjeINarudzbu()
    {
        await using var db = TestContextFactory.Create();
        db.Narudzba.Add(new Narudzba { Id = 24, Status = StatusNarudzbe.Zaprimljena });
        db.Placanje.Add(new Placanje { Id = 6, IdNarudzba = 24, StatusPlacanja = StatusPlacanja.NaCekanju });
        await db.SaveChangesAsync();
        var controller = Controller(db, new Korisnik { Id = "staff" }, Mock.Of<IPlacanjeServisFactory>());

        var result = (RedirectToActionResult)await controller.PotvrdiGotovinu(6);

        Assert.AreEqual("Details", result.ActionName);
        Assert.AreEqual(StatusPlacanja.Uspjesno, (await db.Placanje.FindAsync(6))!.StatusPlacanja);
        Assert.AreEqual(StatusNarudzbe.UObradi, (await db.Narudzba.FindAsync(24))!.Status);
    }

    [TestMethod]
    public async Task PotvrdiGotovinu_NepostojecaUplata_NotFound()
    {
        await using var db = TestContextFactory.Create();
        var controller = Controller(db, new Korisnik { Id = "staff" }, Mock.Of<IPlacanjeServisFactory>());
        Assert.IsInstanceOfType<NotFoundResult>(await controller.PotvrdiGotovinu(999));
    }

    private static PlacanjeController Controller(ApplicationDbContext db, Korisnik kupac, IPlacanjeServisFactory factory)
    {
        var identity = new Mock<UserManager<Korisnik>>(Mock.Of<IUserStore<Korisnik>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        var context = new DefaultHttpContext();
        identity.Setup(u => u.GetUserAsync(context.User)).ReturnsAsync(kupac);
        return new PlacanjeController(db, identity.Object, factory)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
    }
}
