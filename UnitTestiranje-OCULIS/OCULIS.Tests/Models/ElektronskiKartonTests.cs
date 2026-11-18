using OCULIS.Models;
using System.ComponentModel.DataAnnotations;

namespace OCULIS.Tests;

[TestClass]
[TestCategory("Clan3")]
public class ElektronskiKartonTests
{
    [TestMethod]
    [DataRow(0, true)]
    [DataRow(500, true)]
    [DataRow(501, false)]
    public void Napomena_DuzinaUnosa_ValidiraModel(int duzina, bool ocekivano)
    {
        var model = new ElektronskiKarton { Napomena = new string('a', duzina) };
        var context = new ValidationContext(model) { MemberName = nameof(model.Napomena) };
        Assert.AreEqual(ocekivano, Validator.TryValidateProperty(model.Napomena, context, null));
    }
}
