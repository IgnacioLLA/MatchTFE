using System.Net;
using Bunit.TestDoubles;
using MatchTFE.Client.Components;
using MatchTFE.Client.Pages;
using MatchTFE.Client.Tests.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TFELibrary.Shared;

namespace MatchTFE.Client.Tests.Pages;

[TestClass]
public class NewTfeTests : BunitTestBase
{
    // -------------------------------------------------------------------------
    // API flows
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task ApiSuccess_NavigatesToAuthorPage()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        ComponentFactories.AddStub<TfeForm>();
        var cut = Render<NewTfe>();

        var callback = cut.FindComponent<Stub<TfeForm>>().Instance.Parameters.Get(p => p.OnValidSubmit);
        await cut.InvokeAsync(() => callback.InvokeAsync(new TfeDto { Title = "Test" }));

        var navMan = Services.GetRequiredService<NavigationManager>();
        Assert.AreEqual("http://localhost/tfe/author", navMan.Uri);
    }

    [TestMethod]
    public async Task ApiError_SetsServerErrorOnTfeForm()
    {
        SetupGatewayApi(HttpStatusCode.InternalServerError);
        ComponentFactories.AddStub<TfeForm>();
        var cut = Render<NewTfe>();

        var callback = cut.FindComponent<Stub<TfeForm>>().Instance.Parameters.Get(p => p.OnValidSubmit);
        await cut.InvokeAsync(() => callback.InvokeAsync(new TfeDto { Title = "Test" }));

        cut.WaitForAssertion(() =>
            Assert.AreEqual("TfeForm_CreateError",
                cut.FindComponent<Stub<TfeForm>>().Instance.Parameters.Get(p => p.ServerError)));
    }

    [TestMethod]
    public async Task ConnectionException_SetsServerErrorOnTfeForm()
    {
        SetupGatewayApiThrows();
        ComponentFactories.AddStub<TfeForm>();
        var cut = Render<NewTfe>();

        var callback = cut.FindComponent<Stub<TfeForm>>().Instance.Parameters.Get(p => p.OnValidSubmit);
        await cut.InvokeAsync(() => callback.InvokeAsync(new TfeDto { Title = "Test" }));

        cut.WaitForAssertion(() =>
            Assert.AreEqual("TfeForm_ConnectionError",
                cut.FindComponent<Stub<TfeForm>>().Instance.Parameters.Get(p => p.ServerError)));
    }

    // -------------------------------------------------------------------------
    // Initial render
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Render_ShowsFormTitle()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        ComponentFactories.AddStub<TfeForm>();
        var cut = Render<NewTfe>();

        Assert.IsTrue(cut.Markup.Contains("TfeForm_NewTitle"));
    }
}
