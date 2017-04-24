<%@ Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false"
  CodeFile="Login.aspx.vb" Inherits="Account_Login" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
  <h2>
    Log In
  </h2>
  <p>
    Please enter your username and password.
    <asp:HyperLink ID="RegisterHyperLink" runat="server" EnableViewState="false">Register</asp:HyperLink>
    if you don't have an account.
  </p>
  <asp:Login ID="LoginUser" runat="server" EnableViewState="false" RenderOuterTable="false">
    <LayoutTemplate>
      <span class="failure-notification">
        <asp:Literal ID="FailureText" runat="server"></asp:Literal>
      </span>
      <asp:ValidationSummary ID="LoginUserValidationSummary" runat="server" CssClass="failure-notification"
        ValidationGroup="LoginUserValidationGroup" />
      <div class="account-info">
        <fieldset class="login">
          <legend>Account Information</legend>
          <p>
            <asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName">Username:</asp:Label>
            <asp:TextBox ID="UserName" runat="server" CssClass="text-entry"></asp:TextBox>
            <asp:RequiredFieldValidator ID="UserNameRequired" runat="server" ControlToValidate="UserName"
              CssClass="failure-notification" ErrorMessage="User Name is required." ToolTip="User Name is required."
              ValidationGroup="LoginUserValidationGroup">*</asp:RequiredFieldValidator>
          </p>
          <p>
            <asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="Password">Password:</asp:Label>
            <asp:TextBox ID="Password" runat="server" CssClass="password-entry" TextMode="Password"></asp:TextBox>
            <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="Password"
              CssClass="failure-notification" ErrorMessage="Password is required." ToolTip="Password is required."
              ValidationGroup="LoginUserValidationGroup">*</asp:RequiredFieldValidator>
          </p>
          <p>
            <asp:CheckBox ID="RememberMe" runat="server" />
            <asp:Label ID="RememberMeLabel" runat="server" AssociatedControlID="RememberMe" CssClass="inline">Keep me logged in</asp:Label>
          </p>
          <p>
            <a href="/RecoverPassword">Forgot Password?</a>
          </p>
        </fieldset>
        <p class="submit-button">
          <asp:Button ID="LoginButton" runat="server" CommandName="Login" Text="Log In" ValidationGroup="LoginUserValidationGroup" CssClass="button" />
        </p>
      </div>
    </LayoutTemplate>
  </asp:Login>
  <asp:Label runat="server" ID="uxInfo"></asp:Label>
    <div class="footer text-small">
      &copy; 2015 — <asp:Label ID="lblCurrentYear" runat="server" Text="2020"></asp:Label>
        Curators of the <a href="http://www.umsystem.edu" target="_blank">University of Missouri</a>. 
        <a href="http://www.missouri.edu/dmca/" target="_blank">DMCA</a> and other 
        <a href="http://www.missouri.edu/copyright.php">copyright information</a>. All rights reserved.
    </div>
</asp:Content>
