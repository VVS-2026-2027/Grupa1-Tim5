using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCULIS.Controllers;
using OCULIS.Data;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
public class AkcijaControllerTests
{
    [TestMethod]
    public async Task Index_OpstaIAkcijaZaProizvod_SortiraIUcitavaProizvod()
    {
        await using var db = TestContextFactory.Create();
        var datum = new DateTime(2026, 1, 1);
        db.Akcija.AddRange(new Akcija { Naziv = "Opšta", DatumPocetka = datum },
            new Akcija { Naziv = "Okviri", DatumPocetka = datum.AddDays(1), Proizvod = new Proizvod { Naziv = "Okvir" } });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var rezultat = (ViewResult)await new AkcijaController(db).Index();

        var akcije = (List<Akcija>)rezultat.Model!;
        CollectionAssert.AreEqual(new[] { "Okviri", "Opšta" }, akcije.Select(a => a.Naziv).ToArray());
        Assert.AreEqual("Okvir", akcije[0].Proizvod!.Naziv);
        Assert.IsNull(akcije[1].Proizvod);
    }

    [TestMethod]
    public async Task Create_FormaZaNovuAkciju_NudiProizvode()
    {
        await using var db = TestContextFactory.Create();
        db.Proizvod.Add(new Proizvod { Id = 4, Naziv = "Okvir" });
        await db.SaveChangesAsync();
        var controller = Controller(db);

        var rezultat = (ViewResult)await controller.Create();

        var proizvod = ((SelectList)controller.ViewData["IdProizvod"]!).Single();
        Assert.IsNull(rezultat.Model);
        Assert.AreEqual("4", proizvod.Value);
        Assert.AreEqual("Okvir", proizvod.Text);
    }

    [TestMethod]
    [DataRow(0d)]
    [DataRow(100d)]
    public async Task Create_GranicniValidniPostotak_SpremaAkciju(double postotak)
    {
        await using var db = TestContextFactory.Create();
        var controller = Controller(db);
        var akcija = new Akcija { Naziv = "Popust", PopustPostotak = postotak };
        PrimijeniValidaciju(controller, akcija);

        var rezultat = (RedirectToActionResult)await controller.Create(akcija);
        db.ChangeTracker.Clear();

        Assert.AreEqual(nameof(AkcijaController.Index), rezultat.ActionName);
        Assert.AreEqual(postotak, (await db.Akcija.SingleAsync()).PopustPostotak);
        Assert.IsTrue(controller.TempData.ContainsKey("Success"));
    }

    [TestMethod]
    [DataRow(-1d)]
    [DataRow(101d)]
    public async Task Create_PostotakIzvanRaspona_VracaModelIIzabraniProizvod(double postotak)
    {
        await using var db = TestContextFactory.Create();
        db.Proizvod.Add(new Proizvod { Id = 4, Naziv = "Okvir" });
        await db.SaveChangesAsync();
        var controller = Controller(db);
        var akcija = new Akcija { Naziv = "Popust", IdProizvod = 4, PopustPostotak = postotak };
        PrimijeniValidaciju(controller, akcija);

        var rezultat = (ViewResult)await controller.Create(akcija);

        Assert.AreSame(akcija, rezultat.Model);
        Assert.IsTrue(controller.ModelState.ContainsKey(nameof(Akcija.PopustPostotak)));
        Assert.IsTrue(((SelectList)controller.ViewData["IdProizvod"]!).Single().Selected);
        Assert.IsFalse(await db.Akcija.AnyAsync());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(404)]
    public async Task Edit_NedostajeAkcija_VracaNotFound(int? id)
    {
        await using var db = TestContextFactory.Create();

        var rezultat = await new AkcijaController(db).Edit(id);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Edit_PostojecaAkcija_VracaOdabraniProizvod()
    {
        await using var db = TestContextFactory.Create();
        db.Akcija.Add(new Akcija { Id = 3, Proizvod = new Proizvod { Id = 4, Naziv = "Okvir" } });
        await db.SaveChangesAsync();
        var controller = Controller(db);

        var rezultat = (ViewResult)await controller.Edit(3);

        Assert.AreEqual(3, ((Akcija)rezultat.Model!).Id);
        Assert.IsTrue(((SelectList)controller.ViewData["IdProizvod"]!).Single().Selected);
    }

    [TestMethod]
    public async Task Edit_IdentifikatoriSeRazlikuju_VracaNotFoundBezPromjene()
    {
        await using var db = TestContextFactory.Create();
        db.Akcija.Add(new Akcija { Id = 3, PopustPostotak = 10 });
        await db.SaveChangesAsync();

        var rezultat = await new AkcijaController(db).Edit(4, new Akcija { Id = 3, PopustPostotak = 5 });

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(10d, (await db.Akcija.SingleAsync()).PopustPostotak);
    }

    [TestMethod]
    public async Task Edit_ValidnaPromjena_SpremaNoviPopust()
    {
        await using var db = TestContextFactory.Create();
        db.Akcija.Add(new Akcija { Id = 3, PopustPostotak = 10 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var controller = Controller(db);
        var akcija = new Akcija { Id = 3, Naziv = "Zimska", PopustPostotak = 20, Aktivna = false };
        PrimijeniValidaciju(controller, akcija);

        var rezultat = (RedirectToActionResult)await controller.Edit(3, akcija);
        db.ChangeTracker.Clear();

        var spremljena = await db.Akcija.SingleAsync();
        Assert.AreEqual(nameof(AkcijaController.Index), rezultat.ActionName);
        Assert.AreEqual(20d, spremljena.PopustPostotak);
        Assert.IsFalse(spremljena.Aktivna);
    }

    [TestMethod]
    public async Task Edit_PrazanNaziv_VracaUnosICuvaSpremljenuAkciju()
    {
        await using var db = TestContextFactory.Create();
        db.Akcija.Add(new Akcija { Id = 3, Naziv = "Zimska" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var controller = Controller(db);
        var akcija = new Akcija { Id = 3, Naziv = " " };
        PrimijeniValidaciju(controller, akcija);

        var rezultat = (ViewResult)await controller.Edit(3, akcija);

        Assert.AreSame(akcija, rezultat.Model);
        Assert.IsTrue(controller.ModelState.ContainsKey(nameof(Akcija.Naziv)));
        Assert.AreEqual("Zimska", (await db.Akcija.SingleAsync()).Naziv);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(404)]
    public async Task Delete_NedostajeAkcija_VracaNotFound(int? id)
    {
        await using var db = TestContextFactory.Create();

        var rezultat = await new AkcijaController(db).Delete(id);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Delete_PostojecaAkcija_VracaPotvrduBezBrisanja()
    {
        await using var db = TestContextFactory.Create();
        db.Akcija.Add(new Akcija { Id = 3, Proizvod = new Proizvod { Naziv = "Okvir" } });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var rezultat = (ViewResult)await new AkcijaController(db).Delete(3);

        Assert.AreEqual("Okvir", ((Akcija)rezultat.Model!).Proizvod!.Naziv);
        Assert.IsTrue(await db.Akcija.AnyAsync(a => a.Id == 3));
    }

    [TestMethod]
    [DataRow(3, 1)]
    [DataRow(404, 2)]
    public async Task DeleteConfirmed_PostojeciIliNepostojeciId_CuvaOstaleAkcije(int id, int ocekivano)
    {
        await using var db = TestContextFactory.Create();
        db.Akcija.AddRange(new Akcija { Id = 3 }, new Akcija { Id = 4 });
        await db.SaveChangesAsync();

        var rezultat = (RedirectToActionResult)await new AkcijaController(db).DeleteConfirmed(id);

        Assert.AreEqual(nameof(AkcijaController.Index), rezultat.ActionName);
        Assert.AreEqual(ocekivano, await db.Akcija.CountAsync());
        Assert.IsTrue(await db.Akcija.AnyAsync(a => a.Id == 4));
    }

    [TestMethod]
    public async Task Create_GreskaPohrane_PropagiraIzuzetakBezPorukeOUspjehu()
    {
        var opcije = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var mock = new Mock<ApplicationDbContext>(opcije) { CallBase = true };
        var greska = new DbUpdateException("Upis nije uspio.");
        mock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(greska);
        await using var db = mock.Object;
        var controller = Controller(db);

        var stvarna = await Assert.ThrowsExactlyAsync<DbUpdateException>(() =>
            controller.Create(new Akcija { Naziv = "Popust" }));

        Assert.AreSame(greska, stvarna);
        Assert.IsFalse(controller.TempData.ContainsKey("Success"));
    }

    [TestMethod]
    [TestCategory("PoznatiDefekt")]
    [DataRow(-0.01d)]
    [DataRow(100.01d)]
    public void ValidateProperty_DecimalniPostotakIzvanRaspona_OdbijaVrijednost(double postotak)
    {
        var akcija = new Akcija { Naziv = "Popust", PopustPostotak = postotak };
        var kontekst = new ValidationContext(akcija) { MemberName = nameof(Akcija.PopustPostotak) };
        var greske = new List<ValidationResult>();

        var validan = Validator.TryValidateProperty(postotak, kontekst, greske);

        Assert.IsFalse(validan, $"Postotak {postotak} je izvan raspona 0–100.");
    }

    private static AkcijaController Controller(ApplicationDbContext db) => new(db)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
    };

    private static void PrimijeniValidaciju(AkcijaController controller, Akcija akcija)
    {
        var greske = new List<ValidationResult>();
        Validator.TryValidateObject(akcija, new ValidationContext(akcija), greske, true);
        foreach (var greska in greske)
            foreach (var polje in greska.MemberNames)
                controller.ModelState.AddModelError(polje, greska.ErrorMessage!);
    }
}
