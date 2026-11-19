using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCULIS.Areas.Identity.Pages.Account;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class LoginTests
{
    private Mock<SignInManager<Korisnik>> prijava = null!;
    private LoginModel stranica = null!;

    [TestInitialize]
    public void Priprema()
    {
        prijava = Clan5TestContext.Prijava(Clan5TestContext.Korisnici());
        stranica = Clan5TestContext.Stranica(new LoginModel(
            prijava.Object, NullLogger<LoginModel>.Instance));
        stranica.Input = new LoginModel.InputModel
        {
            Email = "amila@example.com",
            Password = "Test123!",
            RememberMe = true
        };
    }

    [TestMethod]
    public async Task OnGet_GreskaPrijave_PripremaStranicuIPoruku()
    {
        stranica.ErrorMessage = "Prijava nije uspjela.";

        await stranica.OnGetAsync();

        Assert.AreEqual("/", stranica.ReturnUrl);
        Assert.IsNotNull(stranica.ExternalLogins);
        Assert.AreEqual("Prijava nije uspjela.",
            stranica.ModelState[string.Empty]!.Errors[0].ErrorMessage);
    }

    [TestMethod]
    public async Task OnPost_IspravniPodaci_PrijavljujeKorisnika()
    {
        prijava.Setup(m => m.PasswordSignInAsync(
                "amila@example.com", "Test123!", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var rezultat = (LocalRedirectResult)await stranica.OnPostAsync("/pocetna");

        Assert.AreEqual("/pocetna", rezultat.Url);
        prijava.Verify(m => m.PasswordSignInAsync(
            "amila@example.com", "Test123!", true, false), Times.Once);
    }

    [TestMethod]
    public async Task OnPost_Potrebna2fa_PreusmjeravaNaPotvrdu()
    {
        prijava.Setup(m => m.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.TwoFactorRequired);

        var rezultat = (RedirectToPageResult)await stranica.OnPostAsync("/pocetna");

        Assert.AreEqual("./LoginWith2fa", rezultat.PageName);
        Assert.AreEqual("/pocetna", rezultat.RouteValues!["ReturnUrl"]);
        Assert.AreEqual(true, rezultat.RouteValues["RememberMe"]);
    }

    [TestMethod]
    public async Task OnPost_ZakljucanRacun_PreusmjeravaNaLockout()
    {
        prijava.Setup(m => m.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var rezultat = (RedirectToPageResult)await stranica.OnPostAsync();

        Assert.AreEqual("./Lockout", rezultat.PageName);
    }

    [TestMethod]
    public async Task OnPost_PogresnaLozinka_VracaFormuSGreskom()
    {
        prijava.Setup(m => m.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("Neuspješan pokušaj prijave.",
            stranica.ModelState[string.Empty]!.Errors[0].ErrorMessage);
    }

    [TestMethod]
    public async Task OnPost_NeispravanModel_NePozivaPrijavu()
    {
        stranica.ModelState.AddModelError("Input.Email", "Neispravan email.");

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(rezultat);
        prijava.Verify(m => m.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    [DataRow("amila@example.com", "Test123!", true)]
    [DataRow("pogresna-adresa", "Test123!", false)]
    [DataRow("amila@example.com", "", false)]
    public void Input_EmailILozinka_ProvjeravaObaveznaPolja(
        string email, string lozinka, bool ocekivano)
    {
        var unos = new LoginModel.InputModel { Email = email, Password = lozinka };

        var rezultat = Validator.TryValidateObject(
            unos, new ValidationContext(unos), new List<ValidationResult>(), true);

        Assert.AreEqual(ocekivano, rezultat);
    }
}
