using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCULIS.Models;
using OCULIS.Areas.Identity.Pages.Account.Manage;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class EnableAuthenticatorTests
{
    [TestMethod]
    public async Task Handleri_NepostojeciKorisnik_VracajuNotFound()
    {
        var page = Create(Clan3TestContext.Users());
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGetAsync());
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnPostAsync());
    }

    [TestMethod]
    public async Task OnGet_NedostajeKljuc_GeneriseKljucIPripremaUri()
    {
        var user = new Korisnik();
        var users = Clan3TestContext.Users(user);
        users.SetupSequence(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync((string?)null).ReturnsAsync("ABCDEFGH");
        users.Setup(m => m.GetEmailAsync(user)).ReturnsAsync("ana@example.test");
        var page = Create(users);

        Assert.IsInstanceOfType<PageResult>(await page.OnGetAsync());
        users.Verify(m => m.ResetAuthenticatorKeyAsync(user), Times.Once);
        Assert.AreEqual("abcd efgh", page.SharedKey);
        StringAssert.Contains(page.AuthenticatorUri, "ana@example.test");
        StringAssert.Contains(page.AuthenticatorUri, "secret=ABCDEFGH");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task OnPost_NeispravanKod_Ostavlja2faIskljucenu(bool validanModel)
    {
        var user = new Korisnik();
        var users = Clan3TestContext.Users(user);
        users.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("ABCD");
        users.Setup(m => m.GetEmailAsync(user)).ReturnsAsync("ana@example.test");
        var page = Create(users);
        if (!validanModel)
            page.ModelState.AddModelError("Input.Code", "Kod je obavezan");

        Assert.IsInstanceOfType<PageResult>(await page.OnPostAsync());
        Assert.IsFalse(page.ModelState.IsValid);
        Assert.AreEqual("abcd", page.SharedKey);
        users.Verify(m => m.VerifyTwoFactorTokenAsync(user, It.IsAny<string>(), "123456"),
            validanModel ? Times.Once() : Times.Never());
        users.Verify(m => m.SetTwoFactorEnabledAsync(user, true), Times.Never);
    }

    [TestMethod]
    [DataRow(0, "123-456", "./ShowRecoveryCodes")]
    [DataRow(3, "123 456", "./TwoFactorAuthentication")]
    public async Task OnPost_IspravanKod_Ukljucuje2faIGeneriseKodovePoPotrebi(int broj, string unos, string odrediste)
    {
        var user = new Korisnik();
        var users = Clan3TestContext.Users(user);
        var provider = users.Object.Options.Tokens.AuthenticatorTokenProvider;
        users.Setup(m => m.VerifyTwoFactorTokenAsync(user, provider, "123456")).ReturnsAsync(true);
        users.Setup(m => m.CountRecoveryCodesAsync(user)).ReturnsAsync(broj);
        var kodovi = Enumerable.Range(1, 10).Select(i => $"test-{i}").ToArray();
        users.Setup(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ReturnsAsync(kodovi);
        var page = Create(users);
        page.Input.Code = unos;

        var result = (RedirectToPageResult)await page.OnPostAsync();
        Assert.AreEqual(odrediste, result.PageName);
        users.Verify(m => m.SetTwoFactorEnabledAsync(user, true), Times.Once);
        users.Verify(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10), broj == 0 ? Times.Once() : Times.Never());
        if (broj == 0)
            CollectionAssert.AreEqual(kodovi, page.RecoveryCodes);
    }

    private static EnableAuthenticatorModel Create(Mock<UserManager<Korisnik>> users)
    {
        return Clan3TestContext.Page(new EnableAuthenticatorModel(users.Object,
            NullLogger<EnableAuthenticatorModel>.Instance, System.Text.Encodings.Web.UrlEncoder.Default)
        {
            Input = new EnableAuthenticatorModel.InputModel { Code = "123-456" }
        });
    }
}
