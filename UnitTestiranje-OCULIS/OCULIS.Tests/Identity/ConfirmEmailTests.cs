using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using OCULIS.Areas.Identity.Pages.Account;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class ConfirmEmailTests
{
    [TestMethod]
    [DataRow(null, "token")]
    [DataRow("korisnik-1", null)]
    public async Task OnGet_NedostajeParametar_PreusmjeravaNaPocetnu(
        string? id, string? kod)
    {
        var stranica = Clan5TestContext.Stranica(
            new ConfirmEmailModel(Clan5TestContext.Korisnici().Object));

        var rezultat = (RedirectToPageResult)await stranica.OnGetAsync(id, kod);

        Assert.AreEqual("/Index", rezultat.PageName);
    }

    [TestMethod]
    public async Task OnGet_NepostojeciKorisnik_VracaNotFound()
    {
        var korisnici = Clan5TestContext.Korisnici();
        korisnici.Setup(m => m.FindByIdAsync("nepoznat"))
            .ReturnsAsync((Korisnik?)null);
        var stranica = Clan5TestContext.Stranica(new ConfirmEmailModel(korisnici.Object));

        var rezultat = await stranica.OnGetAsync("nepoznat", "token");

        Assert.IsInstanceOfType<NotFoundObjectResult>(rezultat);
        korisnici.Verify(m => m.ConfirmEmailAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OnGet_IspravanToken_PotvrdjujeEmail()
    {
        var korisnik = new Korisnik { Id = "korisnik-1" };
        var korisnici = Clan5TestContext.Korisnici(korisnik);
        korisnici.Setup(m => m.FindByIdAsync(korisnik.Id)).ReturnsAsync(korisnik);
        korisnici.Setup(m => m.ConfirmEmailAsync(korisnik, "test-token"))
            .ReturnsAsync(IdentityResult.Success);
        var stranica = Clan5TestContext.Stranica(new ConfirmEmailModel(korisnici.Object));
        var kod = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("test-token"));

        var rezultat = await stranica.OnGetAsync(korisnik.Id, kod);

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("Thank you for confirming your email.", stranica.StatusMessage);
        korisnici.Verify(m => m.ConfirmEmailAsync(korisnik, "test-token"), Times.Once);
    }

    [TestMethod]
    public async Task OnGet_NevazeciToken_PrikazujeGresku()
    {
        var korisnik = new Korisnik { Id = "korisnik-1" };
        var korisnici = Clan5TestContext.Korisnici(korisnik);
        korisnici.Setup(m => m.FindByIdAsync(korisnik.Id)).ReturnsAsync(korisnik);
        korisnici.Setup(m => m.ConfirmEmailAsync(korisnik, "los-token"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError()));
        var stranica = Clan5TestContext.Stranica(new ConfirmEmailModel(korisnici.Object));
        var kod = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("los-token"));

        var rezultat = await stranica.OnGetAsync(korisnik.Id, kod);

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("Error confirming your email.", stranica.StatusMessage);
    }
}
