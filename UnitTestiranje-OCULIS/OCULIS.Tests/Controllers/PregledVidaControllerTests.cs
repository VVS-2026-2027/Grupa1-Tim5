using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OCULIS.Constants;
using OCULIS.Controllers;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class PregledVidaControllerTests
{
    [TestMethod]
    public async Task Create_ValidanPregled_SpremaIPrikazujeKarton()
    {
        await using var db = TestContextFactory.Create();
        var controller = Clan3TestContext.Controller(new PregledVidaController(db), Uloge.Zaposlenik);
        var karton = new ElektronskiKarton { Korisnik = new Korisnik { Ime = "Ana", Prezime = "Test" } };
        db.Add(karton);
        await db.SaveChangesAsync();
        var forma = (ViewResult)await controller.Create(karton.Id);
        Assert.AreEqual(karton.Id, ((PregledVida)forma.Model!).IdElektronskiKarton);
        Assert.AreEqual("Ana Test - Karton #" + karton.Id,
            ((SelectList)controller.ViewData["IdElektronskiKarton"]!).Single().Text);

        var pregled = new PregledVida { IdElektronskiKarton = karton.Id, Preporuka = "Kontrola", DioptrijaLijevo = -1.25, DioptrijaDesno = 0.50, DatumPregleda = new(2026, 11, 1) };
        controller.ModelState.AddModelError(nameof(PregledVida.ElektronskiKarton), "Navigacija");
        var redirect = (RedirectToActionResult)await controller.Create(pregled);
        Assert.AreEqual("Details", redirect.ActionName);
        Assert.AreEqual("ElektronskiKarton", redirect.ControllerName);
        Assert.AreEqual(karton.Id, redirect.RouteValues!["id"]);
        db.ChangeTracker.Clear();
        var saved = db.PregledVida.Single();
        Assert.AreEqual("Kontrola", saved.Preporuka);
        Assert.IsTrue(saved.Id > 0);
        Assert.AreEqual(-1.25, saved.DioptrijaLijevo);
        Assert.AreEqual(0.50, saved.DioptrijaDesno);

        db.Add(new PregledVida { IdElektronskiKarton = karton.Id, Preporuka = "Ranije", DatumPregleda = pregled.DatumPregleda.AddDays(-1) });
        await db.SaveChangesAsync();
        var lista = (List<PregledVida>)((ViewResult)await controller.Index()).Model!;
        CollectionAssert.AreEqual(new[] { "Kontrola", "Ranije" }, lista.Select(p => p.Preporuka).ToArray());
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(5)]
    public async Task Create_NeispravanUnos_VracaFormuBezUpisa(int kartonId)
    {
        await using var db = TestContextFactory.Create();
        var controller = new PregledVidaController(db);
        var get = (ViewResult)await controller.Create((int?)null);
        Assert.AreEqual(0, ((PregledVida)get.Model!).IdElektronskiKarton);
        var pregled = new PregledVida { IdElektronskiKarton = kartonId };
        string polje = kartonId == 0 ? nameof(PregledVida.IdElektronskiKarton) : nameof(PregledVida.Preporuka);
        if (kartonId != 0)
            controller.ModelState.AddModelError(polje, "Preporuka je obavezna.");
        Assert.AreSame(pregled, ((ViewResult)await controller.Create(pregled)).Model);
        Assert.IsTrue(controller.ModelState[polje]!.Errors.Count > 0);
        Assert.IsFalse(db.PregledVida.Any());
    }
}
