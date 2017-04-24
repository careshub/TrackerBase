
Partial Class Account_RecoverPassword
  Inherits System.Web.UI.Page

  ' Reset the field label background color.
  Sub PasswordRecovery_Load(ByVal sender As Object, ByVal e As System.EventArgs)
    PasswordRecovery.LabelStyle.ForeColor = System.Drawing.Color.Black
  End Sub

  ' Set the field label background color if the user name is not found.
  Sub PasswordRecovery_UserLookupError(ByVal sender As Object, ByVal e As System.EventArgs)
    PasswordRecovery.LabelStyle.ForeColor = System.Drawing.Color.Red
  End Sub

  Protected Sub PasswordRecovery_SendMailError(sender As Object, e As System.Web.UI.WebControls.SendMailErrorEventArgs)
    PasswordRecovery.FailureTextStyle.ForeColor = System.Drawing.Color.Red
  End Sub

  'Protected Sub uxSubmit_Click(ByVal sender As Object, ByVal e As System.EventArgs)
  '  'Dim localErrorTitle As String = MethodIdentifier() & " error: "
  '  'Try
  '  '  'uxUploadMessage.Text = "Nada"

  '  '  Dim result As String = ""
  '  '  Dim goodRes As Boolean = LoadGisXmlData(result)
  '  '  'uxUploadMessage.Text = "Result: " & result & "//END"

  '  'Catch ex As Exception
  '  '  'uxUploadMessage.Text = "File Upload error: " & ex.Message
  '  'End Try
  'End Sub

End Class
