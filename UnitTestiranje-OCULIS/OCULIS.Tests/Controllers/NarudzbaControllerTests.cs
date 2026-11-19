using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using OCULIS.Constants;
using OCULIS.Controllers;
using OCULIS.Data;
using OCULIS.Models;
using OCULIS.Models.ViewModels;
using OCULIS.Services.Dostava;
using OCULIS.Services.Obavijest;
using OCULIS.Services.Popust;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan2")]
public class NarudzbaControllerTests
{
    [TestMethod]
    public async Task Details_Vlasnik_VracaNarudzbu()
    {
        await using var db = TestContextFactory.Create();
        var owner = new Korisnik { Id = "owner" };
        db.Narudzba.Add(new Narudzba { Id = 3, IdKorisnik = owner.Id, Korisnik = owner });
        await db.SaveChangesAsync();
        var controller = CreateController(db, owner, []);

        var result = await controller.Details(3);

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual(3, ((Narudzba)((ViewResult)result).Model!).Id);
    }

    [TestMethod]
    public async Task Details_DrugiKorisnik_Forbidden()
    {
        await using var db = TestContextFactory.Create();
        db.Narudzba.Add(new Narudzba { Id = 4, IdKorisnik = "owner", Korisnik = new Korisnik { Id = "owner" } });
        await db.SaveChangesAsync();
        var controller = CreateController(db, new Korisnik { Id = "intruder" }, []);

        var result = await controller.Details(4);

        Assert.IsInstanceOfType<ForbidResult>(result);
    }

    [TestMethod]
    public async Task Details_AdministratorMozePristupitiNarudzbiDrugogKorisnika()
    {
        await using var db = TestContextFactory.Create();
        db.Narudzba.Add(new Narudzba { Id = 5, IdKorisnik = "owner", Korisnik = new Korisnik { Id = "owner" } });
        await db.SaveChangesAsync();
        var controller = CreateController(db, new Korisnik { Id = "admin" }, [Uloge.Administrator]);
        Assert.IsInstanceOfType<ViewResult>(await controller.Details(5));
    }

    [TestMethod]
    public async Task Details_ZaposlenikMozePristupitiNarudzbiDrugogKorisnika()
    {
        await using var db = TestContextFactory.Create();
        db.Narudzba.Add(new Narudzba { Id = 6, IdKorisnik = "owner", Korisnik = new Korisnik { Id = "owner" } });
        await db.SaveChangesAsync();
        var controller = CreateController(db, new Korisnik { Id = "staff" }, [Uloge.Zaposlenik]);
        Assert.IsInstanceOfType<ViewResult>(await controller.Details(6));
    }

    [TestMethod]
    public async Task Index_KupacVidiSamoVlastiteNarudzbe()
    {
        await using var db = TestContextFactory.Create();
        db.Narudzba.AddRange(
            new Narudzba { Id = 1, IdKorisnik = "buyer", Korisnik = new Korisnik { Id = "buyer" } },
            new Narudzba { Id = 2, IdKorisnik = "other", Korisnik = new Korisnik { Id = "other" } });
        await db.SaveChangesAsync();
        var result = (ViewResult)await CreateController(db, new Korisnik { Id = "buyer" }, [Uloge.Kupac]).Index();
        var orders = (List<Narudzba>)result.Model!;
        CollectionAssert.AreEqual(new[] { 1 }, orders.Select(o => o.Id).ToArray());
    }

    [TestMethod]
    public async Task Index_ZaposlenikVidiSveNarudzbe()
    {
        await using var db = TestContextFactory.Create();
        db.Narudzba.AddRange(
            new Narudzba { Id = 1, IdKorisnik = "buyer1", Korisnik = new Korisnik { Id = "buyer1" } },
            new Narudzba { Id = 2, IdKorisnik = "buyer2", Korisnik = new Korisnik { Id = "buyer2" } });
        await db.SaveChangesAsync();
        var result = (ViewResult)await CreateController(db, new Korisnik { Id = "staff" }, [Uloge.Zaposlenik]).Index();
        Assert.AreEqual(2, ((List<Narudzba>)result.Model!).Count);
    }

    [TestMethod]
    public async Task Kreiraj_PraznaKorpa_PreusmjeravaNaKorpu()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Id = "buyer" };
        db.Korpa.Add(new Korpa { Id = 9, IdKorisnik = user.Id, Korisnik = user });
        await db.SaveChangesAsync();
        var controller = CreateController(db, user, [Uloge.Kupac]);

        var result = (RedirectToActionResult)await controller.Kreiraj(new NarudzbaCheckoutViewModel { KorpaId = 9 });

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual("Korpa", result.ControllerName);
        Assert.AreEqual(0, db.Narudzba.Count());
    }

    [TestMethod]
    public async Task Kreiraj_NeispravanIznosNakonPopusta_VracaCheckoutBezNarudzbe()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Id = "buyer" };
        var product = new Proizvod { Id = 31, Naziv = "Okvir" };
        var cart = new Korpa { Id = 32, IdKorisnik = user.Id, Korisnik = user };
        cart.Stavke.Add(new StavkaKorpe { Id = 33, IdProizvod = product.Id, Proizvod = product, Kolicina = 1, Cijena = 10 });
        db.Korpa.Add(cart);
        db.Proizvod.Add(product);
        await db.SaveChangesAsync();

        var discount = new Mock<IPopustKalkulatorService>();
        discount.Setup(s => s.IzracunajPopustAsync(user.Id, cart.Id))
            .ReturnsAsync(new PopustRezultatViewModel { UkupnaCijena = -1 });
        var delivery = new Mock<IDostavaServis>(MockBehavior.Strict);
        delivery.Setup(s => s.IzracunajCijenuDostave(-1))
            .Throws(new ArgumentOutOfRangeException("iznosNakonPopusta"));
        var controller = CreateController(db, user, [Uloge.Kupac], discount.Object, dostava: delivery.Object);

        var result = await controller.Kreiraj(new NarudzbaCheckoutViewModel { KorpaId = cart.Id });

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreEqual(0, db.Narudzba.Count());
        Assert.AreEqual("Iznos narudžbe nije ispravan. Narudžba nije kreirana.",
            controller.ModelState[nameof(NarudzbaCheckoutViewModel.Popust)]!.Errors.Single().ErrorMessage);
        delivery.Verify(s => s.IzracunajCijenuDostave(-1), Times.Once);
    }

    [TestMethod]
    public async Task Kreiraj_NeispravanCheckout_VracaPopunjenuFormu()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Id = "buyer" };
        var product = new Proizvod { Id = 12, Naziv = "Okvir", DostupnaKolicina = 3 };
        var cart = new Korpa { Id = 13, IdKorisnik = user.Id, Korisnik = user };
        cart.Stavke.Add(new StavkaKorpe { Id = 14, IdProizvod = product.Id, Proizvod = product, Kolicina = 1, Cijena = 15 });
        db.Korpa.Add(cart);
        db.Proizvod.Add(product);
        await db.SaveChangesAsync();
        var discount = new Mock<IPopustKalkulatorService>();
        discount.Setup(s => s.IzracunajPopustAsync(user.Id, cart.Id)).ReturnsAsync(new PopustRezultatViewModel { UkupnaCijena = 15 });
        var delivery = new Mock<IDostavaServis>(MockBehavior.Strict);
        var controller = CreateController(db, user, [Uloge.Kupac], discount.Object, dostava: delivery.Object);
        controller.ModelState.AddModelError("AdresaIsporuke", "Neispravna adresa");

        var result = (ViewResult)await controller.Kreiraj(new NarudzbaCheckoutViewModel { KorpaId = cart.Id });

        Assert.AreSame(typeof(NarudzbaCheckoutViewModel), result.Model!.GetType());
        var model = (NarudzbaCheckoutViewModel)result.Model;
        Assert.AreEqual(13, model.Korpa.KorpaId);
        Assert.AreEqual(15d, model.Korpa.UkupnaCijena);
        Assert.AreEqual("Okvir", model.Korpa.Stavke.Single().NazivProizvoda);
        Assert.AreEqual(0, db.Narudzba.Count());
        delivery.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Edit_GetNedostajuciIdIliNarudzba_NotFound()
    {
        await using var db = TestContextFactory.Create();
        var controller = CreateController(db, new Korisnik { Id = "staff" }, [Uloge.Zaposlenik]);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Edit(null));
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Edit(90));
    }

    [TestMethod]
    public async Task Edit_GetPostojecaNarudzba_VracaModel()
    {
        await using var db = TestContextFactory.Create();
        db.Narudzba.Add(new Narudzba { Id = 91, Status = StatusNarudzbe.Zaprimljena });
        await db.SaveChangesAsync();
        var result = (ViewResult)await CreateController(db, new Korisnik { Id = "staff" }, [Uloge.Zaposlenik]).Edit(91);
        Assert.AreEqual(91, ((Narudzba)result.Model!).Id);
    }

    [TestMethod]
    public async Task Details_NedostajuciIdIliNarudzba_NotFound()
    {
        await using var db = TestContextFactory.Create();
        var controller = CreateController(db, new Korisnik { Id = "owner" }, []);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Details(null));
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Details(999));
    }

    [TestMethod]
    public async Task Edit_StatusMijenjaPostojecuNarudzbu()
    {
        await using var db = TestContextFactory.Create();
        db.Narudzba.Add(new Narudzba { Id = 7, Status = StatusNarudzbe.Zaprimljena });
        await db.SaveChangesAsync();
        var controller = CreateController(db, new Korisnik { Id = "staff" }, [Uloge.Zaposlenik]);

        var result = await controller.Edit(7, new Narudzba { Status = StatusNarudzbe.Poslana });

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(StatusNarudzbe.Poslana, (await db.Narudzba.FindAsync(7))!.Status);
    }

    [TestMethod]
    public async Task Kreiraj_ValidnaKorpa_KreiraNarudzbuIAzuriraZaliheIKupca()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Id = "buyer", BrojNarudzbi = 1, LojalnostBodovi = 2 };
        var product = new Proizvod { Id = 8, Naziv = "Okvir", DostupnaKolicina = 4, Cijena = 20 };
        var cart = new Korpa { Id = 5, IdKorisnik = user.Id, Korisnik = user, UkupnaCijena = 40 };
        cart.Stavke.Add(new StavkaKorpe { Id = 3, IdProizvod = product.Id, Proizvod = product, Kolicina = 2, Cijena = 20 });
        db.Korpa.Add(cart);
        db.Proizvod.Add(product);
        await db.SaveChangesAsync();

        var users = Users(user);
        users.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var discount = new Mock<IPopustKalkulatorService>(MockBehavior.Strict);
        discount.Setup(s => s.IzracunajPopustAsync(user.Id, cart.Id)).ReturnsAsync(new PopustRezultatViewModel
        { OsnovnaCijena = 40, PopustPostotak = 10, PopustIznos = 4, UkupnaCijena = 36 });
        var delivery = new Mock<IDostavaServis>(MockBehavior.Strict);
        delivery.Setup(s => s.IzracunajCijenuDostave(36d)).Returns(7d).Verifiable();
        var notification = new Mock<IObavijestServis>(MockBehavior.Strict);
        notification.Setup(n => n.PosaljiObavijestNarudzbaAsync(user.Id, It.IsAny<int>())).Returns(Task.CompletedTask);
        var controller = CreateController(db, user, [], discount.Object, notification.Object, users, delivery.Object);
        var checkout = new NarudzbaCheckoutViewModel
        { KorpaId = cart.Id, AdresaIsporuke = "Ulica 12", Grad = "Sarajevo", PostanskiBroj = "71000", Telefon = "061 234 567" };

        var result = await controller.Kreiraj(checkout);

        var redirect = (RedirectToActionResult)result;
        var saved = db.Narudzba.Single();
        Assert.AreEqual(nameof(NarudzbaController.Details), redirect.ActionName);
        Assert.AreEqual(40d, saved.OsnovnaCijena);
        Assert.AreEqual(4d, saved.PopustIznos);
        Assert.AreEqual(7d, saved.CijenaDostave);
        Assert.AreEqual(43d, saved.UkupnaCijena);
        Assert.AreEqual("buyer", saved.IdKorisnik);
        Assert.AreEqual("Ulica 12, Sarajevo, 71000, Tel: 061 234 567", saved.AdresaIsporuke);
        Assert.AreEqual(1, saved.Stavke.Count);
        Assert.AreEqual(2, saved.Stavke.Single().Kolicina);
        Assert.AreEqual(2, product.DostupnaKolicina);
        Assert.AreEqual(2, user.BrojNarudzbi);
        Assert.AreEqual(5, user.LojalnostBodovi);
        Assert.AreEqual(0, cart.UkupnaCijena);
        Assert.AreEqual(0, cart.Stavke.Count);
        users.VerifyAll();
        discount.VerifyAll();
        delivery.Verify(s => s.IzracunajCijenuDostave(36d), Times.Once);
        notification.VerifyAll();
    }

    private static NarudzbaController CreateController(ApplicationDbContext db, Korisnik user, string[] roles,
        IPopustKalkulatorService? popust = null, IObavijestServis? obavijest = null,
        Mock<UserManager<Korisnik>>? manager = null, IDostavaServis? dostava = null)
    {
        manager ??= Users(user);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "test")) };
        manager.Setup(m => m.GetUserAsync(context.User)).ReturnsAsync(user);
        return new NarudzbaController(db, manager.Object, popust ?? Mock.Of<IPopustKalkulatorService>(),
            obavijest ?? Mock.Of<IObavijestServis>(), dostava ?? new DostavaServis())
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
    }

    private static Mock<UserManager<Korisnik>> Users(Korisnik user) =>
        new(Mock.Of<IUserStore<Korisnik>>(), null!, null!, null!, null!, null!, null!, null!, null!);
}
