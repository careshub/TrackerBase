Imports CF = CommonFunctions

Partial Class _Default
    Inherits System.Web.UI.Page

  Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
    Page.Title = CF.GetPageTitle()
    uxSiteUseWarningDefault.Visible = False
    Dim site As String = CF.GetSiteSubdomain
    If Not String.IsNullOrWhiteSpace(site) Then
      uxSiteUseWarningDefault.Visible = True
    End If
  End Sub
End Class
