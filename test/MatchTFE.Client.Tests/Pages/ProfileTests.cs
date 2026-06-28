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
public class ProfileTests : BunitTestBase
{
    private static ProfileDto MakeProfile(RoleType role) => new()
    {
        FirstName = "Ana", LastName = "García", Email = "ana@uni.es",
        Bio = "Bio original", Role = role
    };

    private IRenderedComponent<Profile> RenderProfile(ProfileDto? profile)
    {
        ComponentFactories.AddStub<TagSelector>();
        ComponentFactories.AddStub<StudentProfileComponent>();
        ComponentFactories.AddStub<TeacherProfileComponent>();

        Render<MudPopoverProvider>();

        if (profile == null)
            return Render<Profile>();
        return Render<Profile>(p => p.Add(cp => cp.GlobalProfile, profile));
    }

    // -------------------------------------------------------------------------
    // Initial load
    // -------------------------------------------------------------------------

    [TestMethod]
    public void OnInit_WithStudentProfile_RendersProfileData()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = RenderProfile(MakeProfile(RoleType.Student));

        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<MudButton>().Count));
        Assert.IsTrue(cut.Markup.Contains("Ana"));
    }

    [TestMethod]
    public void OnInit_NullGlobalProfile_KeepsLoadingState()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = RenderProfile(null);

        cut.WaitForAssertion(() => Assert.IsTrue(cut.FindComponents<MudProgressCircular>().Count > 0));
        Assert.AreEqual(0, cut.FindComponents<MudButton>().Count);
    }

    // -------------------------------------------------------------------------
    // Role-based rendering
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Admin_HidesNotificationAndRoleSpecificSections()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = RenderProfile(MakeProfile(RoleType.Admin));

        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<MudButton>().Count));
        Assert.AreEqual(0, cut.FindComponents<MudSelect<NotificationFrequency>>().Count);
        Assert.AreEqual(0, cut.FindComponents<Stub<StudentProfileComponent>>().Count);
        Assert.AreEqual(0, cut.FindComponents<Stub<TeacherProfileComponent>>().Count);
    }

    [TestMethod]
    public void Student_ShowsStudentProfileComponent()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = RenderProfile(MakeProfile(RoleType.Student));

        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<MudButton>().Count));
        Assert.AreEqual(1, cut.FindComponents<Stub<StudentProfileComponent>>().Count);
    }

    [TestMethod]
    public void Teacher_ShowsTeacherProfileComponent()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = RenderProfile(MakeProfile(RoleType.Teacher));

        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<MudButton>().Count));
        Assert.AreEqual(1, cut.FindComponents<Stub<TeacherProfileComponent>>().Count);
    }

    // -------------------------------------------------------------------------
    // SaveChanges
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SaveChanges_ApiSuccess_UpdatesProfileInDOM()
    {
        var updatedProfile = MakeProfile(RoleType.Student);
        updatedProfile.Bio = "Bio actualizada";
        var body = new ProfileUpdateResponse(new OperationResult(true, "OK"), updatedProfile);
        SetupGatewayApi(HttpStatusCode.OK, body);

        var cut = RenderProfile(MakeProfile(RoleType.Student));
        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<MudButton>().Count));

        await cut.InvokeAsync(() => cut.FindComponents<MudButton>()[1].Find("button").Click());

        Assert.AreEqual("Bio actualizada",
            cut.FindComponents<MudTextField<string>>()[3].Instance.Value);
    }

    // -------------------------------------------------------------------------
    // Discard changes
    // -------------------------------------------------------------------------

    [TestMethod]
    public void DiscardChanges_ResetsBioToOriginalValue()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var profile = MakeProfile(RoleType.Student);
        var cut = RenderProfile(profile);
        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<MudButton>().Count));

        cut.Find("textarea").Change("Nueva bio");
        cut.FindComponents<MudButton>()[0].Find("button").Click();

        Assert.AreEqual("Bio original", cut.FindComponents<MudTextField<string>>()[3].Instance.Value);
    }

    // -------------------------------------------------------------------------
    // Logout
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Logout_ApiSuccess_NavigatesToLogin()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = RenderProfile(MakeProfile(RoleType.Student));
        cut.WaitForAssertion(() => Assert.AreEqual(3, cut.FindComponents<MudButton>().Count));

        await cut.InvokeAsync(() => cut.FindComponents<MudButton>()[2].Find("button").Click());

        Assert.AreEqual("http://localhost/login",
            Services.GetRequiredService<NavigationManager>().Uri);
    }
}
