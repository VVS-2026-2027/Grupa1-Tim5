using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCULIS.Areas.Identity.Pages.Account;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class ExternalLoginTests
{
    private Mock<IUserEmailStore<Korisnik>> spremiste = null!;
    private Mock<UserManager<Korisnik>> korisnici = null!;
    private Mock<SignInManager<Korisnik>> prijava = null!;
    private Mock<IEmailSender> email = null!;
    private ExternalLoginInfo informacije = null!;
    private ExternalLoginModel stranica = null!;

    [TestInitialize]
    public void Priprema()
    {
        spremiste = new Mock<IUserEmailStore<Korisnik>>();
        korisnici = Clan5TestContext.Korisnici(spremiste: spremiste.Object);
        prijava = Clan5TestContext.Prijava(korisnici);
        email = new Mock<IEmailSender>();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Amila"),
            new Claim(ClaimTypes.Email, "amila@example.com")
        }, "Google"));
        informacije = new ExternalLoginInfo(principal, "Google", "google-1", "Google");

        prijava.Setup(m => m.GetExternalLoginInfoAsync(null)).ReturnsAsync(informacije);
        korisnici.Setup(m => m.CreateAsync(It.IsAny<Korisnik>()))
            .ReturnsAsync(IdentityResult.Success);
        korisnici.Setup(m => m.AddLoginAsync(It.IsAny<Korisnik>(), informacije))
            .ReturnsAsync(IdentityResult.Success);
        korisnici.Setup(m => m.GetUserIdAsync(It.IsAny<Korisnik>()))
            .ReturnsAsync("korisnik-1");
        korisnici.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<Korisnik>()))
            .ReturnsAsync("test-token");

        stranica = Clan5TestContext.Stranica(new ExternalLoginModel(
            prijava.Object, korisnici.Object, spremiste.Object,
            NullLogger<ExternalLoginModel>.Instance, email.Object));
        stranica.Input = new ExternalLoginModel.InputModel { Email = "amila@example.com" };
    }

    [TestMethod]
    public void OnGet_Poziv_PreusmjeravaNaPrijavu()
    {
        var rezultat = (RedirectToPageResult)stranica.OnGet();

        Assert.AreEqual("./Login", rezultat.PageName);
    }

    [TestMethod]
    public void OnPost_Google_PokreceVanjskuPrijavu()
    {
        var postavke = new AuthenticationProperties();
        prijava.Setup(m => m.ConfigureExternalAuthenticationProperties(
                "Google", It.IsAny<string>(), null))
            .Returns(postavke);

        var rezultat = (ChallengeResult)stranica.OnPost("Google", "/pocetna");

        CollectionAssert.AreEqual(new[] { "Google" }, rezultat.AuthenticationSchemes.ToArray());
        Assert.AreSame(postavke, rezultat.Properties);
    }

    [TestMethod]
    public async Task Callback_GreskaProvidera_VracaNaPrijavu()
    {
        var rezultat = (RedirectToPageResult)await stranica.OnGetCallbackAsync(
            remoteError: "Pristup odbijen");

        Assert.AreEqual("./Login", rezultat.PageName);
        StringAssert.Contains(stranica.ErrorMessage, "Pristup odbijen");
        prijava.Verify(m => m.GetExternalLoginInfoAsync(null), Times.Never);
    }

    [TestMethod]
    public async Task Callback_NemaPodataka_VracaNaPrijavu()
    {
        prijava.Setup(m => m.GetExternalLoginInfoAsync(null))
            .ReturnsAsync((ExternalLoginInfo?)null);

        var rezultat = (RedirectToPageResult)await stranica.OnGetCallbackAsync();

        Assert.AreEqual("./Login", rezultat.PageName);
        Assert.AreEqual("Error loading external login information.", stranica.ErrorMessage);
    }

    [TestMethod]
    public async Task Callback_PostojeciKorisnik_PrijavljujeKorisnika()
    {
        prijava.Setup(m => m.ExternalLoginSignInAsync("Google", "google-1", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var rezultat = (LocalRedirectResult)await stranica.OnGetCallbackAsync("/pocetna");

        Assert.AreEqual("/pocetna", rezultat.Url);
        prijava.Verify(m => m.ExternalLoginSignInAsync(
            "Google", "google-1", false, true), Times.Once);
    }

    [TestMethod]
    public async Task Callback_ZakljucanRacun_PreusmjeravaNaLockout()
    {
        prijava.Setup(m => m.ExternalLoginSignInAsync("Google", "google-1", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var rezultat = (RedirectToPageResult)await stranica.OnGetCallbackAsync();

        Assert.AreEqual("./Lockout", rezultat.PageName);
    }

    [TestMethod]
    public async Task Callback_NoviKorisnik_PripremaEmailZaRegistraciju()
    {
        prijava.Setup(m => m.ExternalLoginSignInAsync("Google", "google-1", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var rezultat = await stranica.OnGetCallbackAsync("/pocetna");

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("amila@example.com", stranica.Input.Email);
        Assert.AreEqual("Google", stranica.ProviderDisplayName);
        Assert.AreEqual("/pocetna", stranica.ReturnUrl);
    }

    [TestMethod]
    public async Task Confirmation_NemaPodataka_VracaNaPrijavu()
    {
        prijava.Setup(m => m.GetExternalLoginInfoAsync(null))
            .ReturnsAsync((ExternalLoginInfo?)null);

        var rezultat = (RedirectToPageResult)await stranica.OnPostConfirmationAsync();

        Assert.AreEqual("./Login", rezultat.PageName);
        Assert.AreEqual("Error loading external login information during confirmation.",
            stranica.ErrorMessage);
    }

    [TestMethod]
    public async Task Confirmation_NeispravanModel_NeKreiraKorisnika()
    {
        stranica.ModelState.AddModelError("Input.Email", "Neispravan email.");

        var rezultat = await stranica.OnPostConfirmationAsync("/pocetna");

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("Google", stranica.ProviderDisplayName);
        Assert.AreEqual("/pocetna", stranica.ReturnUrl);
        korisnici.Verify(m => m.CreateAsync(It.IsAny<Korisnik>()), Times.Never);
    }

    [TestMethod]
    public async Task Confirmation_KreiranjeNeUspije_PrikazujeGresku()
    {
        korisnici.Setup(m => m.CreateAsync(It.IsAny<Korisnik>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Kreiranje nije uspjelo." }));

        var rezultat = await stranica.OnPostConfirmationAsync();

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("Kreiranje nije uspjelo.",
            stranica.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        korisnici.Verify(m => m.AddLoginAsync(
            It.IsAny<Korisnik>(), It.IsAny<UserLoginInfo>()), Times.Never);
    }

    [TestMethod]
    public async Task Confirmation_PovezivanjeNeUspije_NeSaljeEmail()
    {
        korisnici.Setup(m => m.AddLoginAsync(It.IsAny<Korisnik>(), informacije))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Povezivanje nije uspjelo." }));

        var rezultat = await stranica.OnPostConfirmationAsync();

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("Povezivanje nije uspjelo.",
            stranica.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        email.Verify(m => m.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Confirmation_UspjesnoKreiranje_SaljeEmailIPrijavljuje()
    {
        var rezultat = (LocalRedirectResult)await stranica.OnPostConfirmationAsync("/pocetna");

        Assert.AreEqual("/pocetna", rezultat.Url);
        spremiste.Verify(m => m.SetUserNameAsync(
            It.IsAny<Korisnik>(), "amila@example.com", CancellationToken.None), Times.Once);
        spremiste.Verify(m => m.SetEmailAsync(
            It.IsAny<Korisnik>(), "amila@example.com", CancellationToken.None), Times.Once);
        email.Verify(m => m.SendEmailAsync("amila@example.com", "Confirm your email",
            It.Is<string>(tekst => tekst.Contains("&amp;userId=korisnik-1"))), Times.Once);
        prijava.Verify(m => m.SignInAsync(
            It.IsAny<Korisnik>(), false, "Google"), Times.Once);
    }

    [TestMethod]
    public async Task Confirmation_ObaveznaPotvrdaEmaila_NePrijavljujeOdmah()
    {
        korisnici.Object.Options.SignIn.RequireConfirmedAccount = true;

        var rezultat = (RedirectToPageResult)await stranica.OnPostConfirmationAsync();

        Assert.AreEqual("./RegisterConfirmation", rezultat.PageName);
        Assert.AreEqual("amila@example.com", rezultat.RouteValues!["Email"]);
        email.Verify(m => m.SendEmailAsync("amila@example.com", "Confirm your email",
            It.IsAny<string>()), Times.Once);
        prijava.Verify(m => m.SignInAsync(
            It.IsAny<Korisnik>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }
}
