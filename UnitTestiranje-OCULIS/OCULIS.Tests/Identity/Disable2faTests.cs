using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCULIS.Areas.Identity.Pages.Account.Manage;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class Disable2faTests
{
    private Korisnik korisnik = null!;
    private Mock<UserManager<Korisnik>> korisnici = null!;
    private Disable2faModel stranica = null!;

    [TestInitialize]
    public void Priprema()
    {
        korisnik = new Korisnik { Id = "korisnik-1" };
        korisnici = Clan5TestContext.Korisnici(korisnik);
        stranica = Clan5TestContext.Stranica(new Disable2faModel(
            korisnici.Object, NullLogger<Disable2faModel>.Instance));
    }

    [TestMethod]
    public async Task OnGet_NepostojeciKorisnik_VracaNotFound()
    {
        korisnici.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((Korisnik?)null);

        var rezultat = await stranica.OnGet();

        Assert.IsInstanceOfType<NotFoundObjectResult>(rezultat);
    }

    [TestMethod]
    public async Task OnPost_NepostojeciKorisnik_VracaNotFound()
    {
        korisnici.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((Korisnik?)null);

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<NotFoundObjectResult>(rezultat);
        korisnici.Verify(m => m.SetTwoFactorEnabledAsync(
            It.IsAny<Korisnik>(), false), Times.Never);
    }

    [TestMethod]
    public async Task OnGet_2faNijeUkljucena_BacaIzuzetak()
    {
        korisnici.Setup(m => m.GetTwoFactorEnabledAsync(korisnik)).ReturnsAsync(false);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => stranica.OnGet());
    }

    [TestMethod]
    public async Task OnGet_2faJeUkljucena_PrikazujeStranicu()
    {
        korisnici.Setup(m => m.GetTwoFactorEnabledAsync(korisnik)).ReturnsAsync(true);

        var rezultat = await stranica.OnGet();

        Assert.IsInstanceOfType<PageResult>(rezultat);
    }

    [TestMethod]
    public async Task OnPost_UspjesnoIskljucivanje_PreusmjeravaIPostavljaPoruku()
    {
        korisnici.Setup(m => m.SetTwoFactorEnabledAsync(korisnik, false))
            .ReturnsAsync(IdentityResult.Success);

        var rezultat = (RedirectToPageResult)await stranica.OnPostAsync();

        Assert.AreEqual("./TwoFactorAuthentication", rezultat.PageName);
        StringAssert.Contains(stranica.StatusMessage, "2FA je onemogućen.");
        korisnici.Verify(m => m.SetTwoFactorEnabledAsync(korisnik, false), Times.Once);
    }

    [TestMethod]
    public async Task OnPost_NeuspjeloIskljucivanje_BacaIzuzetak()
    {
        korisnici.Setup(m => m.SetTwoFactorEnabledAsync(korisnik, false))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError()));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => stranica.OnPostAsync());
    }
}
