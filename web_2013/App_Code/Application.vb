Option Strict On

#Region "Imports"
Imports System.Data.SqlClient
Imports EH = ErrorHandler
#End Region
 
Public Class Application

#Region "Module declarations"
   
  Private Shared dataConn As String = CommonFunctions.GetNetDatabaseConnString 
   
#End Region

  ''' <summary>
  ''' Get data from asp membership tables.
  ''' </summary>
  Public Shared Function GetUserData(ByVal s As String, Optional parms() As SqlParameter = Nothing) As String
    Dim output As Object = Nothing
    Try
      Using conn As New SqlConnection(dataConn)
        Using cmd As New SqlCommand(s, conn)
          If parms IsNot Nothing Then cmd.Parameters.AddRange(parms)
          cmd.Connection.Open()
          output = cmd.ExecuteScalar()
        End Using
      End Using
    Catch ex As Exception
      CommonFunctions.SendOzzy(EH.GetCallerMethod(), ex.ToString, Nothing)
    End Try
    If output IsNot Nothing Then Return output.ToString
    Return ""
  End Function

  ''' <summary>
  ''' Get data from asp membership tables.
  ''' </summary>
  Public Shared Function GetUserData(ByVal s As String, Optional parm As SqlParameter = Nothing) As String
    Dim output As Object = Nothing
    Try
      Using conn As New SqlConnection(dataConn)
        Using cmd As New SqlCommand(s, conn)
          If parm IsNot Nothing Then cmd.Parameters.Add(parm)
          cmd.Connection.Open()
          output = cmd.ExecuteScalar()
        End Using
      End Using
    Catch ex As Exception
      CommonFunctions.SendOzzy(EH.GetCallerMethod(), ex.ToString, Nothing)
    End Try
    If output IsNot Nothing Then Return output.ToString
    Return ""
  End Function

End Class
