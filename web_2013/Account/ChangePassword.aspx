<%@ Page Title="TerLoc Profile Editor" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false"
  CodeFile="ChangePassword.aspx.vb" Inherits="Account_ChangePassword" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
  <h2>Edit your profile
  </h2>
  <p>
    Use the form below to change your password and/or email.
  </p>
  <p>
    New passwords are required to be a minimum of <%= Membership.MinRequiredPasswordLength %> characters in length.
  </p>
  <asp:ChangePassword ID="ChangeUserPassword" runat="server" CancelDestinationPageUrl="~/" EnableViewState="false" RenderOuterTable="false"
    SuccessPageUrl="ChangePasswordSuccess.aspx">
    <ChangePasswordTemplate>
      <span class="failure-notification">
        <asp:Literal ID="FailureText" runat="server"></asp:Literal>
      </span>
      <asp:ValidationSummary ID="ChangeUserPasswordValidationSummary" runat="server" CssClass="failure-notification"
        ValidationGroup="ChangeUserPasswordValidationGroup" />
      <div class="account-info">
        <fieldset class="change-password">
          <legend>Account Information</legend>
          <p>
            <asp:Label ID="uxEmailLabel" runat="server" AssociatedControlID="Email" CssClass="left-column-reg">E-mail:</asp:Label>
            <asp:TextBox ID="Email" runat="server" CssClass="text-entry" TextMode="Email"></asp:TextBox>
            <asp:RequiredFieldValidator ID="uxEmailRequired" runat="server" ControlToValidate="Email"
              CssClass="failure-notification" ErrorMessage="E-mail is required." ToolTip="E-mail is required."
              ValidationGroup="RegisterUserValidationGroup">*</asp:RequiredFieldValidator>
          </p>
          <p>
            <asp:Label ID="uxConfirmEmailLabel" runat="server" AssociatedControlID="uxConfirmEmail" CssClass="left-column-reg">Confirm Email:</asp:Label>
            <asp:TextBox ID="uxConfirmEmail" runat="server" CssClass="text-entry" TextMode="Email"></asp:TextBox>
            <asp:RequiredFieldValidator ControlToValidate="uxConfirmEmail" CssClass="failure-notification" Display="Dynamic"
              ErrorMessage="Confirm Email is required." ID="uxConfirmEmailRequired" runat="server"
              ToolTip="Confirm Email is required." ValidationGroup="RegisterUserValidationGroup">*</asp:RequiredFieldValidator>
            <asp:CompareValidator ID="uxEmailCompare" runat="server" ControlToCompare="Email" ControlToValidate="uxConfirmEmail"
              CssClass="failure-notification" Display="Dynamic" ErrorMessage="The Email and Confirmation Email must match."
              ValidationGroup="RegisterUserValidationGroup">*</asp:CompareValidator>
          </p>
          <p>
            <asp:Label ID="CurrentPasswordLabel" runat="server" AssociatedControlID="CurrentPassword">Old Password:</asp:Label>
            <asp:TextBox ID="CurrentPassword" runat="server" CssClass="password-entry" TextMode="Password"></asp:TextBox>
            <asp:RequiredFieldValidator ID="CurrentPasswordRequired" runat="server" ControlToValidate="CurrentPassword"
              CssClass="failure-notification" ErrorMessage="Password is required." ToolTip="Old Password is required."
              ValidationGroup="ChangeUserPasswordValidationGroup">*</asp:RequiredFieldValidator>
          </p>
          <p>
            <asp:Label ID="NewPasswordLabel" runat="server" AssociatedControlID="NewPassword">New Password:</asp:Label>
            <asp:TextBox ID="NewPassword" runat="server" CssClass="password-entry" TextMode="Password"></asp:TextBox>
            <asp:RequiredFieldValidator ID="NewPasswordRequired" runat="server" ControlToValidate="NewPassword"
              CssClass="failure-notification" ErrorMessage="New Password is required." ToolTip="New Password is required."
              ValidationGroup="ChangeUserPasswordValidationGroup">*</asp:RequiredFieldValidator>
          </p>
          <p>
            <asp:Label ID="ConfirmNewPasswordLabel" runat="server" AssociatedControlID="ConfirmNewPassword">Confirm New Password:</asp:Label>
            <asp:TextBox ID="ConfirmNewPassword" runat="server" CssClass="password-entry" TextMode="Password"></asp:TextBox>
            <asp:RequiredFieldValidator ID="ConfirmNewPasswordRequired" runat="server" ControlToValidate="ConfirmNewPassword"
              CssClass="failure-notification" Display="Dynamic" ErrorMessage="Confirm New Password is required."
              ToolTip="Confirm New Password is required." ValidationGroup="ChangeUserPasswordValidationGroup">*</asp:RequiredFieldValidator>
            <asp:CompareValidator ID="NewPasswordCompare" runat="server" ControlToCompare="NewPassword" ControlToValidate="ConfirmNewPassword"
              CssClass="failure-notification" Display="Dynamic" ErrorMessage="The Confirm New Password must match the New Password entry."
              ValidationGroup="ChangeUserPasswordValidationGroup">*</asp:CompareValidator>
          </p>
        </fieldset>
        <p class="submit-button">
          <asp:Button ID="CancelPushButton" runat="server" CausesValidation="False" CommandName="Cancel" Text="Cancel" />
          <asp:Button ID="ChangePasswordPushButton" runat="server" CommandName="ChangePassword" Text="Change Password"
            ValidationGroup="ChangeUserPasswordValidationGroup" />
        </p>
      </div>
    </ChangePasswordTemplate>
  </asp:ChangePassword>
</asp:Content>
