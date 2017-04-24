' HelperBase.vb - Service base for helper modules
' Created 9/20/2015 by AthertonK
' Copyright © 2015 Curators of the University of Missouri

Option Explicit On
Option Strict On

Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace TerLoc.Services
  Class HelperBase
    Private EPOCH As Date = New Date(1970, 1, 1, 0, 0, 0, 0)

    Protected Function Read(Reader As SqlDataReader, Column As Integer, Def As Guid) As Guid
      If Reader.IsDBNull(Column) Then
        Return Def
      End If
      Return Reader.GetGuid(Column)
    End Function

    Protected Function Read(Reader As SqlDataReader, Column As Integer, Def As String) As String
      If Reader.IsDBNull(Column) Then
        Return Def
      End If
      Return Reader.GetString(Column)
    End Function

    Protected Function Read(Reader As SqlDataReader, Column As Integer, Def As Integer) As Integer
      If Reader.IsDBNull(Column) Then
        Return Def
      End If
      Return CInt(Reader.GetValue(Column))
    End Function

    Protected Function Read(Reader As SqlDataReader, Column As Integer, Def As Long) As Long
      If Reader.IsDBNull(Column) Then
        Return Def
      End If
      Return CLng(Reader.GetValue(Column))
    End Function

    Protected Function Read(Reader As SqlDataReader, Column As Integer, Def As DateTime) As DateTime
      If Reader.IsDBNull(Column) Then
        Return Def
      End If
      Return Reader.GetDateTime(Column)
    End Function

    Protected Function ReadDate(Reader As SqlDataReader, Column As Integer, Def As Long) As Long
      If Reader.IsDBNull(Column) Then
        Return Def
      End If
      Return DateAsMillis(CDate(Reader.GetDateTime(Column)))
    End Function

    Private Function DateAsMillis(Value As Date) As Long
      Dim span As TimeSpan = Value.Subtract(EPOCH)
      Return CLng(span.TotalMilliseconds)
    End Function

    Protected Function DateFromMillis(Value As Long) As Date
      Const TICKS_PER_MS As Long = 10000
      Dim ticks As Long = Value * TICKS_PER_MS
      Dim span As TimeSpan = New TimeSpan(ticks)
      Return EPOCH.Add(span)
    End Function

    Protected Function Read(Reader As SqlDataReader, Column As Integer, Def As Single) As Single
      If Reader.IsDBNull(Column) Then
        Return Def
      End If
      Return CSng(Reader.GetValue(Column))
    End Function

    Protected Function AddParameter(Cmd As SqlCommand, Name As String, Value As Guid) As SqlParameter
      Return AddParameter(Cmd, Name, Value, SqlDbType.UniqueIdentifier)
    End Function

    Protected Function AddParameter(Cmd As SqlCommand, Name As String, Value As Integer) As SqlParameter
      Return AddParameter(Cmd, Name, Value, SqlDbType.Int)
    End Function

    Protected Function AddParameter(Cmd As SqlCommand, Name As String, Value As Single) As SqlParameter
      Return AddParameter(Cmd, Name, Value, SqlDbType.Real)
    End Function

    Protected Function AddParameter(Cmd As SqlCommand, Name As String, Value As Date) As SqlParameter
      Return AddParameter(Cmd, Name, Value, SqlDbType.DateTime)
    End Function

    Protected Function AddParameter(Cmd As SqlCommand, Name As String, Value As Long) As SqlParameter
      Return AddParameter(Cmd, Name, Value, SqlDbType.BigInt)
    End Function

    Protected Function AddParameter(Cmd As SqlCommand, Name As String, Value As Object, Type As SqlDbType) As SqlParameter
      Dim param As SqlParameter = New SqlParameter(Name, Type)
      param.Value = Value
      Cmd.Parameters.Add(param)
      Return param
    End Function

  End Class
End Namespace
