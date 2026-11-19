using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using OCULIS.Areas.Identity.Pages.Account.Manage;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan5")]
public class ManageIndexTests
{
    private Korisnik korisnik = null!;
    private Mock<UserManager<Korisnik>> korisnici = null!;
    private Mock<SignInManager<Korisnik>> prijava = null!;
    private IndexModel stranica = null!;

    [TestInitialize]
    public void Priprema()
    {
        korisnik = new Korisnik { Id = "korisnik-1", UserName = "amila" };
        korisnici = Clan5TestContext.Korisnici(korisnik);
        korisnici.Setup(m => m.GetUserNameAsync(korisnik)).ReturnsAsync("amila");
        korisnici.Setup(m => m.GetPhoneNumberAsync(korisnik)).ReturnsAsync("061123456");
        prijava = Clan5TestContext.Prijava(korisnici);
        stranica = Clan5TestContext.Stranica(new IndexModel(
            korisnici.Object, prijava.Object));
        stranica.Input = new IndexModel.InputModel { PhoneNumber = "061654321" };
    }

    [TestMethod]
    public async Task OnGet_PostojeciKorisnik_UcitavaProfil()
    {
        var rezultat = await stranica.OnGetAsync();

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("amila", stranica.Username);
        Assert.AreEqual("061123456", stranica.Input.PhoneNumber);
    }

    [TestMethod]
    public async Task OnGet_NepostojeciKorisnik_VracaNotFound()
    {
        korisnici.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((Korisnik?)null);

        var rezultat = await stranica.OnGetAsync();

        Assert.IsInstanceOfType<NotFoundObjectResult>(rezultat);
    }

    [TestMethod]
    public async Task OnPost_NepostojeciKorisnik_VracaNotFound()
    {
        korisnici.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync((Korisnik?)null);

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<NotFoundObjectResult>(rezultat);
    }

    [TestMethod]
    public async Task OnPost_NeispravanModel_VracaProfilBezIzmjene()
    {
        stranica.ModelState.AddModelError("Input.PhoneNumber", "Neispravan broj.");

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(rezultat);
        Assert.AreEqual("061123456", stranica.Input.PhoneNumber);
        korisnici.Verify(m => m.SetPhoneNumberAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task OnPost_NoviBroj_AzuriraProfil()
    {
        korisnici.Setup(m => m.SetPhoneNumberAsync(korisnik, "061654321"))
            .ReturnsAsync(IdentityResult.Success);

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<RedirectToPageResult>(rezultat);
        Assert.AreEqual("Vaš profil je ažuriran.", stranica.StatusMessage);
        korisnici.Verify(m => m.SetPhoneNumberAsync(korisnik, "061654321"), Times.Once);
        prijava.Verify(m => m.RefreshSignInAsync(korisnik), Times.Once);
    }

    [TestMethod]
    public async Task OnPost_IstiBroj_NeUpisujePonovoBroj()
    {
        stranica.Input.PhoneNumber = "061123456";

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<RedirectToPageResult>(rezultat);
        korisnici.Verify(m => m.SetPhoneNumberAsync(
            It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
        prijava.Verify(m => m.RefreshSignInAsync(korisnik), Times.Once);
    }

    [TestMethod]
    public async Task OnPost_NeuspjeloSpremanje_PrikazujeGresku()
    {
        korisnici.Setup(m => m.SetPhoneNumberAsync(korisnik, "061654321"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError()));

        var rezultat = await stranica.OnPostAsync();

        Assert.IsInstanceOfType<RedirectToPageResult>(rezultat);
        Assert.AreEqual("Greška: Neočekivana greška pri postavljanju broja telefona.",
            stranica.StatusMessage);
        prijava.Verify(m => m.RefreshSignInAsync(korisnik), Times.Never);
    }

    [TestMethod]
    [DataRow("061123456", true)]
    [DataRow("123", false)]
    [DataRow("", false)]
    public void Input_BrojTelefona_ProvjeravaFormat(string broj, bool ocekivano)
    {
        var unos = new IndexModel.InputModel { PhoneNumber = broj };

        var rezultat = Validator.TryValidateObject(
            unos, new ValidationContext(unos), new List<ValidationResult>(), true);

        Assert.AreEqual(ocekivano, rezultat);
    }
}
