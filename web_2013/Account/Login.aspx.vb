Imports CE = CommonEnums
Imports CF = CommonFunctions
Imports CV = CommonVariables

Partial Class Account_Login
    Inherits System.Web.UI.Page

  Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
    Try
      Page.Title = CF.GetPageTitle("Login")
      If Request.QueryString("ReturnUrl") IsNot Nothing Then Response.Redirect("/Account/Login.aspx")
      'RegisterHyperLink.NavigateUrl = "Register.aspx?ReturnUrl=" + HttpUtility.UrlEncode(Request.QueryString("ReturnUrl"))
      RegisterHyperLink.NavigateUrl = "/Register"
      LoginUser.FindControl("UserName").Focus()

      Dim currYr As Label = CommonFunctions.FindControlRecursive(Me.Page, "lblCurrentYear")
      If currYr IsNot Nothing Then currYr.Text = Now.Year
    Catch ex As Exception
      uxInfo.Text = ex.Message
    End Try
  End Sub

  Protected Sub LoginUser_LoggedIn(sender As Object, e As System.EventArgs) Handles LoginUser.LoggedIn
    'Try
    '  CommonFunctions.DeleteMooCookie()
    'Catch ex As Exception
    '  Session("callInfo") = String.Format("log: {0}", ex.InnerException.Message)
    'End Try
    CType(Master.FindControl("uxHiddenProjectName"), HiddenField).Value = ""
    CType(Master.FindControl("uxHiddenProjectId"), HiddenField).Value = ""
    Session(CV.SessionProjectId) = ""
    'CType(Master.FindControl("uxHiddenPageFlag"), HiddenField).Value = "" 'set on prjmgmt page
    Response.Redirect("/ProjectMgmt")
  End Sub
End Class