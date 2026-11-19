using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OCULIS.Controllers;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
public class KorisnikTests
{
    [TestMethod]
    public void Create_Get_VracaPrazanFormular()
    {
        var controller = new KorisnikController(Clan4TestContext.CreateUserManager().Object, Clan4TestContext.CreateRoleManager().Object);
        Assert.IsInstanceOfType<ViewResult>(controller.Create());
    }

    [TestMethod]
    public async Task Index_VracaKorisnikeISvakomDodjeljujeUloge()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Id = "i1", UserName = "i1", Email = "i1@example.test" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var users = Clan4TestContext.CreateUserManager();
        users.SetupGet(x => x.Users).Returns(db.Users);
        users.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Kupac" });
        var controller = new KorisnikController(users.Object, Clan4TestContext.CreateRoleManager().Object);
        var result = (ViewResult)await controller.Index();
        Assert.AreSame(user, ((IEnumerable<Korisnik>)result.Model!).Single());
        CollectionAssert.AreEqual(new[] { "Kupac" }, ((Dictionary<string, IList<string>>)controller.ViewBag.Uloge)["i1"].ToArray());
    }

    [TestMethod]
    [DataRow("aktivan", true)]
    [DataRow("nepoznat", false)]
    public async Task Details_PostojanjeKorisnika_VracaProfilIliNotFound(string id, bool postoji)
    {
        var korisnik = new Korisnik { Id = "aktivan", Ime = "Ada" };
        var users = new Mock<UserManager<Korisnik>>(Mock.Of<IUserStore<Korisnik>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        users.Setup(u => u.FindByIdAsync(id)).ReturnsAsync(postoji ? korisnik : null);
        users.Setup(u => u.GetRolesAsync(korisnik)).ReturnsAsync(new List<string> { "Kupac" });
        var controller = new KorisnikController(users.Object, null!);

        var rezultat = await controller.Details(id);

        if (postoji)
        {
            Assert.AreSame(korisnik, ((ViewResult)rezultat).Model);
            CollectionAssert.AreEqual(new[] { "Kupac" }, ((IList<string>)controller.ViewBag.Uloge).ToArray());
        }
        else
        {
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        users.Verify(u => u.FindByIdAsync(id), Times.Once);
        users.Verify(u => u.GetRolesAsync(korisnik), postoji ? Times.Once() : Times.Never());
    }

    [TestMethod]
    public async Task Create_PostanIspravanIUlogaPostoji_KreiraKorisnikaIProslijediUlogu()
    {
        var users = Clan4TestContext.CreateUserManager();
        var roles = Clan4TestContext.CreateRoleManager();
        var user = new Korisnik { Ime = "Lejla", Prezime = "H." , Email = "lejla@example.test" };
        users.Setup(x => x.CreateAsync(user, "Pass123!")).ReturnsAsync(IdentityResult.Success);
        roles.Setup(x => x.RoleExistsAsync("Kupac")).ReturnsAsync(true);
        users.Setup(x => x.AddToRoleAsync(user, "Kupac")).ReturnsAsync(IdentityResult.Success);
        var controller = Clan4TestContext.Controller(new KorisnikController(users.Object, roles.Object));

        var result = await controller.Create(user, "Pass123!", "Kupac");

        AssertRedirectToIndex(result);
        Assert.AreEqual(user.Email, user.UserName);
        Assert.IsTrue(user.EmailConfirmed);
        Assert.AreEqual("Korisnik kreiran.", controller.TempData["Success"]);
        users.Verify(x => x.AddToRoleAsync(user, "Kupac"), Times.Once);
    }

    [TestMethod]
    public async Task Create_PraznaModelState_NePozivaCreateAsync()
    {
        var users = Clan4TestContext.CreateUserManager();
        var controller = Clan4TestContext.Controller(new KorisnikController(users.Object, Clan4TestContext.CreateRoleManager().Object));
        controller.ModelState.AddModelError("Ime", "obavezno");
        var result = await controller.Create(new Korisnik(), "lozinka", "Kupac");
        Assert.IsInstanceOfType<ViewResult>(result);
        users.Verify(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Create_NeuspjehIdentity_DodajeGreskuIPrikazujeFormu()
    {
        var users = Clan4TestContext.CreateUserManager();
        var user = new Korisnik { Ime = "A", Prezime = "B", Email = "a@b.test" };
        users.Setup(x => x.CreateAsync(user, "weak")).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Lozinka preslaba" }));
        var controller = Clan4TestContext.Controller(new KorisnikController(users.Object, Clan4TestContext.CreateRoleManager().Object));
        var result = await controller.Create(user, "weak", "");
        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsTrue(controller.ModelState[string.Empty]!.Errors.Any(e => e.ErrorMessage == "Lozinka preslaba"));
    }

    [TestMethod]
    public async Task Create_UlogaNepostojeca_IpakKreiraKorisnikaBezDodjeleUloge()
    {
        var users = Clan4TestContext.CreateUserManager();
        var roles = Clan4TestContext.CreateRoleManager();
        var user = new Korisnik { Ime = "A", Prezime = "B", Email = "a@example.test" };
        users.Setup(x => x.CreateAsync(user, "Pass123!")).ReturnsAsync(IdentityResult.Success);
        roles.Setup(x => x.RoleExistsAsync("Nepoznata")).ReturnsAsync(false);
        var result = await Clan4TestContext.Controller(new KorisnikController(users.Object, roles.Object)).Create(user, "Pass123!", "Nepoznata");
        AssertRedirectToIndex(result);
        users.Verify(x => x.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_IdSeNePoklapaSaModelom_VracaNotFound()
    {
        var controller = new KorisnikController(Clan4TestContext.CreateUserManager().Object, Clan4TestContext.CreateRoleManager().Object);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Edit("jedan", new Korisnik { Id = "drugi" }, "Kupac"));
    }

    [TestMethod]
    public async Task Edit_NepostojeciKorisnik_VracaNotFound()
    {
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((Korisnik?)null);
        var controller = new KorisnikController(users.Object, Clan4TestContext.CreateRoleManager().Object);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Edit("missing", new Korisnik { Id = "missing", Ime = "A", Prezime = "B" }, "Kupac"));
    }

    [TestMethod]
    public async Task Edit_Get_NepostojeciIdVracaNotFoundAPostojeciPripremaUlogu()
    {
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByIdAsync("ok")).ReturnsAsync(new Korisnik { Id = "ok" });
        users.Setup(x => x.GetRolesAsync(It.IsAny<Korisnik>())).ReturnsAsync(new List<string> { "Kupac" });
        var controller = new KorisnikController(users.Object, Clan4TestContext.CreateRoleManager().Object);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Edit("missing"));
        Assert.IsInstanceOfType<ViewResult>(await controller.Edit("ok"));
        Assert.AreEqual("Kupac", controller.ViewBag.TrenutnaUloga);
    }

    [TestMethod]
    public async Task Edit_UspjesnoAzuiranje_MijenjaPodatkeIUlоgu()
    {
        var existing = new Korisnik { Id = "u1", Ime = "Staro", Prezime = "Ime", Email = "old@example.test" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByIdAsync("u1")).ReturnsAsync(existing);
        users.Setup(x => x.UpdateAsync(existing)).ReturnsAsync(IdentityResult.Success);
        users.Setup(x => x.GetRolesAsync(existing)).ReturnsAsync(new List<string> { "Kupac" });
        users.Setup(x => x.RemoveFromRolesAsync(existing, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        users.Setup(x => x.AddToRoleAsync(existing, "Zaposlenik")).ReturnsAsync(IdentityResult.Success);
        var roles = Clan4TestContext.CreateRoleManager();
        var controller = Clan4TestContext.Controller(new KorisnikController(users.Object, roles.Object));
        var model = new Korisnik { Id = "u1", Ime = "Novo", Prezime = "Ime", Email = "new@example.test", Telefon = "123" };

        var result = await controller.Edit("u1", model, "Zaposlenik");

        AssertRedirectToIndex(result);
        Assert.AreEqual("Novo", existing.Ime);
        Assert.AreEqual("new@example.test", existing.UserName);
        users.Verify(x => x.RemoveFromRolesAsync(existing, It.IsAny<IEnumerable<string>>()), Times.Once);
        users.Verify(x => x.AddToRoleAsync(existing, "Zaposlenik"), Times.Once);
    }

    [TestMethod]
    public async Task DeleteConfirmed_KorisnikNePostoji_SamoPreusmjerava()
    {
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByIdAsync("x")).ReturnsAsync((Korisnik?)null);
        var controller = new KorisnikController(users.Object, Clan4TestContext.CreateRoleManager().Object);
        AssertRedirectToIndex(await controller.DeleteConfirmed("x"));
        users.Verify(x => x.DeleteAsync(It.IsAny<Korisnik>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_Get_NullOrUnknownIdNotFound_ExistingUserShown()
    {
        var user = new Korisnik { Id = "u1" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByIdAsync("u1")).ReturnsAsync(user);
        var controller = new KorisnikController(users.Object, Clan4TestContext.CreateRoleManager().Object);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Delete(null!));
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Delete("missing"));
        Assert.AreSame(user, ((ViewResult)await controller.Delete("u1")).Model);
    }

    [TestMethod]
    public async Task DeleteConfirmed_PostojeciKorisnikPozivaDelete()
    {
        var user = new Korisnik { Id = "u1" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByIdAsync("u1")).ReturnsAsync(user);
        users.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
        var controller = new KorisnikController(users.Object, Clan4TestContext.CreateRoleManager().Object);
        AssertRedirectToIndex(await controller.DeleteConfirmed("u1"));
        users.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    private static void AssertRedirectToIndex(IActionResult result)
    {
        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Index", redirect.ActionName);
    }
}
