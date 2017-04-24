' ProjectDatum.vb - <Enter description>
' Created 9/20/2015 by AthertonK
' Copyright © 2015 Curators of the University of Missouri

Option Explicit On
Option Strict On

Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports EH = ErrorHandler
Imports CF = CommonFunctions

Namespace TerLoc.Model

  Public Class ProjectDatum
    Private Const NULL_VALUE As Short = -1

    Public ObjectID As Long = NULL_VALUE
    Public ProjectID As Long = NULL_VALUE
    Public Locked As Boolean = False
    Public Created As DateTime = DateTime.MinValue
    Public CreatorGuid As Guid = System.Guid.Empty
    Public Creator As String = String.Empty 'name of datum creator
    Public Edited As DateTime = DateTime.MinValue
    Public EditorGuid As Guid = System.Guid.Empty
    Public Editor As String = String.Empty 'name of last datum editor
    Public Notes As String = String.Empty
    Public GUID As Guid = System.Guid.Empty

  End Class

  Public Class ProjectDatumHelper

    Private Shared dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private Shared dataSchema As String = CommonVariables.ProjectProductionSchema

    ''' <summary>
    ''' Returns id of new project datum record.
    ''' </summary>
    Public Shared Function CreateNewProjectDatum(ByVal projectId As Long, ByVal usrId As Guid, _
                          ByVal notes As String, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Try
        If projectId < 1 Then Throw New ArgumentNullException("projectId")

        If notes IsNot Nothing Then notes = notes.ToString.Trim
        Dim insertFields As String = "ProjectId " & _
          ",[Created] " & _
          ",[CreatorGuid] " & _
          ",[Edited] " & _
          ",[EditorGuid] " & _
          ",[Notes] "

        Dim insertValues As String = "@ProjectId" & _
          ",@Created " & _
          ",@CreatorGuid " & _
          ",@Edited " & _
          ",@EditorGuid " & _
          ",@Notes "

        Dim cmdText As String = String.Format(<a>
                INSERT INTO {0}.ProjectDatum ( {1} ) Values ( {2} ) 
                SET @newOid = SCOPE_IDENTITY();
                                              </a>.Value, dataSchema, insertFields, insertValues)

        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = cmdText
            Dim newOidParameter As New SqlParameter("@newOid", System.Data.SqlDbType.BigInt)
            newOidParameter.Direction = System.Data.ParameterDirection.Output
            With cmd.Parameters
              .Add(newOidParameter)
              .Add("@ProjectId", SqlDbType.BigInt).Value = projectId
              .Add("@Created", SqlDbType.DateTime).Value = Now
              .Add("@CreatorGuid", SqlDbType.UniqueIdentifier).Value = usrId
              .Add("@Edited", SqlDbType.DateTime).Value = Now
              .Add("@EditorGuid", SqlDbType.UniqueIdentifier).Value = usrId
              .Add("@Notes", SqlDbType.NVarChar).Value = CF.NullSafeSqlString(notes)
            End With

            'Dim debug As String = cmdText.Replace("@ProjectId", cmd.Parameters.Item("@ProjectId").Value.ToString) _
            '                      .Replace("@Created", cmd.Parameters.Item("@Created").Value.ToString) _
            '                      .Replace("@CreatorGuid", cmd.Parameters.Item("@CreatorGuid").Value.ToString) _
            '                      .Replace("@Edited", cmd.Parameters.Item("@Edited").Value.ToString) _
            '                      .Replace("@EditorGuid", cmd.Parameters.Item("@EditorGuid").Value.ToString) _
            '                      .Replace("@Notes", cmd.Parameters.Item("@Notes").Value.ToString)
            'CF.SendOzzy("datum insert", debug, Nothing)

            If conn.State = ConnectionState.Closed Then conn.Open()
            Dim rowsInserted As Integer = cmd.ExecuteNonQuery()
            
            'callInfo &= Environment.NewLine & "error: datum rowsInserted " & rowsInserted ' ----- DEBUG
            'callInfo &= Environment.NewLine & "error: datum newOidParameter.Value " & newOidParameter.Value.ToString ' ----- DEBUG
             
            retVal = CF.NullSafeLong(newOidParameter.Value, -1)
          End Using
        End Using

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns number of IDs deleted from ProjectDatum table matching projectID.
    ''' </summary>
    Public Shared Function DeleteAllProjectDatumRecordsByProjectId(ByVal projectId As Long, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = "DELETE FROM " & dataSchema & ".ProjectDatum WHERE ProjectID=" & projectId
            If conn.State = ConnectionState.Closed Then conn.Open()
            retVal = cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    'TODO: rename to row
    ''' <summary>
    ''' Gets the project datum row for a given guid.
    ''' </summary> 
    Public Shared Function GetProjectDatumRecordByGuid(ByVal datumGuid As String, ByRef callInfo As String) As DataRow
      Dim retVal As DataRow = Nothing
      Dim localInfo As String = ""
      Try
        Dim cmdText As String
        Dim params As New List(Of SqlParameter)
        Dim param As SqlParameter

        cmdText = String.Format(<a>SELECT * FROM {0}.ProjectDatum WHERE [GUID]=@guid</a>.Value, dataSchema)

        param = New SqlParameter("@guid", SqlDbType.UniqueIdentifier)
        param.Value = New Guid(datumGuid)
        params.Add(param)

        Dim featTbl As DataTable = CF.GetDataTable(dataConn, cmdText, params.ToArray, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        If featTbl.Rows.Count > 0 Then retVal = featTbl.Rows(0)

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    'TODO: rename to row
    ''' <summary>
    ''' Delete row from ProjectDatum table matching ObjectId.
    ''' </summary>
    Public Shared Function DeleteProjectDatumRecord(ByVal datumID As Integer, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = "DELETE FROM " & dataSchema & ".ProjectDatum WHERE ObjectId=" & datumID
            If conn.State = ConnectionState.Closed Then conn.Open()
            retVal = cmd.ExecuteNonQuery()
          End Using
        End Using

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns all Ids from ProjectDatum table matching ProjectId.
    ''' </summary>
    Public Shared Function GetProjectDatumObjectIdsByProjectId(ByVal projectId As Long, ByRef callInfo As String) As Integer()
      Dim retVal As New Generic.List(Of Integer)
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = "SELECT ObjectID FROM " & dataSchema & ".ProjectDatum WHERE ProjectID=" & projectId
            If conn.State = ConnectionState.Closed Then conn.Open()

            Dim readr As SqlDataReader = cmd.ExecuteReader
            While readr.Read
              Try
                retVal.Add(CF.NullSafeInteger(readr(0), -1))
              Catch ex As Exception 'error if cant convert to integer
                retVal.Add(-1)
              End Try
            End While
          End Using
        End Using

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal.ToArray
    End Function

    ''' <summary>
    ''' Returns first Id from ProjectDatum table matching ProjectId.
    ''' </summary>
    Public Shared Function GetProjectDatumObjectIdByProjectId(ByVal projectId As Long, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = "SELECT ObjectID FROM " & dataSchema & ".ProjectDatum WHERE ProjectID=" & projectId
            If conn.State = ConnectionState.Closed Then conn.Open()

            Dim readr As SqlDataReader = cmd.ExecuteReader
            While readr.Read
              Try
                retVal = CInt(readr(0))
              Catch ex As Exception 'error if cant convert to integer
                retVal = -1
              End Try
            End While
          End Using
        End Using

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns project datum information by datum Guid.
    ''' </summary>
    Public Shared Function GetProjectDatumInfoByGuid(ByVal datumGuid As String, ByRef callInfo As String) As TerLoc.Model.ProjectDatum
      Dim retVal As TerLoc.Model.ProjectDatum = Nothing
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = String.Format("SELECT * FROM {0}.ProjectDatum WHERE [GUID]=@guid", dataSchema, datumGuid)
            cmd.Parameters.Add(New SqlParameter("@guid", SqlDbType.UniqueIdentifier)).Value = New Guid(datumGuid)
            If conn.State = ConnectionState.Closed Then conn.Open()

            Dim tmpStr As String
            Dim tmpGuid As Guid
            Dim readr As SqlDataReader = cmd.ExecuteReader
            While readr.Read
              Try
                retVal = New TerLoc.Model.ProjectDatum
                With retVal
                  .ObjectID = CF.NullSafeInteger(readr("ObjectID"), -1)
                  .ProjectID = CF.NullSafeInteger(readr("ProjectId"), -1)
                  .Locked = CF.NullSafeBoolean(readr("Locked"))
                  .Created = CDate(CF.NullSafeString(readr("Created")))
                  tmpStr = CF.NullSafeString(readr("CreatorGuid"))
                  If Not String.IsNullOrWhiteSpace(tmpStr) AndAlso Guid.TryParse(tmpStr, tmpGuid) Then
                    .CreatorGuid = tmpGuid
                  End If
                  .Edited = CDate(CF.NullSafeString(readr("Edited")))
                  tmpStr = CF.NullSafeString(readr("EditorGuid"))
                  If Not String.IsNullOrWhiteSpace(tmpStr) AndAlso Guid.TryParse(tmpStr, tmpGuid) Then
                    .EditorGuid = tmpGuid
                  End If
                  .Notes = CF.NullSafeString(readr("Notes"))
                End With
              Catch ex As Exception
                callInfo &= String.Format("{0} read error: {1}", EH.GetCallerMethod(), ex.Message)
              End Try
            End While
          End Using
        End Using

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Extract project datum info for datarow.
    ''' </summary>
    Public Shared Function ExtractFromRow(ByVal dr As DataRow, ByRef callInfo As String) As ProjectDatum
      Dim retVal As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Dim allUsers As UserList = UserHelper.Fetch(localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .ObjectID = CF.NullSafeLong(dr.Item("ObjectID"), -1)
          .ProjectID = CF.NullSafeLong(dr.Item("ProjectID"), -1)
          .Created = CF.NullSafeDateTime(dr.Item("Created"))
          .CreatorGuid = CF.NullSafeGuid(dr.Item("CreatorGuid"))
          .Edited = CF.NullSafeDateTime(dr.Item("Edited"))
          .EditorGuid = CF.NullSafeGuid(dr.Item("EditorGuid"))
          .Notes = CF.NullSafeString(dr.Item("Notes"), "")
          .GUID = CF.NullSafeGuid(dr.Item("GUID"))

          .Creator = UserHelper.GetUserFullNameByUserId(allUsers.Users, .CreatorGuid, localInfo)
          .Editor = UserHelper.GetUserFullNameByUserId(allUsers.Users, .EditorGuid, localInfo)
        End With
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

  End Class

End Namespace