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
    [DataRow(99.99, 7.0, 106.99)]
    [DataRow(100.0, 0.0, 100.0)]
    public async Task Checkout_IznosNakonPopusta_PripremaDostavuIUkupno(
        double iznosNakonPopusta,
        double ocekivanaDostava,
        double ocekivanoUkupno)
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
                Cijena = 125
            },
            Kolicina = 1,
            Cijena = 125
        });

        await db.SaveChangesAsync();

        var korisnici = Clan5TestContext.Korisnici(kupac);
        var popust = new Mock<IPopustKalkulatorService>();

        popust.Setup(p => p.IzracunajPopustAsync(kupac.Id, korpa.Id))
            .ReturnsAsync(new PopustRezultatViewModel
            {
                OsnovnaCijena = 125,
                UkupnaCijena = iznosNakonPopusta
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
        Assert.IsNotNull(rezultat.ViewData["CijenaDostave"]);
        Assert.IsNotNull(rezultat.ViewData["UkupnoSaDostavom"]);

        Assert.AreEqual(
            ocekivanaDostava,
            (double)rezultat.ViewData["CijenaDostave"]!,
            0.001);

        Assert.AreEqual(
            ocekivanoUkupno,
            (double)rezultat.ViewData["UkupnoSaDostavom"]!,
            0.001);
    }
}