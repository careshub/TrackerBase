﻿Option Explicit On
Option Strict On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports System.Transactions
Imports System.Web
Imports EH = ErrorHandler
Imports CommonFunctions
Imports CommonVariables 

Namespace TerLoc.Model

  Public Class TerraceReportRecord
    Private NULL_VALUE As Short = -1

    Public ObjectID As Long = NULL_VALUE
    Public Report As String = ""
    Public FortranDate As DateTime = Nothing
  End Class

  <Serializable()> _
  Public Class TerraceReportPackage
    Public terraceReportRecord As TerraceReportRecord
    Public datumRecord As ProjectDatum
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  <Serializable()> _
  Public Class TerraceReportPackageList
    Public terraceReports As New List(Of TerraceReportPackage)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class TerraceReportHelper
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private dataSchema As String = CommonVariables.ProjectProductionSchema
    Private workHelper As New Legacy.ProjWorkHelper

    ''' <summary>
    ''' Transfer terrace reports from fortran output.
    ''' </summary>
    Public Sub TransferTerraceReports(ByVal projectId As Long, ByRef callInfo As String)
      Dim debugInfo As String = ""
      Try
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId

        Dim localInfo As String = ""
        Dim workTable As DataTable = workHelper.GetProjectWorkTable(projectId, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        Dim pwr As Legacy.ProjWorkRecord
        Dim terraceReport As TerraceReportRecord
        Dim terrRecs As New List(Of TerraceReportRecord)
        Dim featureIds As New List(Of Legacy.TerraceFeature) From {Legacy.TerraceFeature.TERRACEREPORT}
        Dim workView As DataView
        Dim maxTerrDate As DateTime

        Dim filterExp As String = "FEATUREID = {0}"
        Dim filterExpFull As String = "FEATUREID = {0} AND TERRACEDATE = '{1}'"
        Dim sortExp As String = "TERRACEDATE DESC, SCENARIOTYPE, TYPETERRACE"

        For Each featureId As Integer In featureIds
          workView = New DataView(workTable, String.Format(filterExp, featureId), sortExp, DataViewRowState.OriginalRows)
          If workView.Count > 0 Then
            DateTime.TryParse(workView.Item(0).Item("TERRACEDATE").ToString, maxTerrDate)
          Else
            Continue For
          End If

          workView.RowFilter = String.Format(filterExpFull, featureId, maxTerrDate)
          debugInfo &= Environment.NewLine & featureId & " rows: " & workView.Count
          If workView.Count = 0 Then Continue For

          For terrIx As Integer = 0 To workView.Count - 1
            localInfo = ""
            pwr = workHelper.ExtractProjWorkRecordFromTableRow(workView(terrIx).Row, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            debugInfo &= Environment.NewLine & " featid: " & terrIx & " id: " & pwr.PROJWORKID

            localInfo = ""
            terraceReport = ConvertTerraceReport(pwr, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

            terrRecs = New List(Of TerraceReportRecord)
            terrRecs.Add(terraceReport)

            For Each terr As TerraceReportRecord In terrRecs
              localInfo = ""
              Insert(projectId, usrId, terr, localInfo)
              If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            Next
          Next
        Next

      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      End Try
      SendOzzy("Transfer Terrace Reports", debugInfo, Nothing)
    End Sub

    ''' <summary>
    ''' Convert legacy terrace error to terloc terrace error
    ''' </summary>
    Public Function ConvertTerraceReport(ByVal pwr As Legacy.ProjWorkRecord, ByRef callInfo As String) As TerraceReportRecord
      Dim retVal As New TerraceReportRecord
      Try
        With retVal
          .Report = pwr.FEATURECOORDS
          .FortranDate = pwr.TERRACEDATE
        End With
      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Delete"

    Public Function DeleteAll(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim numRecs As Integer = 0
      Try
        Dim allTerraceReports As DataTable = GetTerraceReportsTable(projectId, Nothing)
        Dim contIds As New List(Of Long)
        Dim contId As Long
        For contIx As Integer = 0 To allTerraceReports.Rows.Count - 1
          contId = NullSafeLong(allTerraceReports.Rows(contIx).Item("ObjectID"), -1)
          contIds.Add(contId)
        Next

        'If contIds.Count < 1 Then SendOzzy("TerraceReport " & ErrorHandler.GetCallerMethod, "no records", Nothing) ' ----- DEBUG
        If contIds.Count < 1 Then Return True

        Dim cmdIds = String.Join(",", contIds.ToArray)
        Dim cmdText As String = <a>
              DELETE FROM terloc.terloc.TABLENAME
              WHERE ObjectID IN (IDSTRING)
              </a>.Value.Replace("IDSTRING", cmdIds)

        ' SendOzzy("TerraceReport " & ErrorHandler.GetCallerMethod, cmdText & "    " & HtmlLineBreak & cmdIds, Nothing) ' ----- DEBUG
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using trans As SqlTransaction = conn.BeginTransaction
            Using cmd As SqlCommand = conn.CreateCommand()
              cmd.Transaction = trans
              Try
                'Should have cascade delete, but do both anyway
                cmd.CommandText = cmdText.Replace("TABLENAME", "TerraceReport")
                numRecs += cmd.ExecuteNonQuery

                cmd.CommandText = cmdText.Replace("TABLENAME", "ProjectDatum")
                numRecs += cmd.ExecuteNonQuery

                trans.Commit()
              Catch ex As Exception
                trans.Rollback()
                Throw
              End Try
            End Using
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("TerraceReportHelper DeleteAll error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#End Region

#Region "Fetch"

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As TerraceReportPackageList
      Dim retVal As New TerraceReportPackageList
      Dim terraceReports As New List(Of TerraceReportPackage)
      Dim localInfo As String = ""
      Try
        retVal = GetTerraceReports(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function GetTerraceReports(ByVal projectId As Long, ByRef callInfo As String) As TerraceReportPackageList
      Dim retVal As New TerraceReportPackageList
      Dim retTerraceReports As List(Of TerraceReportPackage)
      Dim retInfo As String = ""
      Dim localInfo As String = ""
      Try

        localInfo = ""
        retTerraceReports = GetTerraceReportsList(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        retVal.terraceReports = retTerraceReports
        retVal.info = retInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function GetTerraceReportsList(ByVal projectId As Long, ByRef callInfo As String) As List(Of TerraceReportPackage)
      Dim retVal As New List(Of TerraceReportPackage)
      Dim retTerraceReport As TerraceReportPackage = Nothing
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetTerraceReportsTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

          localInfo = ""
          features = UpdateNames(features, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw New Exception("TerraceReportsTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retTerraceReport = ExtractTerraceReportFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

            If retTerraceReport IsNot Nothing Then
              If retVal Is Nothing Then retVal = New List(Of TerraceReportPackage)
              retVal.Add(retTerraceReport)
            End If
          Catch ex As Exception
            Throw New Exception("TerraceReport (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    Public Function GetTerraceReportsTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>Select TOP 1 *   
              FROM {0}.TerraceReport as {1}   
              INNER JOIN {0}.ProjectDatum as {2} ON {2}.ObjectID = {1}.ObjectID 
              WHERE {2}.ProjectID = @projectId 
              ORDER BY {1}.FortranDate DESC
              </a>.Value, dataSchema, "FT", "PD")

        Dim parms As New List(Of SqlParameter)
        Dim parm As New SqlParameter("@projectId", SqlDbType.BigInt)
        parm.Value = projectId
        parms.Add(parm)

        localInfo = ""
        retVal = GetDataTable(dataConn, cmdText, parms.ToArray, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function ExtractTerraceReportFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As TerraceReportPackage
      Dim retVal As New TerraceReportPackage
      Dim feature As New TerraceReportRecord
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Dim tmpDateTime As DateTime
        Try
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .Report = NullSafeString(dr.Item("Report"), "")
            If DateTime.TryParse(NullSafeString(dr.Item("FortranDate"), ""), tmpDateTime) Then .FortranDate = tmpDateTime
          End With

        Catch ex As Exception
          Throw New Exception("TerraceReportRecord (" & callInfo & ")", ex)
        End Try

        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .terraceReportRecord = feature
          .datumRecord = datum
          .info = ""
        End With

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

#End Region

#Region "Insert"

    Public Function Insert(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featuredata As String, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Dim feature As New TerraceReportRecord
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of TerraceReportRecord)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (terraceReport deserialization): {1}", EH.GetCallerMethod(), ex.Message)
          Return Nothing
        End Try

        localInfo = ""
        retVal = Insert(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function Insert(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal feature As TerraceReportRecord, ByRef callInfo As String) As Long
      Dim localInfo As String = ""
      Dim datumId As Long = -1
      Try
        Using scope As New TransactionScope
          localInfo = ""
          datumId = ProjectDatumHelper.CreateNewProjectDatum(projectId, usrId, "", localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          If datumId <= 0 Then 'failure
            Throw New ArgumentOutOfRangeException("ObjectID", datumId, "New datum id was out of bounds.")
          End If
          feature.ObjectID = datumId

          localInfo = ""
          Insert(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

          scope.Complete()
        End Using

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return datumId
    End Function

    Private Sub Insert(ByVal feature As TerraceReportRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[Report] " & _
              ",[FortranDate] "

            Dim insertValues As String = "@ObjectID" & _
              ",@Report " & _
              ",@FortranDate "

            cmdText = "INSERT INTO " & dataSchema & ".TerraceReport (" & insertFields & ") Values (" & insertValues & ")"

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@Report", SqlDbType.NVarChar).Value = feature.Report
            cmd.Parameters.Add("@FortranDate", SqlDbType.DateTime).Value = feature.FortranDate

            cmd.CommandText = cmdText
            cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
        Throw
      End Try
    End Sub

#End Region

    Public Sub OpenTerraceReport(ByVal projectId As Long, ByRef callInfo As String)
      'Try

      'Catch ex As Exception
      '  callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      'End Try

      'Try
      '  If System.IO.File.Exists(Session(ZipFileForDownloadFullPathAndName & uxUploadedSessionID.InnerHtml)) Then
      '    Dim downloadZipFileName As String = System.IO.Path.GetFileName(Session(ZipFileForDownloadFullPathAndName & uxUploadedSessionID.InnerHtml))
      '    Response.ContentType = "application/x-zip-compressed"
      '    Response.AppendHeader("Content-Disposition", "attachment; filename=" & downloadZipFileName)
      '    Response.WriteFile(Session(ZipFileForDownloadFullPathAndName & uxUploadedSessionID.InnerHtml))
      '    Response.Flush()
      '    Response.Close()
      '  Else
      '    callInfo &= "Zip file does not exist for download. Try running your plans again."
      '  End If

      'Catch ex As System.Exception
      '  callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      'End Try
    End Sub

    Public Sub Duplicate(ByVal origProjectId As Long, ByVal newProjectId As Long, Optional ByRef callInfo As String = "")
      Dim localInfo As String = ""
      Try
        Dim feat As TerraceReportRecord
        Dim pkg As TerraceReportPackage
        Dim featureList As TerraceReportPackageList

        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        featureList = Fetch(origProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim origFeatId As Long
        Dim newFeatId As Long
        For Each pkg In featureList.terraceReports
          feat = pkg.terraceReportRecord
          origFeatId = feat.ObjectID

          localInfo = ""
          newFeatId = Insert(newProjectId, usrId, feat, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & "orig id " & origFeatId & ", new id " & newFeatId & ": " & localInfo
        Next
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

  End Class

End Namespace