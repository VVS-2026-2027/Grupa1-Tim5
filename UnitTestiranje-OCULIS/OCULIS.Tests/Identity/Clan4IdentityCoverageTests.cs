using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moq;
using OCULIS.Areas.Identity.Pages.Account;
using OCULIS.Models;

namespace OCULIS.Tests.Identity;

/// <summary>
/// Dodatni scenariji za povećanje pokrivenosti triju Identity stranica člana 4.
/// Ovaj fajl se dodaje pored postojećeg Clan4IdentityPagesTests.cs.
/// </summary>
[TestClass]
public class Clan4IdentityCoverageTests
{
    [TestMethod]
    public async Task Register_Get_PostavljaReturnUrlIPrijaveVanjskeProvajdere()
    {
        var users = Clan4TestContext.CreateUserManager();
        users.SetupGet(x => x.SupportsUserEmail).Returns(true);
        var store = new Mock<IUserEmailStore<Korisnik>>();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        var schemes = new[] { new AuthenticationScheme("Test", "Test", typeof(IAuthenticationHandler)) };
        signIn.Setup(x => x.GetExternalAuthenticationSchemesAsync()).ReturnsAsync(schemes);
        var page = Clan4TestContext.Page(new RegisterModel(users.Object, store.Object, signIn.Object,
            Mock.Of<ILogger<RegisterModel>>(), Mock.Of<IEmailSender>()));

        await page.OnGetAsync("/nastavi");

        Assert.AreEqual("/nastavi", page.ReturnUrl);
        Assert.AreEqual("Test", page.ExternalLogins.Single().Name);
    }

    [TestMethod]
    public async Task Register_PostUspjesanUzPotvrduEmaila_VracaRegisterConfirmation()
    {
        var (page, users, store, signIn, emailSender) = CreateRegisterPage(requireConfirmedAccount: true);
        page.Input = ValidRegistration();
        users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), "Lozinka123!")).ReturnsAsync(IdentityResult.Success);
        users.Setup(x => x.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        users.Setup(x => x.GetUserIdAsync(It.IsAny<Korisnik>())).ReturnsAsync("new-user");
        users.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<Korisnik>())).ReturnsAsync("token+/=");
        emailSender.Setup(x => x.SendEmailAsync("ajna@example.test", It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await page.OnPostAsync("/pocetna");

        var redirect = (RedirectToPageResult)result;
        Assert.AreEqual("RegisterConfirmation", redirect.PageName);
        users.Verify(x => x.CreateAsync(It.Is<Korisnik>(u => u.Ime == "Ajna" && u.Prezime == "Kovacevic"), "Lozinka123!"), Times.Once);
        store.Verify(x => x.SetUserNameAsync(It.IsAny<Korisnik>(), "ajna@example.test", It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.SetEmailAsync(It.IsAny<Korisnik>(), "ajna@example.test", It.IsAny<CancellationToken>()), Times.Once);
        users.Verify(x => x.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Once);
        emailSender.Verify(x => x.SendEmailAsync("ajna@example.test", It.IsAny<string>(), It.Is<string>(body => body.Contains("https://example.test/confirm"))), Times.Once);
        signIn.Verify(x => x.SignInAsync(It.IsAny<Korisnik>(), false, null), Times.Never);
    }

    [TestMethod]
    public async Task Register_PostUspjesanBezPotvrdeEmaila_PrimaPrijavuILokalnoPreusmjerava()
    {
        var (page, users, _, signIn, emailSender) = CreateRegisterPage(requireConfirmedAccount: false);
        page.Input = ValidRegistration();
        users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        users.Setup(x => x.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        users.Setup(x => x.GetUserIdAsync(It.IsAny<Korisnik>())).ReturnsAsync("new-user");
        users.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<Korisnik>())).ReturnsAsync("token");
        emailSender.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        signIn.Setup(x => x.SignInAsync(It.IsAny<Korisnik>(), false, null)).Returns(Task.CompletedTask);

        var result = await page.OnPostAsync("/dashboard");

        Assert.AreEqual("/dashboard", ((LocalRedirectResult)result).Url);
        signIn.Verify(x => x.SignInAsync(It.IsAny<Korisnik>(), false, null), Times.Once);
    }

    [TestMethod]
    public async Task Register_PostNeuspjesnoKreiranje_DodajeIdentityGreskuIVracaPage()
    {
        var (page, users, _, _, _) = CreateRegisterPage(requireConfirmedAccount: true);
        page.Input = ValidRegistration();
        users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Lozinka nije prihvaćena." }));

        var result = await page.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.IsTrue(page.ModelState[string.Empty]!.Errors.Any(e => e.ErrorMessage == "Lozinka nije prihvaćena."));
        users.Verify(x => x.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void Register_StoreBezEmailPodrske_BacaNotSupportedException()
    {
        var users = Clan4TestContext.CreateUserManager();
        users.SetupGet(x => x.SupportsUserEmail).Returns(false);
        var store = new Mock<IUserStore<Korisnik>>();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);

        Assert.ThrowsExactly<NotSupportedException>(() => new RegisterModel(users.Object, store.Object, signIn.Object,
            Mock.Of<ILogger<RegisterModel>>(), Mock.Of<IEmailSender>()));
    }

    [TestMethod]
    public async Task LoginWith2fa_GetPostavljaRememberMeIReturnUrl()
    {
        var user = new Korisnik { Id = "u1" };
        var users = Clan4TestContext.CreateUserManager();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        var page = Clan4TestContext.Page(new LoginWith2faModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWith2faModel>>()));

        var result = await page.OnGetAsync(true, "/profile");

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.IsTrue(page.RememberMe);
        Assert.AreEqual("/profile", page.ReturnUrl);
    }

    [TestMethod]
    public async Task LoginWith2fa_PostNeispravanModel_VracaPageBezIdentityPoziva()
    {
        var users = Clan4TestContext.CreateUserManager();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        var page = Clan4TestContext.Page(new LoginWith2faModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWith2faModel>>()));
        page.ModelState.AddModelError("Input.TwoFactorCode", "required");

        Assert.IsInstanceOfType<PageResult>(await page.OnPostAsync(false));
        signIn.Verify(x => x.GetTwoFactorAuthenticationUserAsync(), Times.Never);
    }

    [TestMethod]
    public async Task LoginWith2fa_PostZakljucanKorisnik_VracaLockoutStranicu()
    {
        var user = new Korisnik { Id = "u1" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserIdAsync(user)).ReturnsAsync("u1");
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signIn.Setup(x => x.TwoFactorAuthenticatorSignInAsync("123456", false, false)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var page = Clan4TestContext.Page(new LoginWith2faModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWith2faModel>>()));
        page.Input = new LoginWith2faModel.InputModel { TwoFactorCode = "123456" };

        var result = await page.OnPostAsync(false);

        Assert.AreEqual("./Lockout", ((RedirectToPageResult)result).PageName);
    }

    [TestMethod]
    public async Task LoginWith2fa_PostBezPrivremenogKorisnika_BacaInvalidOperationException()
    {
        var users = Clan4TestContext.CreateUserManager();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((Korisnik?)null);
        var page = Clan4TestContext.Page(new LoginWith2faModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWith2faModel>>()));
        page.Input = new LoginWith2faModel.InputModel { TwoFactorCode = "123456" };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => page.OnPostAsync(false));
    }

    [TestMethod]
    public async Task LoginWithRecoveryCode_GetPostavljaReturnUrl()
    {
        var users = Clan4TestContext.CreateUserManager();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(new Korisnik { Id = "u1" });
        var page = Clan4TestContext.Page(new LoginWithRecoveryCodeModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>()));

        var result = await page.OnGetAsync("/sigurno");

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.AreEqual("/sigurno", page.ReturnUrl);
    }

    [TestMethod]
    public async Task LoginWithRecoveryCode_GetBezPrivremenogKorisnika_BacaInvalidOperationException()
    {
        var users = Clan4TestContext.CreateUserManager();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((Korisnik?)null);
        var page = Clan4TestContext.Page(new LoginWithRecoveryCodeModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>()));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => page.OnGetAsync());
    }

    [TestMethod]
    public async Task LoginWithRecoveryCode_PostBezPrivremenogKorisnika_BacaInvalidOperationException()
    {
        var users = Clan4TestContext.CreateUserManager();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync((Korisnik?)null);
        var page = Clan4TestContext.Page(new LoginWithRecoveryCodeModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>()));
        page.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "ABCD1234" };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => page.OnPostAsync());
    }

    [TestMethod]
    public async Task LoginWithRecoveryCode_PostZakljucanKorisnik_VracaLockoutStranicu()
    {
        var user = new Korisnik { Id = "u1" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserIdAsync(user)).ReturnsAsync("u1");
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signIn.Setup(x => x.TwoFactorRecoveryCodeSignInAsync("ABCD1234")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);
        var page = Clan4TestContext.Page(new LoginWithRecoveryCodeModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>()));
        page.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "ABCD 1234" };

        var result = await page.OnPostAsync();

        Assert.AreEqual("./Lockout", ((RedirectToPageResult)result).PageName);
    }

    [TestMethod]
    public async Task LoginWithRecoveryCode_PostPogresanKod_DodajeModelError()
    {
        var user = new Korisnik { Id = "u1" };
        var users = Clan4TestContext.CreateUserManager();
        users.Setup(x => x.GetUserIdAsync(user)).ReturnsAsync("u1");
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetTwoFactorAuthenticationUserAsync()).ReturnsAsync(user);
        signIn.Setup(x => x.TwoFactorRecoveryCodeSignInAsync("BADCODE")).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        var page = Clan4TestContext.Page(new LoginWithRecoveryCodeModel(signIn.Object, users.Object, Mock.Of<ILogger<LoginWithRecoveryCodeModel>>()));
        page.Input = new LoginWithRecoveryCodeModel.InputModel { RecoveryCode = "BADCODE" };

        Assert.IsInstanceOfType<PageResult>(await page.OnPostAsync());
        Assert.IsTrue(page.ModelState[string.Empty]!.Errors.Any(e => e.ErrorMessage.Contains("Invalid recovery code")));
    }

    private static RegisterModel.InputModel ValidRegistration() => new()
    {
        Email = "ajna@example.test",
        Ime = "Ajna",
        Prezime = "Kovacevic",
        Password = "Lozinka123!",
        ConfirmPassword = "Lozinka123!"
    };

    private static (RegisterModel Page, Mock<UserManager<Korisnik>> Users, Mock<IUserEmailStore<Korisnik>> Store,
        Mock<SignInManager<Korisnik>> SignIn, Mock<IEmailSender> EmailSender) CreateRegisterPage(bool requireConfirmedAccount)
    {
        var users = Clan4TestContext.CreateUserManager();
        users.SetupGet(x => x.SupportsUserEmail).Returns(true);
        users.Object.Options.SignIn.RequireConfirmedAccount = requireConfirmedAccount;
        var store = new Mock<IUserEmailStore<Korisnik>>();
        var signIn = Clan4TestContext.CreateSignInManager(users.Object);
        signIn.Setup(x => x.GetExternalAuthenticationSchemesAsync()).ReturnsAsync(Array.Empty<AuthenticationScheme>());
        store.Setup(x => x.SetUserNameAsync(It.IsAny<Korisnik>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        store.Setup(x => x.SetEmailAsync(It.IsAny<Korisnik>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var emailSender = new Mock<IEmailSender>();
        var page = Clan4TestContext.Page(new RegisterModel(users.Object, store.Object, signIn.Object,
            Mock.Of<ILogger<RegisterModel>>(), emailSender.Object));
        return (page, users, store, signIn, emailSender);
    }
}







