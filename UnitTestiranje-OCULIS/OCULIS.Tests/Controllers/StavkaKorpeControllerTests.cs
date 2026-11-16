using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCULIS.Controllers;
using OCULIS.Data;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
public class StavkaKorpeControllerTests
{
    [TestMethod]
    public async Task Index_SpremljenaStavka_UcitavaKorpuIProizvod()
    {
        await using var db = TestContextFactory.Create();
        await PripremiStavku(db);

        var rezultat = (ViewResult)await new StavkaKorpeController(db).Index();

        var stavka = ((List<StavkaKorpe>)rezultat.Model!).Single();
        Assert.AreEqual(3, stavka.Korpa.Id);
        Assert.AreEqual("Okvir", stavka.Proizvod.Naziv);
    }

    [TestMethod]
    public async Task Details_PostojecaStavka_VracaCijenuIKolicinu()
    {
        await using var db = TestContextFactory.Create();
        await PripremiStavku(db);

        var rezultat = (ViewResult)await new StavkaKorpeController(db).Details(5);

        var stavka = (StavkaKorpe)rezultat.Model!;
        Assert.AreEqual(2, stavka.Kolicina);
        Assert.AreEqual(20d, stavka.Cijena);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(404)]
    public async Task Details_NedostajeStavka_VracaNotFound(int? id)
    {
        await using var db = TestContextFactory.Create();

        var rezultat = await new StavkaKorpeController(db).Details(id);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Create_FormaZaStavku_NudiPostojeceKorpeIProizvode()
    {
        await using var db = TestContextFactory.Create();
        await PripremiStavku(db);
        var controller = new StavkaKorpeController(db);

        var rezultat = (ViewResult)controller.Create();

        Assert.IsNull(rezultat.Model);
        Assert.AreEqual("3", ((SelectList)controller.ViewData["IdKorpa"]!).Single().Value);
        Assert.AreEqual("4", ((SelectList)controller.ViewData["IdProizvod"]!).Single().Value);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(100)]
    public async Task Create_GranicnaKolicina_SpremaStavku(int kolicina)
    {
        await using var db = TestContextFactory.Create();
        var controller = new StavkaKorpeController(db);
        var stavka = new StavkaKorpe
        {
            Korpa = new Korpa(), Proizvod = new Proizvod { Naziv = "Okvir" }, Kolicina = kolicina, Cijena = 20
        };
        ValidirajKolicinu(controller, stavka);

        var rezultat = (RedirectToActionResult)await controller.Create(stavka);
        db.ChangeTracker.Clear();

        Assert.AreEqual(nameof(StavkaKorpeController.Index), rezultat.ActionName);
        Assert.AreEqual(kolicina, (await db.StavkaKorpe.SingleAsync()).Kolicina);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(101)]
    public async Task Create_KolicinaIzvanRaspona_VracaUnosBezUpisa(int kolicina)
    {
        await using var db = TestContextFactory.Create();
        var controller = new StavkaKorpeController(db);
        var stavka = new StavkaKorpe { Kolicina = kolicina };
        ValidirajKolicinu(controller, stavka);

        var rezultat = (ViewResult)await controller.Create(stavka);

        Assert.AreSame(stavka, rezultat.Model);
        Assert.IsTrue(controller.ModelState.ContainsKey(nameof(StavkaKorpe.Kolicina)));
        Assert.IsFalse(await db.StavkaKorpe.AnyAsync());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(404)]
    public async Task Edit_NemaStavke_VracaNotFound(int? id)
    {
        await using var db = TestContextFactory.Create();

        var rezultat = await new StavkaKorpeController(db).Edit(id);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Edit_PostojecaStavka_OznacavaPripadajucuKorpuIProizvod()
    {
        await using var db = TestContextFactory.Create();
        await PripremiStavku(db);
        var controller = new StavkaKorpeController(db);

        var rezultat = (ViewResult)await controller.Edit(5);

        Assert.AreEqual(5, ((StavkaKorpe)rezultat.Model!).Id);
        Assert.IsTrue(((SelectList)controller.ViewData["IdKorpa"]!).Single().Selected);
        Assert.IsTrue(((SelectList)controller.ViewData["IdProizvod"]!).Single().Selected);
    }

    [TestMethod]
    public async Task Edit_RazlicitiIdentifikatori_VracaNotFoundBezUpisa()
    {
        await using var db = TestContextFactory.Create();
        await PripremiStavku(db);

        var rezultat = await new StavkaKorpeController(db).Edit(6, NovaVrijednost(3));

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        Assert.AreEqual(2, (await db.StavkaKorpe.SingleAsync()).Kolicina);
    }

    [TestMethod]
    public async Task Edit_NevalidnaKolicina_ZadrzavaUnosBezIzmjene()
    {
        await using var db = TestContextFactory.Create();
        await PripremiStavku(db);
        var controller = new StavkaKorpeController(db);
        var stavka = NovaVrijednost(101);
        ValidirajKolicinu(controller, stavka);

        var rezultat = (ViewResult)await controller.Edit(5, stavka);

        Assert.AreSame(stavka, rezultat.Model);
        Assert.IsTrue(((SelectList)controller.ViewData["IdProizvod"]!).Single().Selected);
        Assert.AreEqual(2, (await db.StavkaKorpe.SingleAsync()).Kolicina);
    }

    [TestMethod]
    public async Task Edit_ValidnaKolicina_SpremaIzmjenu()
    {
        await using var db = TestContextFactory.Create();
        await PripremiStavku(db);

        var rezultat = (RedirectToActionResult)await new StavkaKorpeController(db).Edit(5, NovaVrijednost(3));
        db.ChangeTracker.Clear();

        Assert.AreEqual(nameof(StavkaKorpeController.Index), rezultat.ActionName);
        Assert.AreEqual(3, (await db.StavkaKorpe.SingleAsync()).Kolicina);
    }

    [TestMethod]
    public async Task Edit_StavkaNestalaPrijeUpisa_VracaNotFound()
    {
        await using var db = TestContextFactory.Create();

        var rezultat = await new StavkaKorpeController(db).Edit(5, NovaVrijednost(3));

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Edit_KonkurentnaIzmjenaStavkeKojaPostoji_PropagiraIzuzetak()
    {
        var opcije = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var mock = new Mock<ApplicationDbContext>(opcije) { CallBase = true };
        await using var db = mock.Object;
        await PripremiStavku(db);
        var greska = new DbUpdateConcurrencyException("Drugi zahtjev je izmijenio stavku.");
        mock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(greska);
        var controller = new StavkaKorpeController(db);

        var stvarna = await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(() => controller.Edit(5, NovaVrijednost(3)));
        db.ChangeTracker.Clear();

        Assert.AreSame(greska, stvarna);
        Assert.AreEqual(2, (await db.StavkaKorpe.SingleAsync()).Kolicina);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(404)]
    public async Task Delete_NemaStavke_VracaNotFound(int? id)
    {
        await using var db = TestContextFactory.Create();

        var rezultat = await new StavkaKorpeController(db).Delete(id);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    public async Task Delete_PostojecaStavka_VracaPotvrduBezBrisanja()
    {
        await using var db = TestContextFactory.Create();
        await PripremiStavku(db);

        var rezultat = (ViewResult)await new StavkaKorpeController(db).Delete(5);

        Assert.AreEqual("Okvir", ((StavkaKorpe)rezultat.Model!).Proizvod.Naziv);
        Assert.IsTrue(await db.StavkaKorpe.AnyAsync(s => s.Id == 5));
    }

    [TestMethod]
    [DataRow(5, 0)]
    [DataRow(404, 1)]
    public async Task DeleteConfirmed_PostojeciIliNepostojeciId_CuvaKorpuIProizvod(int id, int ocekivano)
    {
        await using var db = TestContextFactory.Create();
        await PripremiStavku(db);

        var rezultat = (RedirectToActionResult)await new StavkaKorpeController(db).DeleteConfirmed(id);

        Assert.AreEqual(nameof(StavkaKorpeController.Index), rezultat.ActionName);
        Assert.AreEqual(ocekivano, await db.StavkaKorpe.CountAsync());
        Assert.IsTrue(await db.Korpa.AnyAsync(k => k.Id == 3));
        Assert.IsTrue(await db.Proizvod.AnyAsync(p => p.Id == 4));
    }

    private static async Task PripremiStavku(ApplicationDbContext db)
    {
        db.StavkaKorpe.Add(new StavkaKorpe
        {
            Id = 5, Kolicina = 2, Cijena = 20,
            Korpa = new Korpa { Id = 3 }, Proizvod = new Proizvod { Id = 4, Naziv = "Okvir" }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static StavkaKorpe NovaVrijednost(int kolicina) =>
        new() { Id = 5, IdKorpa = 3, IdProizvod = 4, Kolicina = kolicina, Cijena = 20 };

    private static void ValidirajKolicinu(StavkaKorpeController controller, StavkaKorpe stavka)
    {
        var kontekst = new ValidationContext(stavka) { MemberName = nameof(StavkaKorpe.Kolicina) };
        var greske = new List<ValidationResult>();
        Validator.TryValidateProperty(stavka.Kolicina, kontekst, greske);
        foreach (var greska in greske)
            controller.ModelState.AddModelError(nameof(StavkaKorpe.Kolicina), greska.ErrorMessage!);
    }
}
