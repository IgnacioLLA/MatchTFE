using System.Net;
using Bunit.TestDoubles;
using MatchTFE.Client.Pages;
using MatchTFE.Client.Tests.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using TFELibrary.Shared;

namespace MatchTFE.Client.Tests.Pages;

[TestClass]
public class LoginTests : BunitTestBase
{
    // -------------------------------------------------------------------------
    // Form validation
    // -------------------------------------------------------------------------

    [TestMethod]
    [DataRow("", "somepassword", "Login_EmailRequired")]
    [DataRow("notanemail", "somepassword", "Login_EmailInvalid")]
    public void Submit_InvalidEmail_ShowsExpectedEmailError(string email, string password, string expectedErrorKey)
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Login>();

        cut.FindAll("input")[0].Change(email);
        cut.FindAll("input")[1].Change(password);
        cut.Find("form").Submit();

        var emailField = cut.FindComponents<MudTextField<string>>()[0];
        Assert.IsTrue(emailField.Instance.Error);
        Assert.AreEqual(expectedErrorKey, emailField.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_EmptyPassword_ShowsPasswordRequiredError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Login>();

        cut.FindAll("input")[0].Change("valid@email.com");
        cut.Find("form").Submit();

        var passwordField = cut.FindComponents<MudTextField<string>>()[1];
        Assert.IsTrue(passwordField.Instance.Error);
        Assert.AreEqual("Login_PasswordRequired", passwordField.Instance.ErrorText);
    }

    // -------------------------------------------------------------------------
    // UI interactions
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ForgotPasswordLink_Click_ShowsInfoAlert()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Login>();

        Assert.AreEqual(0, cut.FindComponents<MudAlert>().Count);

        cut.FindComponents<MudLink>()[0].Find("a").Click();

        var alerts = cut.FindComponents<MudAlert>();
        Assert.AreEqual(1, alerts.Count);
        Assert.AreEqual(Severity.Info, alerts[0].Instance.Severity);
    }

    [TestMethod]
    public void ForgotPasswordLink_DoubleClick_HidesInfoAlert()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Login>();

        var link = cut.FindComponents<MudLink>()[0].Find("a");
        link.Click();
        link.Click();

        Assert.AreEqual(0, cut.FindComponents<MudAlert>().Count);
    }

    [TestMethod]
    public void PasswordVisibilityToggle_Click_ChangesInputTypeToText()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Login>();

        var passwordField = cut.FindComponents<MudTextField<string>>()[1];
        Assert.AreEqual(InputType.Password, passwordField.Instance.InputType);

        cut.Find(".mud-input-adornment-end button").Click();

        Assert.AreEqual(InputType.Text, passwordField.Instance.InputType);
    }

    // -------------------------------------------------------------------------
    // API flows
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Submit_ApiSuccess_NavigatesToHome()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Login>();

        cut.FindAll("input")[0].Change("valid@email.com");
        cut.FindAll("input")[1].Change("password123");
        cut.Find("form").Submit();

        // BunitNavigationManager.History starts empty; one entry means NavigateTo was called.
        var navMan = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        Assert.AreEqual(1, navMan.History.Count);
        Assert.AreEqual("/", navMan.History.First().Uri);
    }

    [TestMethod]
    public void Submit_ApiReturnsBadRequest_ShowsEmailError()
    {
        SetupGatewayApi(HttpStatusCode.BadRequest);
        var cut = Render<Login>();

        cut.FindAll("input")[0].Change("valid@email.com");
        cut.FindAll("input")[1].Change("password123");
        cut.Find("form").Submit();

        var emailField = cut.FindComponents<MudTextField<string>>()[0];
        Assert.IsTrue(emailField.Instance.Error);
        Assert.AreEqual("Login_EmailInvalid", emailField.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_ApiReturnsAccountSuspended_ShowsSuspendedAlert()
    {
        var body = new LoginResponse
        {
            Error = new OperationResult(false, "Account suspended", "AccountSuspended")
        };
        SetupGatewayApi(HttpStatusCode.Unauthorized, body);
        var cut = Render<Login>();

        cut.FindAll("input")[0].Change("valid@email.com");
        cut.FindAll("input")[1].Change("password123");
        cut.Find("form").Submit();

        var alerts = cut.FindComponents<MudAlert>();
        Assert.AreEqual(1, alerts.Count);
        Assert.AreEqual(Severity.Warning, alerts[0].Instance.Severity);
    }

    [TestMethod]
    public void Submit_ApiReturnsGenericError_ShowsDefaultErrorAlert()
    {
        var body = new LoginResponse
        {
            Error = new OperationResult(false, "Unknown error", "OtherError")
        };
        SetupGatewayApi(HttpStatusCode.InternalServerError, body);
        var cut = Render<Login>();

        cut.FindAll("input")[0].Change("valid@email.com");
        cut.FindAll("input")[1].Change("password123");
        cut.Find("form").Submit();

        var alerts = cut.FindComponents<MudAlert>();
        Assert.AreEqual(1, alerts.Count);
        Assert.AreEqual(Severity.Error, alerts[0].Instance.Severity);
    }

    [TestMethod]
    public void Submit_ConnectionException_ShowsConnectionErrorAlert()
    {
        SetupGatewayApiThrows();
        var cut = Render<Login>();

        cut.FindAll("input")[0].Change("valid@email.com");
        cut.FindAll("input")[1].Change("password123");
        cut.Find("form").Submit();

        var alerts = cut.FindComponents<MudAlert>();
        Assert.AreEqual(1, alerts.Count);
        Assert.AreEqual(Severity.Error, alerts[0].Instance.Severity);
    }
}
