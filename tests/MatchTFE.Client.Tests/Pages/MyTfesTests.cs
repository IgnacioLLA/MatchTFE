using System.Net;
using Bunit.TestDoubles;
using MatchTFE.Client.Components;
using MatchTFE.Client.Pages;
using MatchTFE.Client.Tests.Shared;
using MudBlazor;
using TFELibrary.Shared;

namespace MatchTFE.Client.Tests.Pages;

[TestClass]
public class MyTfesTests : BunitTestBase
{
    private static TfeDto MakeTfe(int id, string title, TfeStatus status = TfeStatus.Open) => new()
    {
        Id = id,
        Title = title,
        Status = status,
        ExpirationDate = DateTime.Today.AddMonths(1),
        CreationDate = DateTime.Now
    };

    private IRenderedComponent<MyTfes> RenderMyTfes(List<TfeDto> proposals)
    {
        ComponentFactories.AddStub<TfeListItem>();
        SetupGatewayApiSequence(
            (HttpStatusCode.OK, proposals),
            (HttpStatusCode.OK, new List<TagDto>()));

        Render<MudPopoverProvider>();
        return Render<MyTfes>();
    }

    // -------------------------------------------------------------------------
    // Initial load
    // -------------------------------------------------------------------------

    [TestMethod]
    public void OnInit_LoadSuccess_ShowsAllProposals()
    {
        var proposals = new List<TfeDto>
        {
            MakeTfe(1, "TFE A"),
            MakeTfe(2, "TFE B"),
            MakeTfe(3, "TFE C")
        };
        var cut = RenderMyTfes(proposals);

        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<Stub<TfeListItem>>().Count));
    }

    [TestMethod]
    public void OnInit_LoadError_ShowsEmptyList()
    {
        ComponentFactories.AddStub<TfeListItem>();
        SetupGatewayApiThrows();
        Render<MudPopoverProvider>();
        var cut = Render<MyTfes>();

        Assert.AreEqual(0, cut.FindComponents<Stub<TfeListItem>>().Count);
    }

    // -------------------------------------------------------------------------
    // Filtering
    // -------------------------------------------------------------------------

    [TestMethod]
    public void SearchQuery_FiltersByTitle()
    {
        var proposals = new List<TfeDto>
        {
            MakeTfe(1, "Machine Learning"),
            MakeTfe(2, "Web Development"),
            MakeTfe(3, "Data Science")
        };
        var cut = RenderMyTfes(proposals);
        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<Stub<TfeListItem>>().Count));

        cut.FindComponent<MudTextField<string>>().Find("input").Change("Machine");

        Assert.AreEqual(1, cut.FindComponents<Stub<TfeListItem>>().Count);
    }

    [TestMethod]
    public async Task StatusFilter_Open_ShowsOnlyOpenProposals()
    {
        var proposals = new List<TfeDto>
        {
            MakeTfe(1, "Open 1", TfeStatus.Open),
            MakeTfe(2, "Open 2", TfeStatus.Open),
            MakeTfe(3, "Done",   TfeStatus.Completed)
        };
        var cut = RenderMyTfes(proposals);
        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<Stub<TfeListItem>>().Count));

        // StatusFilter is bound to MudSelect[1] via @bind-Value. Invoking ValueChanged sets the
        // property and the InvokeAsync rendering cycle re-renders MyTfes with the new filter.
        await cut.InvokeAsync(() =>
            cut.FindComponents<MudSelect<string>>()[1].Instance.ValueChanged.InvokeAsync("open"));

        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindComponents<Stub<TfeListItem>>().Count));
    }

    // -------------------------------------------------------------------------
    // Delete
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task HandleDelete_ApiSuccess_RemovesFromList()
    {
        var proposals = new List<TfeDto>
        {
            MakeTfe(1, "TFE A"),
            MakeTfe(2, "TFE B")
        };
        ComponentFactories.AddStub<TfeListItem>();
        SetupGatewayApiSequence(
            (HttpStatusCode.OK, proposals),    // GET proposals on init
            (HttpStatusCode.OK, new List<TagDto>()), // GET tags on init
            (HttpStatusCode.OK, null));               // DELETE
        Render<MudPopoverProvider>();
        var cut = Render<MyTfes>();

        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindComponents<Stub<TfeListItem>>().Count));

        var onDeleted = cut.FindComponents<Stub<TfeListItem>>()[0].Instance.Parameters.Get(p => p.OnDeleted);
        await cut.InvokeAsync(() => onDeleted.InvokeAsync(1));

        Assert.AreEqual(1, cut.FindComponents<Stub<TfeListItem>>().Count);
    }
}
