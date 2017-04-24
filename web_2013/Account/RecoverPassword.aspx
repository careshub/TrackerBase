<%@ Page Title="Recover Password" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false"
  CodeFile="RecoverPassword.aspx.vb" Inherits="Account_RecoverPassword" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
  <h2>
    Recover Password
  </h2>
  <p>
    Enter your User Name to receive your password.
  </p>
  <p>
    <asp:PasswordRecovery ID="PasswordRecovery" runat="server" CssClass=" " GeneralFailureText="General Failure"
      OnLoad="PasswordRecovery_Load" OnSendMailError="PasswordRecovery_SendMailError"
      OnUserLookupError="PasswordRecovery_UserLookupError" UserNameFailureText="We couldn't find that user name. Please try again." Width="95%"
      MailDefinition-From="no-reply@terrace.missouri.edu" MailDefinition-IsBodyHtml="true" MailDefinition-Subject="Terrace information"
    >
      <UserNameTemplate>
        <div class="account-info">
          <fieldset class="register">
            <legend>Account Information</legend>
            <p>
              <asp:Label ID="uxUserNameLabel" runat="server" AssociatedControlID="UserName" CssClass="left-column-reg">User Name:</asp:Label>
              <asp:TextBox ID="UserName" runat="server" CssClass="text-entry"></asp:TextBox>
              <asp:RequiredFieldValidator ID="uxUserNameRequired" runat="server" ControlToValidate="UserName"
                CssClass="failure-notification" ErrorMessage="User Name is required." ToolTip="User Name is required."
                ValidationGroup="RegisterUserValidationGroup">*</asp:RequiredFieldValidator>
            </p>
          </fieldset>
          <p class="submit-button">
            <asp:Button ID="uxSubmit" runat="server" CommandName="Submit" Text="Submit" ValidationGroup="RegisterUserValidationGroup" />
          </p>
          <p>
            <asp:Literal runat="server" ID="FailureText"></asp:Literal>
          </p>
        </div>
      </UserNameTemplate>
      <SuccessTemplate>
        <p>
          <label>
            Your password has been sent to you.
          </label>
        </p>
      </SuccessTemplate>
      <FailureTextStyle ForeColor="Red" />
    </asp:PasswordRecovery>
  </p>
</asp:Content>
