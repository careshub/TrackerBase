Imports System.Data
Imports System.Data.SqlClient
Imports CommonFunctions 

Partial Class Account_Register
    Inherits System.Web.UI.Page
   
  Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
    Dim userNameCtrl As TextBox = CType(RegisterUserWizardStep.ContentTemplateContainer.FindControl("UserName"), TextBox)
    userNameCtrl.Attributes.Add("onblur", "UpdateDisplayName(this.value);")
    RegisterUser.ContinueDestinationPageUrl = Request.QueryString("ReturnUrl")

    Dim currYr As Label = FindControlRecursive(Me.Page, "lblCurrentYear")
    If currYr IsNot Nothing Then currYr.Text = Now.Year
  End Sub

  Protected Sub RegisterUser_CreatedUser(ByVal sender As Object, ByVal e As EventArgs) Handles RegisterUser.CreatedUser 
    Try 
      FormsAuthentication.SetAuthCookie(RegisterUser.UserName, False)
      Dim newUser As MembershipUser = Membership.GetUser(CType(sender, CreateUserWizard).UserName) 
      Dim addUserInfo As String = AddToCustomUser(newUser)
      If String.IsNullOrWhiteSpace(addUserInfo) Then
        Dim continueUrl As String = RegisterUser.ContinueDestinationPageUrl
        If String.IsNullOrEmpty(continueUrl) Then continueUrl = "ProjectMgmt" 
        Response.Redirect(continueUrl)
      Else
        uxInfo.Text = addUserInfo
      End If

    Catch ex As Exception
      uxInfo.Text = "RegisterUser_CreatedUser: " & ex.ToString
    End Try
  End Sub

  Protected Function AddToCustomUser(usr As MembershipUser) As String
    Dim retVal As String = ""
    Try
      Dim dataConn As String = ConfigurationManager.ConnectionStrings("AspNetConnString").ConnectionString
      Dim encryptedCookie As HttpCookie = Response.Cookies.Get(FormsAuthentication.FormsCookieName)
      Dim decryptedCookie As FormsAuthenticationTicket
      Dim profle As ProfileCommon = Nothing
      Try
        decryptedCookie = FormsAuthentication.Decrypt(encryptedCookie.Value)
        profle = Profile.GetProfile(decryptedCookie.Name)
      Catch ex As Exception
        retVal &= " Decrypt cookie issue: " & ex.Message
      End Try
      Using conn As New SqlConnection(dataConn)
        Dim prm As SqlParameter

        conn.Open()

        Dim cmdText As String = "INSERT INTO dbo.Terrace_Users values (@UserId, @FirstName, @LastName, @DisplayName);"
        Dim intRecs As Integer

        Dim cmd As New SqlCommand(cmdText, conn)
        cmd.CommandType = CommandType.Text

        Dim usrCtrl As TextBox

        prm = New SqlParameter("@UserId", SqlDbType.UniqueIdentifier)
        prm.Direction = ParameterDirection.Input
        prm.Value = New Guid(usr.ProviderUserKey.ToString())
        cmd.Parameters.Add(prm)

        usrCtrl = CType(RegisterUserWizardStep.ContentTemplateContainer.FindControl("uxFirstName"), TextBox)
        prm = New SqlParameter("@FirstName", SqlDbType.VarChar, 50)
        prm.Direction = ParameterDirection.Input
        prm.Value = usrCtrl.Text
        cmd.Parameters.Add(prm)
        If profle IsNot Nothing Then profle.FirstName = usrCtrl.Text

        usrCtrl = CType(RegisterUserWizardStep.ContentTemplateContainer.FindControl("uxLastName"), TextBox)
        prm = New SqlParameter("@LastName", SqlDbType.VarChar, 50)
        prm.Direction = ParameterDirection.Input
        prm.Value = usrCtrl.Text
        cmd.Parameters.Add(prm)
        If profle IsNot Nothing Then profle.LastName = usrCtrl.Text

        usrCtrl = CType(RegisterUserWizardStep.ContentTemplateContainer.FindControl("uxDisplayNameReg"), TextBox)
        prm = New SqlParameter("@DisplayName", SqlDbType.VarChar, 128)
        prm.Direction = ParameterDirection.Input
        prm.Value = usrCtrl.Text
        cmd.Parameters.Add(prm)
        If profle IsNot Nothing Then profle.DisplayName = usrCtrl.Text
        If profle IsNot Nothing Then profle.Save() 'must save since created here

        intRecs = CInt(cmd.ExecuteNonQuery())

        If intRecs = 0 Then retVal = "Failed to add user to supplemental table."
      End Using
    Catch ex As Exception
      retVal = ex.Message
    End Try
    Return retVal
  End Function

End Class
