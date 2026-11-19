using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCULIS.Constants;
using OCULIS.Controllers;
using OCULIS.Models;
using OCULIS.Services.Obavijest;

namespace OCULIS.Tests.Controllers;

[TestClass]
public class ObavijestControllerTests
{
    [TestMethod]
    public async Task Index_KupacVidiSamoSvojeObavijesti_SortiranePoDatumu()
    {
        await using var db = TestContextFactory.Create();
        var own = new Korisnik { Id = "u1", UserName = "one", Email = "one@example.test", Ime = "A", Prezime = "B" };
        var other = new Korisnik { Id = "u2", UserName = "two", Email = "two@example.test", Ime = "C", Prezime = "D" };
        db.Users.AddRange(own, other);
        db.Obavijest.AddRange(
            new Obavijest { IdKorisnik = "u1", Naslov = "starija", Tekst = "x", DatumSlanja = new DateTime(2025, 1, 1) },
            new Obavijest { IdKorisnik = "u2", Naslov = "tuđa", Tekst = "x", DatumSlanja = new DateTime(2026, 1, 1) },
            new Obavijest { IdKorisnik = "u1", Naslov = "novija", Tekst = "x", DatumSlanja = new DateTime(2025, 2, 1) });
        await db.SaveChangesAsync();
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(own);
        var controller = CreateController(db, users, Uloge.Kupac);

        var result = (ViewResult)await controller.Index();

        var notices = ((IEnumerable<Obavijest>)result.Model!).ToList();
        CollectionAssert.AreEqual(new[] { "novija", "starija" }, notices.Select(x => x.Naslov).ToArray());
    }

    [TestMethod]
    public async Task Index_AdministratorVidiSveObavijesti_BezTrazenjaKorisnika()
    {
        await using var db = TestContextFactory.Create();
        var users = Clan4TestContext.CreateUserManager();
        db.Users.Add(new Korisnik { Id = "u", UserName = "u", Email = "u@example.test" });
        db.Obavijest.Add(new Obavijest { IdKorisnik = "u", Naslov = "n", Tekst = "t" });
        await db.SaveChangesAsync();
        var result = (ViewResult)await CreateController(db, users, Uloge.Administrator).Index();
        Assert.AreEqual(1, ((IEnumerable<Obavijest>)result.Model!).Count());
        users.Verify(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [TestMethod]
    public async Task Details_NullIliNepostojeciId_VracaNotFound()
    {
        await using var db = TestContextFactory.Create();
        var controller = CreateController(db, Clan4TestContext.CreateUserManager(), Uloge.Administrator);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Details(null));
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Details(1));
    }

    [TestMethod]
    public async Task Details_KupacOtvaraTudjuObavijest_VracaForbid()
    {
        await using var db = TestContextFactory.Create();
        db.Users.Add(new Korisnik { Id = "other", UserName = "other", Email = "other@example.test" });
        db.Obavijest.Add(new Obavijest { IdKorisnik = "other", Naslov = "n", Tekst = "t" });
        await db.SaveChangesAsync();
        var user = new Korisnik { Id = "user-1", UserName = "u" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        Assert.IsInstanceOfType<ForbidResult>(await CreateController(db, users, Uloge.Kupac).Details(1));
    }

    [TestMethod]
    public async Task Details_KupacOtvaraSvojuObavijest_VracaModel()
    {
        await using var db = TestContextFactory.Create();
        db.Users.Add(new Korisnik { Id = "user-1", UserName = "user-1", Email = "user@example.test" });
        var notice = new Obavijest { IdKorisnik = "user-1", Naslov = "n", Tekst = "t" };
        db.Obavijest.Add(notice);
        await db.SaveChangesAsync();
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(new Korisnik { Id = "user-1" });
        Assert.AreSame(notice, ((ViewResult)await CreateController(db, users, Uloge.Kupac).Details(notice.Id)).Model);
    }

    [TestMethod]
    public async Task Create_EmailNijePronadjen_DodajeGreskuIPrikazujeFormu()
    {
        await using var db = TestContextFactory.Create();
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByEmailAsync("missing@example.test")).ReturnsAsync((Korisnik?)null);
        var service = new Mock<IObavijestServis>(MockBehavior.Strict);
        var controller = Clan4TestContext.Controller(new ObavijestController(db, users.Object, service.Object));
        var result = await controller.Create("missing@example.test", "naslov", "tekst");
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsTrue(controller.ModelState[string.Empty]!.Errors.Single().ErrorMessage.Contains("nije pronađen"));
        service.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Create_EmailPostoji_PozivaServisIZatvaraFormu()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Id = "u1", Email = "u@example.test" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        var service = new Mock<IObavijestServis>(MockBehavior.Strict);
        service.Setup(x => x.PosaljiObavijestAsync("u1", "Pozdrav", "Sadrzaj", true)).Returns(Task.CompletedTask);
        var controller = Clan4TestContext.Controller(new ObavijestController(db, users.Object, service.Object));

        var result = await controller.Create(user.Email!, "Pozdrav", "Sadrzaj");

        Assert.AreEqual("Index", ((RedirectToActionResult)result).ActionName);
        Assert.AreEqual("Obavijest poslana.", controller.TempData["Success"]);
        service.Verify(x => x.PosaljiObavijestAsync("u1", "Pozdrav", "Sadrzaj", true), Times.Once);
    }

    private static ObavijestController CreateController(OCULIS.Data.ApplicationDbContext db,
        Moq.Mock<Microsoft.AspNetCore.Identity.UserManager<Korisnik>> users, string role)
    {
        var service = new Mock<IObavijestServis>();
        var controller = new ObavijestController(db, users.Object, service.Object);
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"))
        };
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }
}
