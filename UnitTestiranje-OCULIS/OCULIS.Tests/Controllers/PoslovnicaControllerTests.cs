using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OCULIS.Controllers;
using OCULIS.Models;

namespace OCULIS.Tests.Controllers;

[TestClass]
public class PoslovnicaControllerTests
{
    [TestMethod]
    public async Task Index_PraznaBaza_VracaPraznuListuIKonfiguriraniApiKey()
    {
        await using var db = TestContextFactory.Create();
        var controller = CreateController(db, "key-123");
        var result = (ViewResult)await controller.Index();
        Assert.AreEqual("key-123", controller.ViewBag.GoogleMapsApiKey);
        Assert.AreEqual(0, ((IEnumerable<Poslovnica>)result.Model!).Count());
    }

    [TestMethod]
    public async Task Details_NullIdIliNepostojeciId_VracaNotFound()
    {
        await using var db = TestContextFactory.Create();
        var controller = CreateController(db, null);
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Details(null));
        Assert.IsInstanceOfType<NotFoundResult>(await controller.Details(42));
    }

    [TestMethod]
    public async Task Details_PostojecaPoslovnica_VracaModelIKonfiguraciju()
    {
        await using var db = TestContextFactory.Create();
        var branch = ValidBranch();
        db.Poslovnica.Add(branch);
        await db.SaveChangesAsync();
        var controller = CreateController(db, null);
        var result = (ViewResult)await controller.Details(branch.Id);
        Assert.AreSame(branch, result.Model);
        Assert.AreEqual("", controller.ViewBag.GoogleMapsApiKey);
    }

    [TestMethod]
    public async Task Create_IspravanModel_CuvaPoslovnicuIPreusmjerava()
    {
        await using var db = TestContextFactory.Create();
        var controller = Clan4TestContext.Controller(CreateController(db, null));
        var result = await controller.Create(ValidBranch());
        AssertRedirectToIndex(result);
        Assert.AreEqual(1, await db.Poslovnica.CountAsync());
        Assert.AreEqual("Poslovnica kreirana.", controller.TempData["Success"]);
    }

    [TestMethod]
    public async Task Create_NeispravanModel_VracaUneseniModelBezUpisa()
    {
        await using var db = TestContextFactory.Create();
        var controller = CreateController(db, null);
        controller.ModelState.AddModelError("Naziv", "obavezan");
        var branch = ValidBranch();
        Assert.AreSame(branch, ((ViewResult)await controller.Create(branch)).Model);
        Assert.AreEqual(0, await db.Poslovnica.CountAsync());
    }

    [TestMethod]
    public async Task Edit_IdSeNePoklapa_VracaNotFound()
    {
        await using var db = TestContextFactory.Create();
        Assert.IsInstanceOfType<NotFoundResult>(await CreateController(db, null).Edit(1, new Poslovnica { Id = 2 }));
    }

    [TestMethod]
    public async Task Edit_NepostojecaPoslovnica_VracaNotFound()
    {
        await using var db = TestContextFactory.Create();
        Assert.IsInstanceOfType<NotFoundResult>(await CreateController(db, null).Edit(10));
    }

    [TestMethod]
    public async Task Edit_IspravanModelAzuriraSlog()
    {
        await using var db = TestContextFactory.Create();
        var branch = ValidBranch();
        db.Poslovnica.Add(branch);
        await db.SaveChangesAsync();
        var changed = new Poslovnica { Id = branch.Id, Naziv = "Nova", Adresa = "Nova 5", Telefon = "111", RadnoVrijeme = "09-17" };
        db.ChangeTracker.Clear();
        var controller = Clan4TestContext.Controller(CreateController(db, null));
        AssertRedirectToIndex(await controller.Edit(branch.Id, changed));
        Assert.AreEqual("Nova", (await db.Poslovnica.FindAsync(branch.Id))!.Naziv);
    }

    [TestMethod]
    public async Task Delete_NepostojecaPoslovnica_VracaNotFound()
    {
        await using var db = TestContextFactory.Create();
        Assert.IsInstanceOfType<NotFoundResult>(await CreateController(db, null).Delete(1));
    }

    [TestMethod]
    public async Task DeleteConfirmed_PostojecaPoslovnica_UklanjaJe()
    {
        await using var db = TestContextFactory.Create();
        var branch = ValidBranch();
        db.Poslovnica.Add(branch);
        await db.SaveChangesAsync();
        AssertRedirectToIndex(await CreateController(db, null).DeleteConfirmed(branch.Id));
        Assert.AreEqual(0, await db.Poslovnica.CountAsync());
    }

    private static PoslovnicaController CreateController(OCULIS.Data.ApplicationDbContext db, string? apiKey)
    {
        var values = new Dictionary<string, string?>();
        if (apiKey != null) values["GoogleMaps:ApiKey"] = apiKey;
        return new PoslovnicaController(db, new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }

    private static Poslovnica ValidBranch() => new()
    {
        Naziv = "Centar", Adresa = "Ulica 1", Telefon = "033-123456", RadnoVrijeme = "Pon-Pet 09-17"
    };

    private static void AssertRedirectToIndex(IActionResult result) => Assert.AreEqual("Index", ((RedirectToActionResult)result).ActionName);
}
