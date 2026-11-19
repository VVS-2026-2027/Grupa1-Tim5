using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OCULIS.Controllers;
using OCULIS.Models;
using OCULIS.Models.ViewModels;
using OCULIS.Services.Popust;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Task3Clan5")]
public class CheckoutDostavaTests
{
    [TestMethod]
    public async Task Checkout_IznosNakonPopustaIspod100_UkljucujeDostavu()
    {
        // Arrange - priprema
        await using var db = TestContextFactory.Create();

        var kupac = new Korisnik { Id = "kupac-5" };
        var korpa = new Korpa { Korisnik = kupac };

        db.StavkaKorpe.Add(new StavkaKorpe
        {
            Korpa = korpa,
            Proizvod = new Proizvod
            {
                Naziv = "Okvir",
                Cijena = 111.10
            },
            Kolicina = 1,
            Cijena = 111.10
        });

        await db.SaveChangesAsync();

        var korisnici = Clan5TestContext.Korisnici(kupac);
        var popust = new Mock<IPopustKalkulatorService>();

        popust.Setup(p => p.IzracunajPopustAsync(kupac.Id, korpa.Id))
            .ReturnsAsync(new PopustRezultatViewModel
            {
                OsnovnaCijena = 111.10,
                PopustPostotak = 10,
                PopustIznos = 11.11,
                UkupnaCijena = 99.99
            });

        var kontroler = new KorpaController(
            db, korisnici.Object, popust.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act - poziv metode
        var rezultat = (ViewResult)await kontroler.Checkout();

        // Assert - provjera sažetka
        Assert.IsNotNull(
            rezultat.ViewData["CijenaDostave"],
            "Checkout treba pripremiti iznos dostave.");

        Assert.AreEqual(
            7d,
            (double)rezultat.ViewData["CijenaDostave"]!);

        Assert.IsNotNull(rezultat.ViewData["UkupnoSaDostavom"]);

        Assert.AreEqual(
            106.99d,
            (double)rezultat.ViewData["UkupnoSaDostavom"]!,
            0.001);
    }
}