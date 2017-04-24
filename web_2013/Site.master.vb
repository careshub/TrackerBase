Imports CE = CommonEnums
Imports CF = CommonFunctions
Imports CV = CommonVariables

Partial Class Site
  Inherits System.Web.UI.MasterPage
  Dim BR As String = CV.HtmlLineBreak
  Dim sessionPrjId As String = CV.SessionProjectId
  Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
    Dim whatIsNothing As String = "" 'find the issue, turns out lbl is sometimes nothing, not sure how
    Session("randomthingtoholdontosessionid") = 666  'setting anything on Session prevents new Session creation on page refresh
    Try
      whatIsNothing &= "  uxHiddenIsUserAuth: " & (uxHiddenIsUserAuth Is Nothing).ToString & BR ' ----- debug
      If Page.User.Identity.IsAuthenticated Then uxHiddenIsUserAuth.Value = "true" Else uxHiddenIsUserAuth.Value = "false"
      Dim lbl As New Label 'need to instantiate or won't work
      whatIsNothing &= "  HeadLoginView: " & (HeadLoginView Is Nothing).ToString & BR ' ----- debug
      lbl = CType(HeadLoginView.FindControl("uxDisplayName"), Label)
      whatIsNothing &= "  lbl: " & (lbl Is Nothing).ToString & BR ' ----- debug
      whatIsNothing &= "  Profile: " & (Profile Is Nothing).ToString & BR ' ----- debug
      If lbl IsNot Nothing Then lbl.Text = Profile.DisplayName
      whatIsNothing &= "  uxSiteUseWarning: " & (uxSiteUseWarning Is Nothing).ToString & BR ' ----- debug
      uxSiteUseWarning.Visible = False
      Dim site As String = CF.GetSiteSubdomain
      If Not String.IsNullOrWhiteSpace(site) Then uxSiteUseWarning.Visible = True
    Catch ex As Exception
      CF.SendEmail(CV.ozzyEmail, CF.GetSiteEmail, "Master " & MethodIdentifier() & " error", ex.ToString & BR & whatIsNothing, Nothing)
    End Try
  End Sub

  Private Function MethodIdentifier() As String
    'Used for error message attributes (title)
    Try
      Return CF.FormatMethodIdentifier(System.Reflection.MethodBase.GetCurrentMethod.DeclaringType.Name, New System.Diagnostics.StackFrame(1).GetMethod().Name) 
    Catch ex As Exception
      Return "Master page MethodIdentifier didn't work"
    End Try
  End Function

End Class
