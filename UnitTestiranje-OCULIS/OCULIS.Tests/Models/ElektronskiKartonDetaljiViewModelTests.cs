using OCULIS.Models.ViewModels;
using OCULIS.Models;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class ElektronskiKartonDetaljiViewModelTests
{
    [TestMethod]
    public void NoviModel_InicijaliziraNezavisneKolekcije()
    {
        var model = new ElektronskiKartonDetaljiViewModel();
        model.Pregledi.Add(new PregledVida());
        model.Narudzbe.Add(new Narudzba());
        model.Reklamacije.Add(new Reklamacija());
        var drugi = new ElektronskiKartonDetaljiViewModel();
        Assert.AreEqual(0, drugi.Pregledi.Count);
        Assert.AreEqual(0, drugi.Narudzbe.Count);
        Assert.AreEqual(0, drugi.Reklamacije.Count);
    }
}
