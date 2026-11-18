using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;
using Moq;
using OCULIS.Controllers;
using OCULIS.Data;
using OCULIS.Models;
using OCULIS.Constants;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan2")]
public class ReklamacijaControllerTests
{
    [TestMethod]
    public async Task Create_PrazanOpis_VracaFormuBezSnimanja()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Id = "u1", Email = "u@example.com" };
        var email = new Mock<IEmailSender>(MockBehavior.Strict);
        var controller = CreateController(db, user, email.Object);

        var result = await controller.Create(new Reklamacija { Opis = " " });

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsFalse(controller.ModelState.IsValid);
        Assert.AreEqual(0, db.Reklamacija.Count());
        email.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task Create_ValidnaReklamacija_SpremaJeSaljeEmail()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Id = "u2", Email = "u@example.com" };
        var email = new Mock<IEmailSender>(MockBehavior.Strict);
        email.Setup(e => e.SendEmailAsync(user.Email, It.Is<string>(s => s.Contains("Reklamacija")), It.Is<string>(s => s.Contains("Opis reklamacije"))))
            .Returns(Task.CompletedTask);
        var controller = CreateController(db, user, email.Object);

        var result = await controller.Create(new Reklamacija { Opis = "Proizvod je oštećen" });

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(nameof(ReklamacijaController.Potvrda), ((RedirectToActionResult)result).ActionName);
        var saved = db.Reklamacija.Single();
        Assert.AreEqual(user.Id, saved.IdKorisnik);
        Assert.AreEqual(StatusReklamacije.Podnesena, saved.Status);
        Assert.IsTrue(saved.DatumPodnosenja > DateTime.MinValue);
        email.VerifyAll();
    }

    [TestMethod]
    public async Task Edit_NepostojecaReklamacija_NotFound()
    {
        await using var db = TestContextFactory.Create();
        var controller = CreateController(db, new Korisnik { Id = "staff" }, Mock.Of<IEmailSender>());
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Edit(15));
    }

    [TestMethod]
    public async Task Create_GetPrikazujePraznuListuNarudzbi()
    {
        await using var db = TestContextFactory.Create();
        var controller = CreateController(db, new Korisnik { Id = "buyer" }, Mock.Of<IEmailSender>(), [Uloge.Kupac]);
        Assert.IsInstanceOfType<ViewResult>(await controller.Create());
        Assert.IsNotNull(controller.ViewData["IdNarudzba"]);
    }

    [TestMethod]
    public async Task Edit_PostAzuriraStatusIOdgovor()
    {
        await using var db = TestContextFactory.Create();
        db.Reklamacija.Add(new Reklamacija { Id = 8, Opis = "Opis", Status = StatusReklamacije.Podnesena });
        await db.SaveChangesAsync();
        var controller = CreateController(db, new Korisnik { Id = "staff" }, Mock.Of<IEmailSender>(), [Uloge.Zaposlenik]);

        var result = await controller.Edit(8, new Reklamacija { Status = StatusReklamacije.Odobrena, Odgovor = "Prihvaćeno" });

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        var saved = await db.Reklamacija.FindAsync(8);
        Assert.AreEqual(StatusReklamacije.Odobrena, saved!.Status);
        Assert.AreEqual("Prihvaćeno", saved.Odgovor);
    }

    [TestMethod]
    public async Task Index_KupacVidiSamoSvojeReklamacije()
    {
        await using var db = TestContextFactory.Create();
        db.Reklamacija.AddRange(
            new Reklamacija { Id = 1, Opis = "Moja", IdKorisnik = "buyer", Korisnik = new Korisnik { Id = "buyer" } },
            new Reklamacija { Id = 2, Opis = "Tuđa", IdKorisnik = "other", Korisnik = new Korisnik { Id = "other" } });
        await db.SaveChangesAsync();
        var result = (ViewResult)await CreateController(db, new Korisnik { Id = "buyer" }, Mock.Of<IEmailSender>(), [Uloge.Kupac]).Index();
        var complaints = (List<Reklamacija>)result.Model!;
        CollectionAssert.AreEqual(new[] { 1 }, complaints.Select(r => r.Id).ToArray());
    }

    [TestMethod]
    public async Task Edit_PostNedostajucaReklamacija_NotFound()
    {
        await using var db = TestContextFactory.Create();
        var result = await CreateController(db, new Korisnik { Id = "staff" }, Mock.Of<IEmailSender>(), [Uloge.Zaposlenik])
            .Edit(404, new Reklamacija { Status = StatusReklamacije.Odbijena });
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    private static ReklamacijaController CreateController(ApplicationDbContext db, Korisnik user, IEmailSender sender, string[]? roles = null)
    {
        var manager = new Mock<UserManager<Korisnik>>(Mock.Of<IUserStore<Korisnik>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity((roles ?? []).Select(r => new Claim(ClaimTypes.Role, r)), "test"))
        };
        manager.Setup(m => m.GetUserAsync(context.User)).ReturnsAsync(user);
        return new ReklamacijaController(db, manager.Object, sender)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
    }
}
