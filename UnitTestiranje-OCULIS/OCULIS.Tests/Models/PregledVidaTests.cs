using OCULIS.Models;
using System.ComponentModel.DataAnnotations;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class PregledVidaTests
{
    [TestMethod]
    [DataRow(0, false)]
    [DataRow(1, true)]
    [DataRow(500, true)]
    [DataRow(501, false)]
    public void Preporuka_DuzinaUnosa_ValidiraModel(int duzina, bool ocekivano)
    {
        var model = new PregledVida { Preporuka = new string('a', duzina) };
        var context = new ValidationContext(model) { MemberName = nameof(model.Preporuka) };
        Assert.AreEqual(ocekivano, Validator.TryValidateProperty(model.Preporuka, context, null));
    }
}
