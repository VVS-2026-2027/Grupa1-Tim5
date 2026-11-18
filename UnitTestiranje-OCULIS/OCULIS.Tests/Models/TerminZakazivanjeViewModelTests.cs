using OCULIS.Models.ViewModels;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class TerminZakazivanjeViewModelTests
{
    [TestMethod]
    public void NoviModel_PocinjeSaSutrasnjimDatumomIPraznomListom()
    {
        var prije = DateTime.Today.AddDays(1);
        var model = new TerminZakazivanjeViewModel();
        var poslije = DateTime.Today.AddDays(1);
        Assert.IsTrue(model.Datum == prije || model.Datum == poslije);
        Assert.AreEqual(0, model.DostupniTermini.Count);
        var vrijeme = TimeSpan.FromHours(8);
        model.DostupniTermini.Add(vrijeme);
        Assert.AreEqual(vrijeme, model.DostupniTermini.Single());
        Assert.AreEqual(0, new TerminZakazivanjeViewModel().DostupniTermini.Count);
    }
}
