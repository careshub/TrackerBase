
Partial Class ErrorPage
    Inherits System.Web.UI.Page 

  Protected Sub Page_Load(sender As Object, e As System.EventArgs) Handles Me.Load
    uxIncomingInfo.Text = "Error: " + Session("EncounteredException")
  End Sub
End Class
