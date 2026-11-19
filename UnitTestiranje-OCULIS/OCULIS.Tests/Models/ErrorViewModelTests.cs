using System.Text.Json;
using OCULIS.Models;

namespace OCULIS.Tests.Models;

[TestClass]
public class ErrorViewModelTests
{
    public static IEnumerable<object[]> Cases
    {
        get
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "error-view-model.json")));
            return document.RootElement.EnumerateArray()
                .Select(x =>
                {
                    string requestId = x.GetProperty("requestId").ValueKind == JsonValueKind.Null
                        ? null!
                        : x.GetProperty("requestId").GetString()!;
                    return new object[] { requestId, x.GetProperty("showRequestId").GetBoolean() };
                }).ToArray();
        }
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void ShowRequestId_JsonTestPodaci_VracaOcekivanuVrijednost(string? requestId, bool expected)
    {
        Assert.AreEqual(expected, new ErrorViewModel { RequestId = requestId }.ShowRequestId);
    }
}
