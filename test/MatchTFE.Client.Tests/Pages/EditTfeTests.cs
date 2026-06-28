using System.Net;
using Bunit.TestDoubles;
using MatchTFE.Client.Components;
using MatchTFE.Client.Pages;
using MatchTFE.Client.Tests.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using TFELibrary.Shared;

namespace MatchTFE.Client.Tests.Pages;

[TestClass]
public class EditTfeTests : BunitTestBase
{
    private static TfeDto MakeTfe(TfeStatus status = TfeStatus.Open) => new()
    {
        Id = 1,
        Title = "Test TFE",
        Description = "Test description",
        Status = status,
        ExpirationDate = DateTime.Today.AddMonths(1)
    };

    private IRenderedComponent<EditTfe> RenderEditTfe(int id = 1)
    {
        ComponentFactories.AddStub<TfeForm>();
        return Render<EditTfe>(p => p.Add(x => x.Id, id));
    }

    // -------------------------------------------------------------------------
    // Initial load
    // -------------------------------------------------------------------------

    [TestMethod]
    public void OnInit_LoadSuccess_RendersForm()
    {
        SetupGatewayApi(HttpStatusCode.OK, MakeTfe());
        var cut = RenderEditTfe();

        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindComponents<Stub<TfeForm>>().Count));
    }

    [TestMethod]
    public void OnInit_LoadError_HidesForm()
    {
        SetupGatewayApiThrows();
        var cut = RenderEditTfe();

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindComponents<MudProgressCircular>().Count));
        Assert.AreEqual(0, cut.FindComponents<Stub<TfeForm>>().Count);
    }

    // -------------------------------------------------------------------------
    // Status-based rendering
    // -------------------------------------------------------------------------

    [TestMethod]
    public void OnInit_OpenTfe_ShowsActionButtons()
    {
        SetupGatewayApi(HttpStatusCode.OK, MakeTfe(TfeStatus.Open));
        var cut = RenderEditTfe();

        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindComponents<MudButton>().Count));
    }

    [TestMethod]
    public void OnInit_CompletedTfe_HidesActionButtonsAndRendersFormReadOnly()
    {
        SetupGatewayApi(HttpStatusCode.OK, MakeTfe(TfeStatus.Completed));
        var cut = RenderEditTfe();

        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindComponents<Stub<TfeForm>>().Count));

        Assert.AreEqual(0, cut.FindComponents<MudButton>().Count);
        Assert.IsTrue(cut.FindComponent<Stub<TfeForm>>().Instance.Parameters.Get(p => p.IsReadOnly));
    }

    // -------------------------------------------------------------------------
    // Update proposal
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UpdateProposal_ApiSuccess_NavigatesToAuthorPage()
    {
        SetupGatewayApiSequence(
            (HttpStatusCode.OK, MakeTfe()),
            (HttpStatusCode.OK, null));

        var cut = RenderEditTfe();
        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindComponents<Stub<TfeForm>>().Count));

        var callback = cut.FindComponent<Stub<TfeForm>>().Instance.Parameters.Get(p => p.OnValidSubmit);
        await cut.InvokeAsync(() => callback.InvokeAsync(new TfeDto()));

        Assert.AreEqual("http://localhost/tfe/author",
            Services.GetRequiredService<NavigationManager>().Uri);
    }

    [TestMethod]
    public async Task UpdateProposal_ApiError_SetsServerError()
    {
        SetupGatewayApiSequence(
            (HttpStatusCode.OK, MakeTfe()),
            (HttpStatusCode.InternalServerError, null));

        var cut = RenderEditTfe();
        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindComponents<Stub<TfeForm>>().Count));

        var callback = cut.FindComponent<Stub<TfeForm>>().Instance.Parameters.Get(p => p.OnValidSubmit);
        await cut.InvokeAsync(() => callback.InvokeAsync(new TfeDto()));

        cut.WaitForAssertion(() =>
            Assert.AreEqual("TfeForm_UpdateError",
                cut.FindComponent<Stub<TfeForm>>().Instance.Parameters.Get(p => p.ServerError)));
    }
}
