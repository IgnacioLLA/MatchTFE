using System.Net;
using MatchTFE.Client.Pages;
using MatchTFE.Client.Tests.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using TFELibrary.Shared;

namespace MatchTFE.Client.Tests.Pages;

[TestClass]
public class RegisterTests : BunitTestBase
{
    private const string ValidPassword = "Password1!";
    private const string ValidEmail = "john@example.com";

    private static void FillAllValid(IRenderedComponent<Register> cut)
    {
        cut.FindAll("input")[0].Change("John");
        cut.FindAll("input")[1].Change("Doe");
        cut.FindAll("input")[2].Change(ValidEmail);
        cut.FindAll("input")[3].Change(ValidEmail);
        cut.FindAll("input")[4].Change(ValidPassword);
        cut.FindAll("input")[5].Change(ValidPassword);
    }

    // -------------------------------------------------------------------------
    // Required field validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Submit_EmptyFirstName_ShowsFirstNameError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[0].Change("");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[0];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_FirstNameRequired", field.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_EmptyLastName_ShowsLastNameError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[1].Change("");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[1];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_LastNameRequired", field.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_EmptyEmail_ShowsEmailRequiredError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[2].Change("");
        cut.FindAll("input")[3].Change("");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[2];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_EmailRequired", field.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_InvalidEmail_ShowsEmailFormatError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[2].Change("notanemail");
        cut.FindAll("input")[3].Change("notanemail");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[2];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_EmailInvalid", field.Instance.ErrorText);
    }

    // -------------------------------------------------------------------------
    // Confirmation field validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Submit_EmailMismatch_ShowsConfirmEmailError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[3].Change("other@example.com");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[3];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_EmailMismatch", field.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_PasswordMismatch_ShowsConfirmPasswordError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[5].Change("DifferentPass1!");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[5];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_PasswordMismatch", field.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_EmptyConfirmEmail_ShowsConfirmEmailRequiredError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[3].Change("");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[3];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_ConfirmEmailRequired", field.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_EmptyConfirmPassword_ShowsConfirmPasswordRequiredError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[5].Change("");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[5];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_ConfirmPasswordRequired", field.Instance.ErrorText);
    }

    // -------------------------------------------------------------------------
    // Password complexity validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Submit_EmptyPassword_ShowsPasswordRequiredError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[4].Change("");
        cut.FindAll("input")[5].Change("");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[4];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_PasswordRequired", field.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_PasswordTooShort_ShowsPasswordTooShortError()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[4].Change("Ab1!");  // 4 chars, below minimum of 6
        cut.FindAll("input")[5].Change("Ab1!");
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[4];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_PasswordTooShort", field.Instance.ErrorText);
    }

    [TestMethod]
    [DataRow("password1!", "no uppercase letter")]
    [DataRow("Password!",  "no digit")]
    [DataRow("Password1",  "no special character")]
    public void Submit_PasswordMissingComplexityRequirement_ShowsComplexityError(string password, string _)
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.FindAll("input")[4].Change(password);
        cut.FindAll("input")[5].Change(password);
        cut.Find("form").Submit();

        var field = cut.FindComponents<MudTextField<string>>()[4];
        Assert.IsTrue(field.Instance.Error);
        Assert.AreEqual("Register_PasswordComplexityError", field.Instance.ErrorText);
    }

    [TestMethod]
    public void Submit_AllValidInputs_DoesNotShowErrors()
    {
        SetupGatewayApi(HttpStatusCode.OK,
            new RegisterResponse { Error = new OperationResult(true, "OK") });
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.Find("form").Submit();

        var fields = cut.FindComponents<MudTextField<string>>();
        Assert.IsTrue(fields.All(f => !f.Instance.Error));
    }

    // -------------------------------------------------------------------------
    // UI interactions
    // -------------------------------------------------------------------------

    [TestMethod]
    public void PasswordVisibilityToggle_Click_ChangesInputTypeToText()
    {
        SetupGatewayApi(HttpStatusCode.OK);
        var cut = Render<Register>();

        var passwordField = cut.FindComponents<MudTextField<string>>()[4];
        Assert.AreEqual(InputType.Password, passwordField.Instance.InputType);

        cut.FindAll(".mud-input-adornment-end button")[0].Click();

        Assert.AreEqual(InputType.Text, passwordField.Instance.InputType);
    }

    // -------------------------------------------------------------------------
    // API flows
    // -------------------------------------------------------------------------

    [TestMethod]
    public void Submit_ApiSuccess_ShowsSuccessSnackbarAndNavigatesToLogin()
    {
        SetupGatewayApi(HttpStatusCode.OK,
            new RegisterResponse { Error = new OperationResult(true, "OK") });
        var snackbar = SetupSnackbar();
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.Find("form").Submit();

        snackbar.Verify(
            s => s.Add(It.IsAny<string>(), Severity.Success, It.IsAny<Action<SnackbarOptions>?>(), It.IsAny<string?>()),
            Times.Once);

        var navMan = Services.GetRequiredService<NavigationManager>();
        Assert.AreEqual("http://localhost/login", navMan.Uri);
    }

    [TestMethod]
    public void Submit_ApiReturnsDuplicateEmail_ShowsWarningSnackbar()
    {
        // 200 OK but IsSuccess=false with DuplicateEmail code
        SetupGatewayApi(HttpStatusCode.OK,
            new RegisterResponse { Error = new OperationResult(false, "Duplicate", "DuplicateEmail") });
        var snackbar = SetupSnackbar();
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.Find("form").Submit();

        snackbar.Verify(
            s => s.Add(It.IsAny<string>(), Severity.Warning, It.IsAny<Action<SnackbarOptions>?>(), It.IsAny<string?>()),
            Times.Once);
    }

    [TestMethod]
    public void Submit_ApiReturnsServerError_ShowsErrorSnackbar()
    {
        SetupGatewayApi(HttpStatusCode.InternalServerError,
            new RegisterResponse { Error = new OperationResult(false, "Server error", "ServerError") });
        var snackbar = SetupSnackbar();
        var cut = Render<Register>();

        FillAllValid(cut);
        cut.Find("form").Submit();

        snackbar.Verify(
            s => s.Add(It.IsAny<string>(), Severity.Error, It.IsAny<Action<SnackbarOptions>?>(), It.IsAny<string?>()),
            Times.Once);
    }
}
