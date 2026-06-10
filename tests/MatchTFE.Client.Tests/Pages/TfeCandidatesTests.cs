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
public class TfeCandidatesTests : BunitTestBase
{
    private static TfeDto MakeOpenTfe() => new()
    {
        Id = 1,
        Title = "Test TFE",
        Status = TfeStatus.Open,
        ExpirationDate = DateTime.Today.AddMonths(1)
    };

    private static TfeCandidateDto MakePendingCandidate(string userId = "10") => new()
    {
        Profile = new ProfileDto { Id = userId, FirstName = "Ana", LastName = "López" },
        Status = ProposalStatus.Pending
    };

    private static ProfileByTfeInterestResponse MakeCandidatesResponse(
        params TfeCandidateDto[] candidates) =>
        new(new OperationResult(true, "OK"), [.. candidates]);

    private IRenderedComponent<TfeCandidates> RenderTfeCandidates(int tfeId = 1)
    {
        ComponentFactories.AddStub<TfeSummary>();
        ComponentFactories.AddStub<CandidateListItem>();
        return Render<TfeCandidates>(p => p.Add(x => x.TfeId, tfeId));
    }

    // -------------------------------------------------------------------------
    // Initial load
    // -------------------------------------------------------------------------

    [TestMethod]
    public void OnInit_LoadSuccess_ShowsCandidates()
    {
        var response = MakeCandidatesResponse(MakePendingCandidate("10"), MakePendingCandidate("11"));
        SetupGatewayApiSequence(
            (HttpStatusCode.OK, MakeOpenTfe()),
            (HttpStatusCode.OK, response));
        var cut = RenderTfeCandidates();

        cut.WaitForAssertion(() => Assert.AreEqual(2, cut.FindComponents<Stub<CandidateListItem>>().Count));
    }

    [TestMethod]
    public void OnInit_LoadError_ShowsErrorAlert()
    {
        SetupGatewayApiThrows();
        var cut = RenderTfeCandidates();

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindComponents<MudProgressCircular>().Count));
        var alert = cut.FindComponent<MudAlert>();
        Assert.AreEqual(Severity.Error, alert.Instance.Severity);
    }

    [TestMethod]
    public void OnInit_NoCandidates_ShowsEmptyState()
    {
        SetupGatewayApiSequence(
            (HttpStatusCode.OK, MakeOpenTfe()),
            (HttpStatusCode.OK, MakeCandidatesResponse()));
        var cut = RenderTfeCandidates();

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindComponents<MudProgressCircular>().Count));
        Assert.AreEqual(0, cut.FindComponents<Stub<CandidateListItem>>().Count);
        Assert.IsTrue(cut.Markup.Contains("Candidates_NoneTitle"));
    }

    [TestMethod]
    public void OnInit_CompletedTfe_ShowsCompletedNotice()
    {
        var completedTfe = MakeOpenTfe();
        completedTfe.Status = TfeStatus.Completed;
        SetupGatewayApiSequence(
            (HttpStatusCode.OK, completedTfe),
            (HttpStatusCode.OK, MakeCandidatesResponse()));
        var cut = RenderTfeCandidates();

        cut.WaitForAssertion(() => Assert.AreEqual(0, cut.FindComponents<MudProgressCircular>().Count));
        var alert = cut.FindComponent<MudAlert>();
        Assert.AreEqual(Severity.Info, alert.Instance.Severity);
    }

    // -------------------------------------------------------------------------
    // Navigation
    // -------------------------------------------------------------------------

    [TestMethod]
    public void BackButton_Click_NavigatesToAuthorPage()
    {
        SetupGatewayApi(HttpStatusCode.OK, MakeOpenTfe());
        var cut = RenderTfeCandidates();

        cut.FindComponent<MudButton>().Find("button").Click();

        Assert.AreEqual("http://localhost/tfe/author",
            Services.GetRequiredService<NavigationManager>().Uri);
    }

    // -------------------------------------------------------------------------
    // Accept candidate
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task AcceptCandidate_ApiSuccess_UpdatesCandidateStatus()
    {
        var candidate = MakePendingCandidate();
        SetupGatewayApiSequence(
            (HttpStatusCode.OK, MakeOpenTfe()),
            (HttpStatusCode.OK, MakeCandidatesResponse(candidate)),
            (HttpStatusCode.OK, null));

        var cut = RenderTfeCandidates();
        cut.WaitForAssertion(() => Assert.AreEqual(1, cut.FindComponents<Stub<CandidateListItem>>().Count));

        var onAccept = cut.FindComponent<Stub<CandidateListItem>>().Instance.Parameters.Get(p => p.OnAccept);
        await cut.InvokeAsync(() => onAccept.InvokeAsync(candidate));

        Assert.AreEqual(ProposalStatus.Accepted, candidate.Status);
    }
}
