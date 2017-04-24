
Imports System.Diagnostics
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Web.HttpContext
Imports CF = CommonFunctions

Public Class ErrorHandler

  Private Shared noReplyEmail As String = "no-reply@" & CF.GetSiteName
  Private Shared myEmail As String = "athertonk@missouri.edu"

  Private Shared Function UserInfo() As String
    Dim retVal As String = "<br />--------------------------------------------------------<br />"
    If Current.Request.IsAuthenticated Then
      retVal += "Username: " & Current.User.Identity.Name & "<br />"
    Else
      Dim sessionUserName As String = CommonVariables.SessionUserName
      retVal += "Username: " & CF.NullSafeString(Current.Session(sessionUserName)) & "<br />"
    End If
    retVal &= "Browser: " & Replace(Current.Request.UserAgent, "Mozilla/", "") & "<br />"
    retVal &= "IP: " & Current.Request.ServerVariables("REMOTE_ADDR")
    Return retVal
  End Function

  Public Shared Sub Alert(ByVal usr As String, ByVal project As String, ByVal errorInfo As String, ByVal method As String)
    Dim subject As String = method
    Dim body As String = ""
    body &= usr & " for " & project & "<br />"
    body &= "error:<br />"
    body &= errorInfo & "<br />"

    body &= UserInfo()

    CF.SendEmail(myEmail, noReplyEmail, method, body, Nothing)
  End Sub

  'Public Shared Function GetCallerMethod(ByVal caller As String) As String
  '  Return CF.FormatMethodIdentifier(caller, New System.Diagnostics.StackFrame(1).GetMethod().Name)

  '  'Dim st As StackTrace = New StackTrace()
  '  'Dim sf As StackFrame = st.GetFrame(1)
  '  'Dim mb As MethodBase = sf.GetMethod()
  '  'Return mb.Name
  'End Function

  Public Shared Function GetCallerMethod() As String
    Dim st As StackTrace = New StackTrace()
    Dim sf As StackFrame = st.GetFrame(1)
    Dim fi As String = sf.GetFileName()
    Dim mb As MethodBase = sf.GetMethod()
    Return fi & ":" & mb.Name
  End Function

End Class
