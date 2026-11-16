using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using OCULIS.Controllers;
using OCULIS.Models;
using OCULIS.Models.ViewModels;

namespace OCULIS.Tests;

[TestClass]
public class ProizvodControllerTests
{
    [TestMethod]
    [DataRow("cijena_asc", 1, 2)]
    [DataRow("cijena_desc", 2, 1)]
    [DataRow("naziv", 2, 1)]
    [DataRow("", 2, 1)]
    public async Task Index_KombinovaniFilteri_VracaSamoPodudaranjaUTrazenomPoretku(
        string sortiranje, int prvi, int drugi)
    {
        await using var db = TestContextFactory.Create();
        db.Proizvod.AddRange(
            new Proizvod { Id = 1, Naziv = "Zeta", Opis = "Okvir", Cijena = 20, Proizvodjac = "Optika", Kategorija = KategorijaProizvoda.DioptrijskeNaocale },
            new Proizvod { Id = 2, Naziv = "Alfa okvir", Cijena = 40, Proizvodjac = "Optika", Kategorija = KategorijaProizvoda.DioptrijskeNaocale },
            new Proizvod { Id = 3, Naziv = "Preskup okvir", Cijena = 41, Proizvodjac = "Optika", Kategorija = KategorijaProizvoda.DioptrijskeNaocale },
            new Proizvod { Id = 4, Naziv = "Druga kategorija okvir", Cijena = 30, Proizvodjac = "Optika", Kategorija = KategorijaProizvoda.SuncaneNaocale },
            new Proizvod { Id = 5, Naziv = "Drugi proizvodjac okvir", Cijena = 30, Proizvodjac = "Drugi", Kategorija = KategorijaProizvoda.DioptrijskeNaocale },
            new Proizvod { Id = 6, Naziv = "Bez podudaranja", Cijena = 30, Proizvodjac = "Optika", Kategorija = KategorijaProizvoda.DioptrijskeNaocale });
        await db.SaveChangesAsync();
        var filter = new ProizvodPretragaViewModel
        {
            Pretraga = "OKVIR", MinCijena = 20, MaxCijena = 40, Proizvodjac = "Opt",
            Kategorija = KategorijaProizvoda.DioptrijskeNaocale, Sortiranje = sortiranje
        };

        var rezultat = (ViewResult)await new ProizvodController(db).Index(filter);

        Assert.AreSame(filter, rezultat.Model);
        CollectionAssert.AreEqual(new[] { prvi, drugi }, filter.Proizvodi.Select(p => p.Id).ToArray());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(99)]
    public async Task Details_NedostajeProizvod_VracaNotFound(int? id)
    {
        await using var db = TestContextFactory.Create();

        var rezultat = await new ProizvodController(db).Details(id);

        Assert.IsInstanceOfType<NotFoundResult>(rezultat);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Create_ValidnostModela_UpisujeSamoValidanProizvod(bool validan)
    {
        await using var db = TestContextFactory.Create();
        var controller = new ProizvodController(db)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
        var proizvod = new Proizvod { Naziv = "Okvir", Cijena = 40 };
        if (!validan)
            controller.ModelState.AddModelError(nameof(Proizvod.Naziv), "Naziv je obavezan.");

        var rezultat = await controller.Create(proizvod);

        if (validan)
        {
            Assert.AreEqual(nameof(ProizvodController.Index), ((RedirectToActionResult)rezultat).ActionName);
            Assert.AreEqual(proizvod.Id, (await db.Proizvod.SingleAsync()).Id);
        }
        else
        {
            Assert.AreSame(proizvod, ((ViewResult)rezultat).Model);
            Assert.IsFalse(await db.Proizvod.AnyAsync());
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Edit_PodudaranjeIdentifikatora_MijenjaSamoTrazeniProizvod(bool podudaraSe)
    {
        await using var db = TestContextFactory.Create();
        db.Proizvod.Add(new Proizvod { Id = 7, Naziv = "Okvir", Cijena = 40 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var controller = new ProizvodController(db)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };

        var rezultat = await controller.Edit(podudaraSe ? 7 : 8, new Proizvod { Id = 7, Naziv = "Okvir", Cijena = 30 });

        if (podudaraSe)
            Assert.AreEqual(nameof(ProizvodController.Index), ((RedirectToActionResult)rezultat).ActionName);
        else
            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        db.ChangeTracker.Clear();
        Assert.AreEqual(podudaraSe ? 30d : 40d, (await db.Proizvod.SingleAsync()).Cijena);
    }

    [TestMethod]
    [DataRow(7, 8)]
    [DataRow(99, 7)]
    public async Task DeleteConfirmed_SelektivnoBrisanje_CuvaDrugiProizvod(int zaBrisanje, int ostaje)
    {
        await using var db = TestContextFactory.Create();
        db.Proizvod.AddRange(new Proizvod { Id = 7, Naziv = "Okvir" }, new Proizvod { Id = 8, Naziv = "Staklo" });
        await db.SaveChangesAsync();
        var controller = new ProizvodController(db)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };

        var rezultat = await controller.DeleteConfirmed(zaBrisanje);

        Assert.AreEqual(nameof(ProizvodController.Index), ((RedirectToActionResult)rezultat).ActionName);
        Assert.IsTrue(await db.Proizvod.AnyAsync(p => p.Id == ostaje));
        Assert.AreEqual(zaBrisanje == 99 ? 2 : 1, await db.Proizvod.CountAsync());
    }
}
