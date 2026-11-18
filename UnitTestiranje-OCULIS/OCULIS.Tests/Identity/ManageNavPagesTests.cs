using OCULIS.Areas.Identity.Pages.Account.Manage;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class ManageNavPagesTests
{
    public static IEnumerable<object[]> Navigacija()
    {
        yield return new object[] { "Index", (Func<ViewContext, string?>)ManageNavPages.IndexNavClass };
        yield return new object[] { "Email", (Func<ViewContext, string?>)ManageNavPages.EmailNavClass };
        yield return new object[] { "ChangePassword", (Func<ViewContext, string?>)ManageNavPages.ChangePasswordNavClass };
        yield return new object[] { "DownloadPersonalData", (Func<ViewContext, string?>)ManageNavPages.DownloadPersonalDataNavClass };
        yield return new object[] { "DeletePersonalData", (Func<ViewContext, string?>)ManageNavPages.DeletePersonalDataNavClass };
        yield return new object[] { "ExternalLogins", (Func<ViewContext, string?>)ManageNavPages.ExternalLoginsNavClass };
        yield return new object[] { "PersonalData", (Func<ViewContext, string?>)ManageNavPages.PersonalDataNavClass };
        yield return new object[] { "TwoFactorAuthentication", (Func<ViewContext, string?>)ManageNavPages.TwoFactorAuthenticationNavClass };
    }

    [TestMethod]
    [DynamicData(nameof(Navigacija), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(NazivNavigacije))]
    public void NavClass_PrepoznajeAktivnuStranicu(string page, Func<ViewContext, string?> navClass)
    {
        var context = new ViewContext
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            ActionDescriptor = new ActionDescriptor { DisplayName = $"/Account/Manage/{page}.cshtml" }
        };
        Assert.AreEqual("active", navClass(context));
        context.ViewData["ActivePage"] = page.ToLowerInvariant();
        Assert.AreEqual("active", navClass(context));
        context.ViewData["ActivePage"] = "DrugaStranica";
        Assert.IsNull(navClass(context));
    }

    public static string NazivNavigacije(System.Reflection.MethodInfo method, object[] data) => $"{method.Name} ({data[0]})";
}
