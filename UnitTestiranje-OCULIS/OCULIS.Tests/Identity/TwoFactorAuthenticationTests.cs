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
public class TwoFactorAuthenticationTests
{
    [TestMethod]
    public async Task Handleri_NepostojeciKorisnik_VracajuNotFound()
    {
        var page = Clan3TestContext.Page(new TwoFactorAuthenticationModel(
            Clan3TestContext.Users().Object, null!, NullLogger<TwoFactorAuthenticationModel>.Instance));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGetAsync());
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnPostAsync());
    }

    [TestMethod]
    [DataRow(null, false, 0)]
    [DataRow("TESTKEY", true, 4)]
    public async Task Get_PrikazujePostavke_PostZaboravljaPreglednik(string? key, bool enabled, int codes)
    {
        var user = new Korisnik();
        var users = Clan3TestContext.Users(user);
        users.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync(key);
        users.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(enabled);
        users.Setup(m => m.CountRecoveryCodesAsync(user)).ReturnsAsync(codes);
        var signIn = new Mock<SignInManager<Korisnik>>(users.Object,
            Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<Korisnik>>(), null!, null!, null!, null!);
        signIn.Setup(m => m.IsTwoFactorClientRememberedAsync(user)).ReturnsAsync(enabled);
        var page = Clan3TestContext.Page(new TwoFactorAuthenticationModel(users.Object, signIn.Object,
            NullLogger<TwoFactorAuthenticationModel>.Instance));

        Assert.IsInstanceOfType<PageResult>(await page.OnGetAsync());
        Assert.AreEqual(key != null, page.HasAuthenticator);
        Assert.AreEqual(enabled, page.Is2faEnabled);
        Assert.AreEqual(enabled, page.IsMachineRemembered);
        Assert.AreEqual(codes, page.RecoveryCodesLeft);
        Assert.IsInstanceOfType<RedirectToPageResult>(await page.OnPostAsync());
        signIn.Verify(m => m.ForgetTwoFactorClientAsync(), Times.Once);
        Assert.IsFalse(string.IsNullOrEmpty(page.StatusMessage));
    }
}
