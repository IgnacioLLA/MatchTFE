using MatchTFE.Client.Components;
using MatchTFE.Client.Tests.Shared;
using MudBlazor;
using TFELibrary.Shared;

namespace MatchTFE.Client.Tests.Components;

[TestClass]
public class TfeFormTests : BunitTestBase
{
    private IRenderedComponent<TfeForm> RenderForm(Action<TfeDto>? onSubmit = null, bool isReadOnly = false)
    {
        ComponentFactories.AddStub<TagSelector>();
        ComponentFactories.AddStub<SkillSelector>();
        
        Render<MudPopoverProvider>();
        return Render<TfeForm>(p =>
        {
            if (onSubmit != null)
                p.Add(x => x.OnValidSubmit, onSubmit);
            if (isReadOnly)
                p.Add(x => x.IsReadOnly, true);
        });
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Submit_EmptyTitleAndDescription_ShowsBothErrors()
    {
        var cut = RenderForm();

        cut.FindComponents<MudButton>()[0].Find("button").Click();

        var fields = cut.FindComponents<MudTextField<string>>();
        Assert.IsTrue(fields[0].Instance.Error);
        Assert.AreEqual("TfeForm_TitleError", fields[0].Instance.ErrorText);
        Assert.IsTrue(fields[1].Instance.Error);
        Assert.AreEqual("TfeForm_DescriptionError", fields[1].Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_PastExpirationDate_ShowsExpirationError()
    {
        var cut = RenderForm();

        cut.FindComponents<MudTextField<string>>()[0].Find("input").Change("Valid Title");
        cut.FindComponents<MudTextField<string>>()[1].Find("textarea").Change("Valid Description");
        cut.Find("input[type=date]").Change("2020-01-01");
        cut.FindComponents<MudButton>()[0].Find("button").Click();

        var expirationField = cut.FindComponents<MudTextField<string>>()[2];
        Assert.IsTrue(expirationField.Instance.Error);
        Assert.AreEqual("TfeForm_ExpirationMinError", expirationField.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_NoDuration_ShowsDurationError()
    {
        var cut = RenderForm();

        cut.FindComponents<MudTextField<string>>()[0].Find("input").Change("Valid Title");
        cut.FindComponents<MudTextField<string>>()[1].Find("textarea").Change("Valid Description");
        
        cut.FindComponents<MudButton>()[0].Find("button").Click();

        var durationSelect = cut.FindComponent<MudSelect<string>>();
        Assert.IsTrue(durationSelect.Instance.Error);
        Assert.AreEqual("TfeForm_DurationError", durationSelect.Instance.ErrorText);
    }

    // -------------------------------------------------------------------------
    // Submit behaviour
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Submit_AllValidInputs_InvokesOnValidSubmit()
    {
        TfeDto? captured = null;
        var cut = RenderForm(dto => captured = dto);

        cut.FindComponents<MudTextField<string>>()[0].Find("input").Change("Valid Title");
        cut.FindComponents<MudTextField<string>>()[1].Find("textarea").Change("Valid Description");

        await cut.InvokeAsync(() =>
            cut.FindComponent<MudSelect<string>>().Instance.ValueChanged.InvokeAsync("6"));
        cut.FindComponents<MudButton>()[0].Find("button").Click();

        Assert.IsNotNull(captured);
        Assert.AreEqual("Valid Title", captured.Title);
        Assert.AreEqual("Valid Description", captured.Description);

        Assert.AreEqual(DateTime.Today.AddMonths(6), captured.EstimatedDelivery);
    }

    // -------------------------------------------------------------------------
    // Read-only mode
    // -------------------------------------------------------------------------

    [TestMethod]
    public void IsReadOnly_HidesSubmitButton()
    {
        var cut = RenderForm(isReadOnly: true);

        Assert.AreEqual(0, cut.FindComponents<MudButton>().Count);
    }

    [TestMethod]
    public void IsReadOnly_ShowsReadOnlyAlert()
    {
        var cut = RenderForm(isReadOnly: true);

        var alerts = cut.FindComponents<MudAlert>();

        Assert.IsTrue(alerts.Count > 0);
        Assert.AreEqual(Severity.Info, alerts[0].Instance.Severity);
    }
}
