using Bunit.TestDoubles;
using MatchTFE.Client.Components.Home;
using MatchTFE.Client.Pages;
using MatchTFE.Client.Tests.Shared;
using MudBlazor;
using TFELibrary.Shared;

namespace MatchTFE.Client.Tests.Pages;

[TestClass]
public class HomeTests : BunitTestBase
{
    private IRenderedComponent<Home> RenderHome(ProfileDto? profile)
    {
        ComponentFactories.AddStub<AdminHomePanel>();
        ComponentFactories.AddStub<TeacherHomePanel>();
        ComponentFactories.AddStub<StudentHomePanel>();
        if (profile == null)
            return Render<Home>();
        return Render<Home>(p => p.Add(cp => cp.GlobalProfile, profile));
    }

    // -------------------------------------------------------------------------
    // Null profile
    // -------------------------------------------------------------------------

    [TestMethod]
    public void NullProfile_ShowsProgressBar()
    {
        var cut = RenderHome(null);

        Assert.AreEqual(1, cut.FindComponents<MudProgressLinear>().Count);
        Assert.AreEqual(0, cut.FindComponents<Stub<AdminHomePanel>>().Count);
    }

    // -------------------------------------------------------------------------
    // Role-based panel rendering
    // -------------------------------------------------------------------------

    [TestMethod]
    public void AdminProfile_ShowsAdminPanel()
    {
        var cut = RenderHome(new ProfileDto { Role = RoleType.Admin });

        Assert.AreEqual(1, cut.FindComponents<Stub<AdminHomePanel>>().Count);
        Assert.AreEqual(0, cut.FindComponents<Stub<TeacherHomePanel>>().Count);
        Assert.AreEqual(0, cut.FindComponents<Stub<StudentHomePanel>>().Count);
    }

    [TestMethod]
    public void TeacherProfile_ShowsTeacherPanel()
    {
        var cut = RenderHome(new ProfileDto { Role = RoleType.Teacher });

        Assert.AreEqual(1, cut.FindComponents<Stub<TeacherHomePanel>>().Count);
        Assert.AreEqual(0, cut.FindComponents<Stub<AdminHomePanel>>().Count);
        Assert.AreEqual(0, cut.FindComponents<Stub<StudentHomePanel>>().Count);
    }

    [TestMethod]
    public void StudentProfile_ShowsStudentPanel()
    {
        var cut = RenderHome(new ProfileDto { Role = RoleType.Student });

        Assert.AreEqual(1, cut.FindComponents<Stub<StudentHomePanel>>().Count);
        Assert.AreEqual(0, cut.FindComponents<Stub<AdminHomePanel>>().Count);
        Assert.AreEqual(0, cut.FindComponents<Stub<TeacherHomePanel>>().Count);
    }
}
