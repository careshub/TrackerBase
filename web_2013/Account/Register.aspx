<%@ Page Title="TerLoc Register" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="false" CodeFile="Register.aspx.vb" Inherits="Account_Register" %>


<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
  <script type="text/javascript" src="/Scripts/Account.js"></script>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
  <asp:Label runat="server" ID="uxInfo"></asp:Label>
  <asp:CreateUserWizard ID="RegisterUser" runat="server" EnableViewState="false" 
    DuplicateUserNameErrorMessage="Please enter a different user name. That name may already be registered.">
    <LayoutTemplate>
      <asp:PlaceHolder ID="wizardStepPlaceholder" runat="server"></asp:PlaceHolder>
      <asp:PlaceHolder ID="navigationPlaceholder" runat="server"></asp:PlaceHolder>
    </LayoutTemplate>
    <WizardSteps>
      <asp:CreateUserWizardStep ID="RegisterUserWizardStep" runat="server" >
        <ContentTemplate>
          <h2>
            Register with Terrace Tools
          </h2>
          <p>
            Use the form below to create a new account. Passwords are required to be a minimum of <%= Membership.MinRequiredPasswordLength %> characters in length.
          </p>
          <span class="failure-notification">
            <asp:Literal ID="ErrorMessage" runat="server"></asp:Literal>
          </span>
          <asp:ValidationSummary ID="RegisterUserValidationSummary" runat="server" CssClass="failure-notification" 
                ValidationGroup="RegisterUserValidationGroup"/>
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
                <p>
                  <asp:Label ID="uxFirstNameLabel" runat="server" AssociatedControlID="uxFirstName" CssClass="left-column-reg">First Name:</asp:Label>
                  <asp:TextBox ID="uxFirstName" runat="server" CssClass="text-entry"></asp:TextBox>
                  <asp:RequiredFieldValidator ID="uxFirstNameRequired" runat="server" ControlToValidate="uxFirstName" 
                        CssClass="failure-notification" ErrorMessage="First Name is required." ToolTip="First Name is required." 
                        ValidationGroup="RegisterUserValidationGroup">*</asp:RequiredFieldValidator>
                </p>
                <p>
                  <asp:Label ID="uxLastNameLabel" runat="server" AssociatedControlID="uxLastName" CssClass="left-column-reg">Last Name:</asp:Label>
                  <asp:TextBox ID="uxLastName" runat="server" CssClass="text-entry"></asp:TextBox>
                  <asp:RequiredFieldValidator ID="uxLastNameRequired" runat="server" ControlToValidate="uxLastName" 
                        CssClass="failure-notification" ErrorMessage="Last Name is required." ToolTip="Last Name is required." 
                        ValidationGroup="RegisterUserValidationGroup">*</asp:RequiredFieldValidator>
                </p>
                <p>
                  <asp:Label ID="uxDisplayNameLabel" runat="server" AssociatedControlID="uxDisplayNameReg" CssClass="left-column-reg">Display Name:</asp:Label>
                  <asp:TextBox ID="uxDisplayNameReg" runat="server" CssClass="text-entry"></asp:TextBox>
                  <asp:RequiredFieldValidator ID="uxDisplayNameRequired" runat="server" ControlToValidate="uxDisplayNameReg" 
                        CssClass="failure-notification" ErrorMessage="Display Name is required." ToolTip="Display Name is required." 
                        ValidationGroup="RegisterUserValidationGroup">*</asp:RequiredFieldValidator>
                </p>
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
                  <asp:Label ID="uxPasswordLabel" runat="server" AssociatedControlID="Password" CssClass="left-column-reg">Password:</asp:Label>
                  <asp:TextBox ID="Password" runat="server" CssClass="password-entry" TextMode="Password"></asp:TextBox>
                  <asp:RequiredFieldValidator ID="uxPasswordRequired" runat="server" ControlToValidate="Password" 
                        CssClass="failure-notification" ErrorMessage="Password is required." ToolTip="Password is required." 
                        ValidationGroup="RegisterUserValidationGroup">*</asp:RequiredFieldValidator>
                </p>
                <p>
                  <asp:Label ID="uxConfirmPasswordLabel" runat="server" AssociatedControlID="uxConfirmPassword" CssClass="left-column-reg">Confirm Password:</asp:Label>
                  <asp:TextBox ID="uxConfirmPassword" runat="server" CssClass="password-entry" TextMode="Password"></asp:TextBox>
                  <asp:RequiredFieldValidator ControlToValidate="uxConfirmPassword" CssClass="failure-notification" Display="Dynamic" 
                        ErrorMessage="Confirm Password is required." ID="uxConfirmPasswordRequired" runat="server" 
                        ToolTip="Confirm Password is required." ValidationGroup="RegisterUserValidationGroup">*</asp:RequiredFieldValidator>
                  <asp:CompareValidator ID="uxPasswordCompare" runat="server" ControlToCompare="Password" ControlToValidate="uxConfirmPassword" 
                        CssClass="failure-notification" Display="Dynamic" ErrorMessage="The Password and Confirmation Password must match."
                        ValidationGroup="RegisterUserValidationGroup">*</asp:CompareValidator>
                </p>
            </fieldset>
            <p class="submit-button">
              <asp:Button ID="uxCreateUserButton" runat="server" CommandName="MoveNext" Text="Create User" 
                    ValidationGroup="RegisterUserValidationGroup"/>
            </p>
          </div>
        </ContentTemplate>
        <CustomNavigationTemplate>
        </CustomNavigationTemplate>
      </asp:CreateUserWizardStep>
    </WizardSteps>
  </asp:CreateUserWizard>
    <div class="footer text-small">
      &copy; 2015 — <asp:Label ID="lblCurrentYear" runat="server" Text="2020"></asp:Label>
        Curators of the <a href="http://www.umsystem.edu" target="_blank">University of Missouri</a>. 
        <a href="http://www.missouri.edu/dmca/" target="_blank">DMCA</a> and other 
        <a href="http://www.missouri.edu/copyright.php">copyright information</a>. All rights reserved.
    </div>
</asp:Content>
