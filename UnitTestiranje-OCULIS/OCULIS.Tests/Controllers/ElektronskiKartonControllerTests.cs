using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OCULIS.Constants;
using OCULIS.Controllers;
using OCULIS.Models;
using OCULIS.Models.ViewModels;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class ElektronskiKartonControllerTests
{
    [TestMethod]
    [DataRow(Uloge.Kupac, 1)]
    [DataRow(Uloge.Zaposlenik, 2)]
    public async Task Index_FiltriraPoUloziISortira(string role, int broj)
    {
        await using var db = TestContextFactory.Create();
        var vlasnik = new Korisnik();
        db.AddRange(new ElektronskiKarton { Korisnik = vlasnik, Napomena = "Stariji" },
            new ElektronskiKarton { Korisnik = new Korisnik(), Napomena = "Noviji", DatumKreiranja = new(2026, 1, 1) });
        await db.SaveChangesAsync();
        var controller = Clan3TestContext.Controller(new ElektronskiKartonController(db,
            Clan3TestContext.Users(vlasnik).Object), role);
        var kartoni = (List<ElektronskiKarton>)((ViewResult)await controller.Index()).Model!;
        Assert.AreEqual(broj, kartoni.Count);
        Assert.AreEqual(broj == 1 ? "Stariji" : "Noviji", kartoni[0].Napomena);
    }

    [TestMethod]
    public async Task Details_ProvjeravaPristupIPrikazujePovezaneZapise()
    {
        await using var db = TestContextFactory.Create();
        var vlasnik = new Korisnik();
        var drugi = new Korisnik();
        var karton = new ElektronskiKarton { Korisnik = vlasnik };
        db.AddRange(karton, new PregledVida { ElektronskiKarton = karton, Preporuka = "Kontrola" },
            new Narudzba { Korisnik = vlasnik, AdresaIsporuke = "Ulica 5" },
            new Narudzba { Korisnik = drugi, AdresaIsporuke = "Ulica 6" },
            new Reklamacija { Korisnik = vlasnik, Opis = "Po korisniku" },
            new Reklamacija { Korisnik = drugi, ElektronskiKarton = karton, Opis = "Po kartonu" },
            new Reklamacija { Korisnik = drugi, Opis = "Nepovezana" });
        await db.SaveChangesAsync();
        var strani = Clan3TestContext.Controller(new ElektronskiKartonController(db, Clan3TestContext.Users(drugi).Object), Uloge.Kupac);
        Assert.IsInstanceOfType<NotFoundResult>(await strani.Details(null));
        Assert.IsInstanceOfType<NotFoundResult>(await strani.Details(-1));
        Assert.IsInstanceOfType<ForbidResult>(await strani.Details(karton.Id));
        var domaci = Clan3TestContext.Controller(new ElektronskiKartonController(db, Clan3TestContext.Users(vlasnik).Object), Uloge.Kupac);
        var model = (ElektronskiKartonDetaljiViewModel)((ViewResult)await domaci.Details(karton.Id)).Model!;
        var osoblje = Clan3TestContext.Controller(new ElektronskiKartonController(db,
            Clan3TestContext.Users().Object), Uloge.Zaposlenik);
        Assert.IsInstanceOfType<ViewResult>(await osoblje.Details(karton.Id));
        Assert.AreEqual(karton.Id, model.Karton.Id);
        Assert.AreEqual("Kontrola", model.Pregledi.Single().Preporuka);
        Assert.AreEqual("Ulica 5", model.Narudzbe.Single().AdresaIsporuke);
        CollectionAssert.AreEquivalent(new[] { "Po korisniku", "Po kartonu" }, model.Reklamacije.Select(r => r.Opis).ToArray());
    }

    [TestMethod]
    public async Task Create_PopunjavaKorisnikeISpremaSamoValidanKarton()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik { Ime = "Ana", Prezime = "Test" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var users = Clan3TestContext.Users();
        users.SetupGet(m => m.Users).Returns(db.Users);
        var controller = Clan3TestContext.Controller(new ElektronskiKartonController(db, users.Object), Uloge.Zaposlenik);
        var karton = new ElektronskiKarton { IdKorisnik = user.Id, Napomena = "Napomena" };

        Assert.IsInstanceOfType<ViewResult>(await controller.Create());
        Assert.AreEqual("Ana Test", ((SelectList)controller.ViewData["IdKorisnik"]!).Single().Text);
        controller.ModelState.AddModelError(nameof(karton.Napomena), "Neispravan unos");
        Assert.AreSame(karton, ((ViewResult)await controller.Create(karton)).Model);
        Assert.IsFalse(db.ElektronskiKarton.Any());

        controller.ModelState.Clear();
        var redirect = (RedirectToActionResult)await controller.Create(karton);
        Assert.AreEqual("Details", redirect.ActionName);
        Assert.AreEqual(karton.Id, redirect.RouteValues!["id"]);
        var saved = db.ElektronskiKarton.Single();
        Assert.AreEqual(user.Id, saved.IdKorisnik);
        Assert.AreNotEqual(default(DateTime), saved.DatumKreiranja);
    }

    [TestMethod]
    public async Task MojKarton_PonovljeniPoziv_KoristiIstiKarton()
    {
        await using var db = TestContextFactory.Create();
        var user = new Korisnik();
        var controller = new ElektronskiKartonController(db, Clan3TestContext.Users(user).Object);
        var prvi = (RedirectToActionResult)await controller.MojKarton();
        var drugi = (RedirectToActionResult)await controller.MojKarton();
        Assert.AreEqual(prvi.RouteValues!["id"], drugi.RouteValues!["id"]);
        Assert.AreEqual(user.Id, db.ElektronskiKarton.Single().IdKorisnik);
    }
}
