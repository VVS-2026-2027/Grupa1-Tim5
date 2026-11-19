using System.ComponentModel.DataAnnotations;
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
public class ResetPasswordTests
{
    private Mock<UserManager<Korisnik>> korisnici = null!;
    private ResetPasswordModel stranica = null!;
    private Korisnik korisnik = null!;

    [TestInitialize]
    public void Priprema()
    {
        korisnik = new Korisnik { Id = "korisnik-1", Email = "amila@example.com" };
        korisnici = Clan5TestContext.Korisnici(korisnik);
        korisnici.Setup(m => m.FindByEmailAsync("amila@example.com"))
            .ReturnsAsync(korisnik);
        stranica = Clan5TestContext.Stranica(new ResetPasswordModel(korisnici.Object));
        stranica.Input = new ResetPasswordModel.InputModel
        {
            Email = korisnik.Email,
            Password = "Nova123!",
            ConfirmPassword = "Nova123!",
            Code = "test-token"
        };
    }

    [TestMethod]
    public void OnGet_BezKoda_VracaBadRequest()
    {
        var rezultat = stranica.OnGet();

        Assert.IsInstanceOfType<BadRequestObjectResult>(rezultat);
    }

    [TestMethod]
    public void OnGet_IspravnoKodiranToken_DekodiraToken()
    {
        var kod = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("test-token"));

        var rezultat = stranica.OnGet(kod);

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("test-token", stranica.Input.Code);
    }

    [TestMethod]
    public void OnGet_NeispravnoKodiranToken_BacaFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() => stranica.OnGet("%%%"));
    }

    [TestMethod]
    public async Task OnPost_NeispravanModel_NeMijenjaLozinku()
    {
        stranica.ModelState.AddModelError("Input.Password", "Lozinka je obavezna.");

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(rezultat);
        korisnici.Verify(m => m.ResetPasswordAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OnPost_NepostojeciKorisnik_NeOtkrivaPostojanjeRacuna()
    {
        korisnici.Setup(m => m.FindByEmailAsync("amila@example.com"))
            .ReturnsAsync((Korisnik?)null);

        var rezultat = (RedirectToPageResult)await stranica.OnPostAsync();

        Assert.AreEqual("./ResetPasswordConfirmation", rezultat.PageName);
        korisnici.Verify(m => m.ResetPasswordAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OnPost_UspjesnaPromjena_PreusmjeravaNaPotvrdu()
    {
        korisnici.Setup(m => m.ResetPasswordAsync(korisnik, "test-token", "Nova123!"))
            .ReturnsAsync(IdentityResult.Success);

        var rezultat = (RedirectToPageResult)await stranica.OnPostAsync();

        Assert.AreEqual("./ResetPasswordConfirmation", rezultat.PageName);
        korisnici.Verify(m => m.ResetPasswordAsync(
            korisnik, "test-token", "Nova123!"), Times.Once);
    }

    [TestMethod]
    public async Task OnPost_NevazeciToken_VracaPorukuGreske()
    {
        korisnici.Setup(m => m.ResetPasswordAsync(
                korisnik, "test-token", "Nova123!"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Token nije ispravan." }));

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("Token nije ispravan.",
            stranica.ModelState[string.Empty]!.Errors[0].ErrorMessage);
    }

    [TestMethod]
    [DataRow(5, false)]
    [DataRow(6, true)]
    [DataRow(100, true)]
    [DataRow(101, false)]
    public void Input_DuzinaLozinke_ProvjeravaGranice(int duzina, bool ocekivano)
    {
        stranica.Input.Password = new string('a', duzina);
        stranica.Input.ConfirmPassword = stranica.Input.Password;

        var rezultat = Validator.TryValidateObject(stranica.Input,
            new ValidationContext(stranica.Input), new List<ValidationResult>(), true);

        Assert.AreEqual(ocekivano, rezultat);
    }

    [TestMethod]
    public void Input_RazliciteLozinke_NeProlaziValidaciju()
    {
        stranica.Input.ConfirmPassword = "Druga123!";

        var rezultat = Validator.TryValidateObject(stranica.Input,
            new ValidationContext(stranica.Input), new List<ValidationResult>(), true);

        Assert.IsFalse(rezultat);
    }
}
