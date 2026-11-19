using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OCULIS.Areas.Identity.Pages.Account;
using OCULIS.Areas.Identity.Pages.Account.Manage;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan2")]
public class Clan2IdentityPagesTests
{
    [TestMethod]
    public async Task PersonalData_KorisnikPostoji_VracaStranicu()
    {
        var user = new Korisnik { Id = "user" };
        var manager = Clan3TestContext.Users(user);
        var page = Clan3TestContext.Page(new PersonalDataModel(manager.Object, NullLogger<PersonalDataModel>.Instance));

        Assert.IsInstanceOfType<PageResult>(await page.OnGet());
    }

    [TestMethod]
    public async Task PersonalData_KorisnikNedostaje_NotFound()
    {
        var manager = Clan3TestContext.Users();
        var page = Clan3TestContext.Page(new PersonalDataModel(manager.Object, NullLogger<PersonalDataModel>.Instance));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGet());
    }

    [TestMethod]
    public async Task DownloadPersonalData_PostVracaJSONiAttachmentHeader()
    {
        var user = new Korisnik { Id = "user", Email = "user@example.test", PhoneNumber = "061123456" };
        var manager = Clan3TestContext.Users(user);
        manager.Setup(m => m.GetLoginsAsync(user)).ReturnsAsync(new List<UserLoginInfo> { new("Provider", "provider-key", "Provider") });
        manager.Setup(m => m.GetAuthenticatorKeyAsync(user)).ReturnsAsync("auth-key");
        var page = Clan3TestContext.Page(new DownloadPersonalDataModel(manager.Object, NullLogger<DownloadPersonalDataModel>.Instance));

        var result = (FileContentResult)await page.OnPostAsync();

        Assert.AreEqual("application/json", result.ContentType);
        Assert.AreEqual("attachment; filename=PersonalData.json", page.Response.Headers["Content-Disposition"].ToString());
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(result.FileContents)!;
        Assert.AreEqual("provider-key", data["Provider external login provider key"]);
        Assert.AreEqual("auth-key", data["Authenticator Key"]);
    }

    [TestMethod]
    public async Task DownloadPersonalData_NedostajeKorisnik_NotFound()
    {
        var manager = Clan3TestContext.Users();
        var page = Clan3TestContext.Page(new DownloadPersonalDataModel(manager.Object, NullLogger<DownloadPersonalDataModel>.Instance));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnPostAsync());
    }

    [TestMethod]
    public async Task GenerateRecoveryCodes_OnGetBezDvofaktorskeZastite_BacaIzuzetak()
    {
        var user = new Korisnik { Id = "user" };
        var manager = Clan3TestContext.Users(user);
        manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
        var page = Clan3TestContext.Page(new GenerateRecoveryCodesModel(manager.Object, NullLogger<GenerateRecoveryCodesModel>.Instance));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => page.OnGetAsync());
    }

    [TestMethod]
    public async Task GenerateRecoveryCodes_KorisnikNedostaje_NotFound()
    {
        var manager = Clan3TestContext.Users();
        var page = Clan3TestContext.Page(new GenerateRecoveryCodesModel(manager.Object, NullLogger<GenerateRecoveryCodesModel>.Instance));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGetAsync());
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnPostAsync());
    }

    [TestMethod]
    public async Task GenerateRecoveryCodes_OnPost_GeneriseDesetKodova()
    {
        var user = new Korisnik { Id = "user" };
        var manager = Clan3TestContext.Users(user);
        manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
        manager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id);
        manager.Setup(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)).ReturnsAsync(new[] { "code-1", "code-2" });
        var page = Clan3TestContext.Page(new GenerateRecoveryCodesModel(manager.Object, NullLogger<GenerateRecoveryCodesModel>.Instance));

        var result = (RedirectToPageResult)await page.OnPostAsync();

        Assert.AreEqual("./ShowRecoveryCodes", result.PageName);
        CollectionAssert.AreEqual(new[] { "code-1", "code-2" }, page.RecoveryCodes);
        manager.Verify(m => m.GenerateNewTwoFactorRecoveryCodesAsync(user, 10), Times.Once);
    }

    [TestMethod]
    public async Task GenerateRecoveryCodes_OnPostBezDvofaktorskeZastite_BacaIzuzetak()
    {
        var user = new Korisnik { Id = "user" };
        var manager = Clan3TestContext.Users(user);
        manager.Setup(m => m.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
        var page = Clan3TestContext.Page(new GenerateRecoveryCodesModel(manager.Object, NullLogger<GenerateRecoveryCodesModel>.Instance));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => page.OnPostAsync());
    }

    [TestMethod]
    public async Task SetPassword_NepoznatKorisnik_NotFound()
    {
        var manager = Clan3TestContext.Users();
        var page = Clan3TestContext.Page(new SetPasswordModel(manager.Object, null!));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGetAsync());
    }

    [TestMethod]
    public async Task SetPassword_OnGetKorisnikVecImaLozinku_PreusmjeriNaPromjenu()
    {
        var user = new Korisnik { Id = "user" };
        var users = Clan3TestContext.Users(user);
        users.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(true);
        var page = Clan3TestContext.Page(new SetPasswordModel(users.Object, SignInManager(users.Object).Object));
        Assert.AreEqual("./ChangePassword", ((RedirectToPageResult)await page.OnGetAsync()).PageName);
    }

    [TestMethod]
    public async Task SetPassword_OnGetBezLozinkePrikazujeFormu()
    {
        var user = new Korisnik { Id = "user" };
        var users = Clan3TestContext.Users(user);
        users.Setup(m => m.HasPasswordAsync(user)).ReturnsAsync(false);
        var page = Clan3TestContext.Page(new SetPasswordModel(users.Object, SignInManager(users.Object).Object));
        Assert.IsInstanceOfType<PageResult>(await page.OnGetAsync());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SetPassword_OnPostObradaIdentityResult(bool succeeded)
    {
        var user = new Korisnik { Id = "user" };
        var users = Clan3TestContext.Users(user);
        var result = succeeded ? IdentityResult.Success : IdentityResult.Failed(new IdentityError { Description = "Rejected" });
        users.Setup(m => m.AddPasswordAsync(user, "new-password")).ReturnsAsync(result);
        var signIn = SignInManager(users.Object);
        signIn.Setup(m => m.RefreshSignInAsync(user)).Returns(Task.CompletedTask);
        var page = Clan3TestContext.Page(new SetPasswordModel(users.Object, signIn.Object)
        {
            Input = new SetPasswordModel.InputModel { NewPassword = "new-password", ConfirmPassword = "new-password" }
        });

        var response = await page.OnPostAsync();

        if (succeeded)
        {
            Assert.IsInstanceOfType<RedirectToPageResult>(response);
            signIn.Verify(m => m.RefreshSignInAsync(user), Times.Once);
        }
        else
        {
            Assert.IsInstanceOfType<PageResult>(response);
            Assert.IsTrue(page.ModelState.ErrorCount > 0);
            signIn.Verify(m => m.RefreshSignInAsync(user), Times.Never);
        }
    }

    [TestMethod]
    public async Task SetPassword_OnPostNeispravanModel_NeZoveIdentityServis()
    {
        var users = Clan3TestContext.Users(new Korisnik { Id = "user" });
        var page = Clan3TestContext.Page(new SetPasswordModel(users.Object, SignInManager(users.Object).Object));
        page.ModelState.AddModelError("Input.NewPassword", "Neispravna lozinka");
        Assert.IsInstanceOfType<PageResult>(await page.OnPostAsync());
        users.Verify(m => m.AddPasswordAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ExternalLogins_NepoznatKorisnik_NotFound()
    {
        var manager = Clan3TestContext.Users();
        var page = Clan3TestContext.Page(new ExternalLoginsModel(manager.Object, null!, Mock.Of<IUserStore<Korisnik>>()));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGetAsync());
    }

    [TestMethod]
    public async Task ExternalLogins_OnGetPostavljaDostupneProviderEneIPrikazujeUklanjanje()
    {
        var user = new Korisnik { Id = "user" };
        var users = Clan3TestContext.Users(user);
        var store = new Mock<IUserStore<Korisnik>>();
        var passwordStore = store.As<IUserPasswordStore<Korisnik>>();
        passwordStore.Setup(s => s.GetPasswordHashAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync("hashed");
        users.Setup(m => m.GetLoginsAsync(user)).ReturnsAsync(new List<UserLoginInfo> { new("ProviderA", "key", "Provider A") });
        var signIn = SignInManager(users.Object);
        signIn.Setup(m => m.GetExternalAuthenticationSchemesAsync()).ReturnsAsync(new[]
        {
            new AuthenticationScheme("ProviderA", "Provider A", typeof(IAuthenticationHandler)),
            new AuthenticationScheme("ProviderB", "Provider B", typeof(IAuthenticationHandler))
        });
        var page = Clan3TestContext.Page(new ExternalLoginsModel(users.Object, signIn.Object, store.Object));

        Assert.IsInstanceOfType<PageResult>(await page.OnGetAsync());
        Assert.IsTrue(page.ShowRemoveButton);
        Assert.AreEqual(1, page.CurrentLogins.Count);
        Assert.AreEqual("ProviderB", page.OtherLogins.Single().Name);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ExternalLogins_RemoveLoginPrikazujeOdgovarajucuPoruku(bool succeeded)
    {
        var user = new Korisnik { Id = "user" };
        var users = Clan3TestContext.Users(user);
        users.Setup(m => m.RemoveLoginAsync(user, "ProviderA", "key"))
            .ReturnsAsync(succeeded ? IdentityResult.Success : IdentityResult.Failed());
        var signIn = SignInManager(users.Object);
        signIn.Setup(m => m.RefreshSignInAsync(user)).Returns(Task.CompletedTask);
        var page = Clan3TestContext.Page(new ExternalLoginsModel(users.Object, signIn.Object, Mock.Of<IUserStore<Korisnik>>()));

        Assert.IsInstanceOfType<RedirectToPageResult>(await page.OnPostRemoveLoginAsync("ProviderA", "key"));
        StringAssert.Contains(page.StatusMessage, succeeded ? "uklonjen" : "nije uklonjen");
        signIn.Verify(m => m.RefreshSignInAsync(user), succeeded ? Times.Once : Times.Never);
    }

    [TestMethod]
    public async Task ExternalLogins_RemoveLoginNepoznatKorisnik_NotFound()
    {
        var users = Clan3TestContext.Users();
        var page = Clan3TestContext.Page(new ExternalLoginsModel(users.Object, null!, Mock.Of<IUserStore<Korisnik>>()));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnPostRemoveLoginAsync("ProviderA", "key"));
    }

    [TestMethod]
    public async Task ExternalLogins_LinkLogin_CleansCookieAndChallengesProvider()
    {
        var users = Clan3TestContext.Users(new Korisnik { Id = "user" });
        var signIn = SignInManager(users.Object);
        signIn.Setup(m => m.ConfigureExternalAuthenticationProperties("ProviderB", "https://example.test/confirmation?a=1&b=2", null))
            .Returns(new AuthenticationProperties());
        var page = Clan3TestContext.Page(new ExternalLoginsModel(users.Object, signIn.Object, Mock.Of<IUserStore<Korisnik>>()));
        page.PageContext.ActionDescriptor.RouteValues["page"] = "/Account/Manage/ExternalLogins";
        page.PageContext.RouteData.Values["page"] = "/Account/Manage/ExternalLogins";
        var authentication = new Mock<IAuthenticationService>(MockBehavior.Strict);
        authentication.Setup(a => a.SignOutAsync(page.HttpContext, IdentityConstants.ExternalScheme, null)).Returns(Task.CompletedTask);
        page.HttpContext.RequestServices = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();

        var result = (ChallengeResult)await page.OnPostLinkLoginAsync("ProviderB");

        Assert.AreEqual("ProviderB", result.AuthenticationSchemes.Single());
        authentication.VerifyAll();
        signIn.Verify(m => m.ConfigureExternalAuthenticationProperties("ProviderB", "https://example.test/confirmation?a=1&b=2", null), Times.Once);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task ExternalLogins_CallbackUpravljaPovezivanjem(bool succeeded)
    {
        var user = new Korisnik { Id = "user" };
        var users = Clan3TestContext.Users(user);
        users.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "provider-user")], "external"));
        var info = new ExternalLoginInfo(principal, "ProviderA", "provider-user", "Provider A");
        var signIn = SignInManager(users.Object);
        signIn.Setup(m => m.GetExternalLoginInfoAsync(user.Id)).ReturnsAsync(info);
        users.Setup(m => m.AddLoginAsync(user, info)).ReturnsAsync(succeeded ? IdentityResult.Success : IdentityResult.Failed());
        var page = Clan3TestContext.Page(new ExternalLoginsModel(users.Object, signIn.Object, Mock.Of<IUserStore<Korisnik>>()));
        var authentication = new Mock<IAuthenticationService>(MockBehavior.Strict);
        authentication.Setup(a => a.SignOutAsync(page.HttpContext, IdentityConstants.ExternalScheme, null)).Returns(Task.CompletedTask);
        page.HttpContext.RequestServices = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider();

        Assert.IsInstanceOfType<RedirectToPageResult>(await page.OnGetLinkLoginCallbackAsync());
        StringAssert.Contains(page.StatusMessage, succeeded ? "dodan" : "nije dodan");
        if (succeeded) authentication.VerifyAll();
        else authentication.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task ExternalLogins_CallbackNepoznatKorisnik_NotFound()
    {
        var users = Clan3TestContext.Users();
        var page = Clan3TestContext.Page(new ExternalLoginsModel(users.Object, null!, Mock.Of<IUserStore<Korisnik>>()));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await page.OnGetLinkLoginCallbackAsync());
    }

    [TestMethod]
    public async Task ExternalLogins_CallbackBezPodatakaZaPrijavu_BacaIzuzetak()
    {
        var user = new Korisnik { Id = "user" };
        var users = Clan3TestContext.Users(user);
        users.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id);
        var signIn = SignInManager(users.Object);
        signIn.Setup(m => m.GetExternalLoginInfoAsync(user.Id)).ReturnsAsync((ExternalLoginInfo)null!);
        var page = Clan3TestContext.Page(new ExternalLoginsModel(users.Object, signIn.Object, Mock.Of<IUserStore<Korisnik>>()));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => page.OnGetLinkLoginCallbackAsync());
    }

    [TestMethod]
    public void ForgotPasswordConfirmation_OnGet_NeBacaIzuzetak()
    {
        var page = Clan3TestContext.Page(new ForgotPasswordConfirmation());
        page.OnGet();
    }

    private static Mock<SignInManager<Korisnik>> SignInManager(UserManager<Korisnik> users) =>
        new(users, new HttpContextAccessor(), Mock.Of<IUserClaimsPrincipalFactory<Korisnik>>(),
            Options.Create(new IdentityOptions()), NullLogger<SignInManager<Korisnik>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<Korisnik>>());
}
