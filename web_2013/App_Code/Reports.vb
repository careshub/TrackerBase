Imports Microsoft.VisualBasic
Imports CF = CommonFunctions 
Imports System.Data
Imports System.IO

''' <summary>
''' Return information about a report
''' </summary>
''' <remarks></remarks>
Public Class ReturnReport
  Public fileName As String = ""
  Public info As String = ""
End Class

Public Class Reports

  Public Shared Sub WriteHtmFile(ByVal html As String, ByVal fn As String, ByRef callInfo As String)
    Try
      Dim objStreamWriter As StreamWriter
      objStreamWriter = File.CreateText(fn)
      objStreamWriter.WriteLine(html)
      objStreamWriter.Close()
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", MethodIdentifier(), ex.ToString)
    End Try
  End Sub

  Private Shared Function MethodIdentifier() As String
    'Used for error message attributes (title)
    Try
      Return CF.FormatMethodIdentifier(System.Reflection.MethodBase.GetCurrentMethod.DeclaringType.Name, New System.Diagnostics.StackFrame(1).GetMethod().Name)
    Catch ex As Exception
      Return "GISTools MethodIdentifier didn't work"
    End Try
  End Function

End Class
