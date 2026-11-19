using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using OCULIS.Areas.Identity.Pages.Account;
using OCULIS.Areas.Identity.Pages.Account.Manage;
using OCULIS.Models;

namespace OCULIS.Tests.Identity;

[TestClass]
public class Clan4IdentityPagesTests
{
    [TestMethod]
    public async Task Register_PostNeispravanModel_VracaPageBezKreiranjaKorisnika()
    {
        var users = Clan4TestContext.CreateUserManager();
        users.SetupGet(x => x.SupportsUserEmail).Returns(true);
        var store = new Mock<IUserEmailStore<Korisnik>>();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetExternalAuthenticationSchemesAsync()).ReturnsAsync(Array.Empty<AuthenticationScheme>());
        var page = Clan4TestContext.Page(new RegisterModel(users.Object, store.Object, signIn.Object,
            Mock.Of<ILogger<RegisterModel>>(), Mock.Of<IEmailSender>()));
        page.ModelState.AddModelError("Input.Email", "invalid");

        var result = await page.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(result);
        users.Verify(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task LoginWith2fa_PostUspjesanKodCistiRazmakeICrticeIPreusmjerava()
    {
        var users = Clan4TestContext.CreateUserManager();
        var user = new Korisnik { Id = "u1" };
        users.Setup(x => x.GetUserIdAsync(user)).ReturnsAsync("u1");
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signIn.Setup(x => x.TwoFactorAuthenticatorSignInAsync("123456", true, true)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var page = Clan4TestContext.Page(new LoginWith2faModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWith2faModel>>()));
        page.Input = new LoginWith2faModel.InputModel { TwoFactorCode = "123- 456", RememberMachine = true };

        var result = await page.OnPostAsync(true, "/profile");

        Assert.AreEqual("/profile", ((LocalRedirectResult)result).Url);
        signIn.Verify(x => x.TwoFactorAuthenticatorSignInAsync("123456", true, true), Times.Once);
    }

    [TestMethod]
    public async Task LoginWith2fa_PogresanKodDodajeModelError()
    {
        var user = new Korisnik { Id = "u1" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserIdAsync(user)).ReturnsAsync("u1");
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signIn.Setup(x => x.TwoFactorAuthenticatorSignInAsync("000000", false, false)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        var page = Clan4TestContext.Page(new LoginWith2faModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWith2faModel>>()));
        page.Input = new LoginWith2faModel.InputModel { TwoFactorCode = "000000" };
        Assert.IsInstanceOfType<PageResult>(await page.OnPostAsync(false));
        Assert.IsTrue(page.ModelState[string.Empty]!.Errors.Any(e => e.ErrorMessage.Contains("Invalid authenticator code")));
    }

    [TestMethod]
    public async Task LoginWith2fa_NemaPrivremenogKorisnika_BacaInvalidOperationException()
    {
        var users = Clan4TestContext.CreateUserManager();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((Korisnik?)null);
        var page = Clan4TestContext.Page(new LoginWith2faModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWith2faModel>>()));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => page.OnGetAsync(false));
    }

    [TestMethod]
    public async Task LoginWithRecoveryCode_UspjesanKodUklanjaRazmake()
    {
        var user = new Korisnik { Id = "u1" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserIdAsync(user)).ReturnsAsync("u1");
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signIn.Setup(x => x.TwoFactorRecoveryCodeSignInAsync("ABCD1234")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var page = Clan4TestContext.Page(new LoginWithRecoveryCodeModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>()));
        page.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "ABCD 1234" };
        var result = await page.OnPostAsync("/secure");
        Assert.AreEqual("/secure", ((LocalRedirectResult)result).Url);
        signIn.Verify(x => x.TwoFactorRecoveryCodeSignInAsync("ABCD1234"), Times.Once);
    }

    [TestMethod]
    public async Task LoginWithRecoveryCode_PrazanKod_VracaPageBezPozivaIdentity()
    {
        var users = Clan4TestContext.CreateUserManager();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        var page = Clan4TestContext.Page(new LoginWithRecoveryCodeModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>()));
        page.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "" };
        page.ModelState.AddModelError("Input.RecoveryCode", "required");
        Assert.IsInstanceOfType<PageResult>(await page.OnPostAsync());
        signIn.Verify(x => x.GetTwoFactorAuthenticationUserAsync(), Times.Never);
    }

    [TestMethod]
    public async Task RegisterConfirmation_NullEmail_PreusmjeravaNaIndex()
    {
        var page = Clan4TestContext.Page(new RegisterConfirmationModel(Clan4TestContext.CreateUserManager().Object, Mock.Of<IEmailSender>()));
        var result = await page.OnGetAsync(null!);
        Assert.AreEqual("/Index", ((RedirectToPageResult)result).PageName);
    }

    [TestMethod]
    public async Task RegisterConfirmation_NepostojeciEmail_VracaNotFound()
    {
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByEmailAsync("x@example.test")).ReturnsAsync((Korisnik?)null);
        var page = Clan4TestContext.Page(new RegisterConfirmationModel(users.Object, Mock.Of<IEmailSender>()));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGetAsync("x@example.test"));
    }

    [TestMethod]
    public async Task RegisterConfirmation_PostojeciKorisnikPripremaLinkZaPotvrdu()
    {
        var user = new Korisnik { Id = "u1", Email = "u@example.test" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
        users.Setup(x => x.GetUserIdAsync(user)).ReturnsAsync("u1");
        users.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("token");
        var page = Clan4TestContext.Page(new RegisterConfirmationModel(users.Object, Mock.Of<IEmailSender>()));
        Assert.IsInstanceOfType<PageResult>(await page.OnGetAsync(user.Email));
        Assert.AreEqual(user.Email, page.Email);
        Assert.IsTrue(page.DisplayConfirmAccountLink);
        Assert.AreEqual("https://example.test/confirm", page.EmailConfirmationUrl);
    }

    [TestMethod]
    public async Task ResetAuthenticator_KorisnikNePostoji_VracaNotFound()
    {
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((Korisnik?)null);
        users.Setup(x => x.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns("missing");
        var page = Clan4TestContext.Page(new ResetAuthenticatorModel(users.Object,
            Clan4TestContext.CreateSignInManager(users.Object).Object, Mock.Of<ILogger<ResetAuthenticatorModel>>()));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGet());
    }

    [TestMethod]
    public async Task ResetAuthenticator_Post_ResetujeDvofaktorskuPrijavuIPreusmjerava()
    {
        var user = new Korisnik { Id = "u1" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
        users.Setup(x => x.SetTwoFactorEnabledAsync(user, false)).ReturnsAsync(IdentityResult.Success);
        users.Setup(x => x.ResetAuthenticatorKeyAsync(user)).ReturnsAsync(IdentityResult.Success);
        users.Setup(x => x.GetUserIdAsync(user)).ReturnsAsync("u1");
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.RefreshSignInAsync(user)).Returns(Task.CompletedTask);
        var page = Clan4TestContext.Page(new ResetAuthenticatorModel(users.Object, signIn.Object, Mock.Of<ILogger<ResetAuthenticatorModel>>()));
        Assert.AreEqual("./EnableAuthenticator", ((RedirectToPageResult)await page.OnPostAsync()).PageName);
        Assert.IsTrue(page.StatusMessage.Contains("resetovan"));
        users.Verify(x => x.ResetAuthenticatorKeyAsync(user), Times.Once);
    }

    [TestMethod]
    public async Task Logout_PostSaReturnUrl_KoristiLokalniRedirect()
    {
        var users = Clan4TestContext.CreateUserManager();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);
        var page = Clan4TestContext.Page(new LogoutModel(signIn.Object, Mock.Of<ILogger<LogoutModel>>()));
        Assert.AreEqual("/", ((LocalRedirectResult)await page.OnPost("/")).Url);
        signIn.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [TestMethod]
    public async Task Logout_PostBezReturnUrl_VracaRedirectNaIstuStranicu()
    {
        var signIn = Clan4TestContext.CreateSignInManager(Clan4TestContext.CreateUserManager().Object);
        signIn.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);
        var page = Clan4TestContext.Page(new LogoutModel(signIn.Object, Mock.Of<ILogger<LogoutModel>>()));
        Assert.IsInstanceOfType<RedirectToPageResult>(await page.OnPost());
    }

    [TestMethod]
    public void Lockout_OnGet_ZavrsavaBezRezultata()
    {
        var page = Clan4TestContext.Page(new LockoutModel());
        page.OnGet();
        Assert.IsNotNull(page);
    }
}
