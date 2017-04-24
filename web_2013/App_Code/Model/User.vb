Option Explicit On
Option Strict On

Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports EH = ErrorHandler
Imports CF = CommonFunctions
Imports CV = CommonVariables

Namespace TerLoc.Model

  Public Class User
    Public UserId As Guid = Guid.Empty
    Public UserName As String = ""
    Public FirstName As String = ""
    Public LastName As String = ""
    Public DisplayName As String = ""
    Public Email As String = ""
  End Class

  Public Class UserList
    Public Users As New List(Of User)
  End Class

  Public Class UserHelper

    Private Shared dataConn As String = CF.GetBaseDatabaseConnString
    Private Shared dataSchema As String = CV.ProjectProductionSchema
    Private Shared aspConn As String = CF.GetNetDatabaseConnString

    Public Function Delete(ByVal usrId As Guid, ByRef callInfo As String) As Boolean
      Return False
    End Function

#Region "Fetch"

    ''' <summary>
    ''' Returns User info from user table(s)
    ''' </summary>
    Public Shared Function GetCurrentUser(ByRef callInfo As String) As User
      Dim retVal As New User
      Dim localInfo As String = ""
      Try
        Dim features As DataTable
        Dim name As String = HttpContext.Current.User.Identity.Name
         
        Try
          localInfo = ""
          features = GetTable(localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          Throw New Exception("GetTable (" & callInfo & ")", ex)
        End Try

        If features Is Nothing OrElse features.Rows.Count < 1 Then
          callInfo &= "GetCurrentUser error: no rows found."
          CF.SendOzzy(EH.GetCallerMethod(), "GetCurrentUser error: no rows found.", Nothing)
          Return retVal
        End If

        Dim found() As DataRow = features.Select("UserName = '" & name & "'")
        If found.Count > 0 Then
          Try 
            localInfo = ""
            retVal = ExtractFromRow(found(0), localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo 
          Catch ex As Exception
            Throw New Exception("Extract (" & callInfo & ")", ex)
          End Try
        Else
          callInfo &= "GetCurrentUser error: " & name & " not found."
        End If

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
        CF.SendOzzy(EH.GetCallerMethod(), ex.ToString, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Return a user with the input guid.
    ''' </summary>
    Public Shared Function Fetch(ByVal guid As Guid, ByRef callInfo As String) As User
      Dim retVal As New User
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetTable(localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          Throw New Exception("GetTable (" & callInfo & ")", ex)
        End Try

        Dim found() As DataRow = features.Select("UserId = '" & guid.ToString & "'")
        If found.Count > 0 Then
          Try
            localInfo = ""
            retVal = ExtractFromRow(found(0), localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          Catch ex As Exception
            Throw New Exception("Extract (" & callInfo & ")", ex)
          End Try
        End If

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Return a user with the input user name.
    ''' </summary>
    Public Shared Function Fetch(ByVal name As String, ByRef callInfo As String) As User
      Dim retVal As New User
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetTable(localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          Throw New Exception("GetTable (" & callInfo & ")", ex)
        End Try

        Dim found() As DataRow = features.Select("UserName = '" & name & "'")
        If found.Count > 0 Then
          Try
            localInfo = ""
            retVal = ExtractFromRow(found(0), localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          Catch ex As Exception
            Throw New Exception("Extract (" & callInfo & ")", ex)
          End Try
        End If

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Return a list of application users.
    ''' </summary>
    Public Shared Function Fetch(ByRef callInfo As String) As UserList
      Dim retVal As New UserList
      Dim usrs As New List(Of User)
      Dim usr As New User
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetTable(localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          Throw New Exception("User Table (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            usr = ExtractFromRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          Catch ex As Exception
            Throw New Exception("User (" & callInfo & ")", ex)
          End Try
          If usr IsNot Nothing Then usrs.Add(usr)
        Next

        retVal.Users = usrs

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Get all site users.
    ''' </summary>
    Private Shared Function GetTable(ByRef callInfo As String) As DataTable
      Dim retVal As New DataTable
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        Dim dbName As String = CF.GetAspNetDatabaseName()
        Dim usrTable As String = CV.AppUserTable
        Dim guid As Guid = CV.AppGuid
        cmdText = String.Format(<a>
            SELECT NET.[UserId], NET.UserName, APP.[FirstName], APP.[LastName], APP.[DisplayName], MEM.Email
            FROM {0}.[dbo].[aspnet_Users] NET
            INNER JOIN {0}.{1} APP ON APP.UserId = NET.UserId
            INNER JOIN {0}.[dbo].[aspnet_Membership] MEM ON MEM.UserId = NET.UserId
            WHERE MEM.ApplicationId = @appId 
                              </a>.Value, dbName, usrTable)
        Dim parameter As New SqlParameter("@appId", SqlDbType.UniqueIdentifier)
        parameter.Value = guid

        'callInfo &= Environment.NewLine & "error: "
        'callInfo &= "  dbName: " & dbName
        'callInfo &= "  usrTable: " & usrTable
        'callInfo &= "  guid: " & guid.ToString

        localInfo = ""
        retVal = CF.GetDataTable(aspConn, cmdText, parameter, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Private Shared Function ExtractFromRow(ByVal dr As DataRow, ByRef callInfo As String) As User
      Dim retVal As New User
      Dim localInfo As String = ""
      Try
        With retVal
          .UserId = CF.NullSafeGuid(dr.Item("UserId"))
          .UserName = CF.NullSafeString(dr.Item("UserName"), "")
          .FirstName = CF.NullSafeString(dr.Item("FirstName"), "")
          .LastName = CF.NullSafeString(dr.Item("LastName"), "")
          .DisplayName = CF.NullSafeString(dr.Item("DisplayName"), "")
          .Email = CF.NullSafeString(dr.Item("Email"), "")
        End With
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns full user name.
    ''' </summary>
    Public Shared Function GetUserFullName(ByVal usr As User, ByRef callInfo As String) As String
      Dim retVal As String = ""
      Try
        Return usr.FirstName & " " & usr.LastName
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(), ex.Message)
        CF.SendOzzy(EH.GetCallerMethod(), ex.ToString, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns full user name.
    ''' </summary>
    Public Shared Function GetUserFullNameByUserId(ByVal users As List(Of User), ByVal usrId As Guid, ByRef callInfo As String) As String
      Dim retVal As String = ""
      Try
        For Each U As User In users
          If U.UserId = usrId Then
            Return U.FirstName & " " & U.LastName
          End If
        Next
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

#End Region

    '#Region "User from CommonFunctions"

    '    ''' <summary>
    '    ''' Returns User info from user table(s)
    '    ''' </summary>
    '    Public Shared Sub GetCurrentUserInfo(ByRef usrName As String, ByRef usrGuid As String, ByRef callInfo As String)
    '      Dim localInfo As String = ""
    '      Try
    '        usrName = HttpContext.Current.User.Identity.Name

    '        Dim allUsers As DataTable = GetSiteUsers(localInfo)
    '        If localInfo.Contains("error") Then callInfo &= localInfo

    '        Dim found() As DataRow = allUsers.Select("UserName = '" & usrName & "'")
    '        If found.Count = 0 Then Return

    '        usrGuid = NullSafeString(found(0).Item("UserId"), "")

    '      Catch ex As Exception
    '        callInfo &= String.Format("{0} error: {1}", MethodIdentifier, ex.Message)
    '      End Try
    '    End Sub

    '    ''' <summary>
    '    ''' Returns User email from supplemental user table (non-.net)
    '    ''' </summary>
    '    Public Shared Function GetUserEmailByName(ByVal usrName As String, ByRef callInfo As String) As String
    '      Dim retVal As String = ""
    '      Try
    '        Using conn As New SqlConnection(aspDataConnStr)
    '          Using thisCommand As SqlCommand = conn.CreateCommand()
    '            thisCommand.CommandText = "SELECT Email " & _
    '                " FROM [mmptrackerdev].[dbo].[aspnet_Users] as U " & _
    '                " inner join [mmptrackerdev].[dbo].[aspnet_Membership] as M " & _
    '                " on U.UserId = M.UserId " & _
    '                " where U.UserName = @usrName "

    '            Dim prm As New SqlParameter("@usrName", usrName)
    '            thisCommand.Parameters.Add(prm)

    'If conn.State = ConnectionState.Closed then conn.Open()

    '            Dim readr As SqlDataReader = thisCommand.ExecuteReader
    '            While readr.Read
    '              Try
    '                retVal = NullSafeString(readr(0))
    '              Catch ex As Exception
    '                retVal = ""
    '              End Try
    '            End While
    '          End Using
    '        End Using

    '      Catch ex As Exception
    '        callInfo &= String.Format("{0} error: {1}", MethodIdentifier, ex.Message)
    '      End Try
    '      Return retVal
    '    End Function

    '    ''' <summary>
    '    ''' Returns User id from supplemental user table (non-.net)
    '    ''' </summary>
    '    ''' <param name="usrName">If null or whitespace, will use HttpContext.Current.User.Identity.Name</param>
    '    Public Shared Function GetUserIdByName(ByVal usrName As String, ByRef callInfo As String) As Int32
    '      Dim retVal As Int32 = -1
    '      Try
    '        If String.IsNullOrWhiteSpace(usrName) Then usrName = HttpContext.Current.User.Identity.Name
    '        Dim usrTable As String = CV.AppUserTable
    '        Using conn As New SqlConnection(aspDataConnStr)
    '          Using cmd As SqlCommand = conn.CreateCommand()
    '            cmd.CommandText = String.Format("SELECT UserID FROM {0} WHERE Username= @usrName", usrTable)
    '            SendOzzy(usrName, conn.ConnectionString & "    " & Environment.NewLine & cmd.CommandText, Nothing)
    '            Dim prm As New SqlParameter("@usrName", usrName)
    '            cmd.Parameters.Add(prm)

    '            If conn.State = ConnectionState.Closed then conn.Open()

    '            Dim readr As SqlDataReader = cmd.ExecuteReader
    '            While readr.Read
    '              Try
    '                retVal = CInt(readr(0))
    '              Catch ex As Exception
    '                retVal = -1
    '              End Try
    '            End While
    '          End Using
    '        End Using

    '      Catch ex As Exception
    '        callInfo &= String.Format("{0} error: {1}", MethodIdentifier, ex.Message)
    '      End Try
    '      Return retVal
    '    End Function

    '    ''' <summary>
    '    ''' Returns User Guid from .net user table
    '    ''' </summary>
    '    Public Shared Function GetUserGuid(ByVal usrName As String, ByRef callInfo As String) As String
    '      Dim retVal As String = ""
    '      Try
    '        Dim usr As System.Web.Security.MembershipUser = Nothing
    '        If String.IsNullOrWhiteSpace(usrName) Then
    '          usr = Membership.GetUser()
    '        Else
    '          usr = Membership.GetUser(usrName, False)
    '        End If
    '        If usr IsNot Nothing Then retVal = usr.ProviderUserKey.ToString()
    '      Catch ex As Exception
    '        callInfo &= String.Format("{0} error: {1}", MethodIdentifier, ex.Message)
    '      End Try
    '      Return retVal
    '    End Function

    '    ''' <summary>
    '    ''' Returns full user name from supplemental user table (non-.net)
    '    ''' </summary>
    '    Public Shared Function GetUserFullNameByUserId(ByVal usrId as guid, ByRef callInfo As String) As String
    '      Dim retVal As String = ""
    '      Try
    '        Dim usrTable As String = CV.AppUserTable
    '        Using conn As New SqlConnection(aspDataConnStr)
    '          Using cmd As SqlCommand = conn.CreateCommand()
    '            cmd.CommandText = String.Format("SELECT [FirstName] + ' ' + [LastName] AS FullName FROM {0} WHERE [UserID]= @usrId", usrTable)
    '            Dim prm As New SqlParameter("@usrId", usrId)
    '            cmd.Parameters.Add(prm)

    'If conn.State = ConnectionState.Closed then conn.Open()

    '            Dim readr As SqlDataReader = cmd.ExecuteReader
    '            While readr.Read
    '              Try
    '                retVal = NullSafeString(readr("FullName"))
    '              Catch ex As Exception
    '              End Try
    '            End While
    '          End Using
    '        End Using

    '      Catch ex As Exception
    '        callInfo &= String.Format("{0} error: {1}", MethodIdentifier, ex.Message)
    '      End Try
    '      Return retVal
    '    End Function

    '#End Region

  End Class

End Namespace
