Option Explicit On
Option Strict On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports System.Web.Script.Serialization
Imports System.Transactions
Imports EH = ErrorHandler
Imports CommonFunctions
Imports CommonVariables
Imports GIS.GISToolsAddl
Imports GGeom = GeoAPI.Geometries

Namespace TerLoc.Model

  Public Class TerraceRecord
    Private NULL_VALUE As Short = -1

    Public ObjectID As Long = NULL_VALUE
    Public FeatureID As Integer = NULL_VALUE
    Public Shape As String = ""
    Public Type As String = ""
    Public FortranDate As DateTime = Nothing
    Public ScenarioType As Integer = NULL_VALUE
    Public CostShareID As Integer = NULL_VALUE
    Public Ordinal As Integer = 1
    Public Custom As Boolean = False
  End Class

  Public Class TerraceFull
    Inherits TerraceRecord

    Public Coords As String
    Public Length As Double
  End Class

  <Serializable()> _
  Public Class TerracePackage
    Public terraceRecord As TerraceFull
    Public datumRecord As ProjectDatum
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  <Serializable()> _
  Public Class TerracePackageList
    Public terraces As New List(Of TerracePackage)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  'Attempt at using types, but probably delete this.
  '<Serializable()> _
  'Public Class TerraceTypePackage
  '  Public terraceType As String = ""
  '  Public terraces As New List(Of TerracePackage)
  'End Class

  '<Serializable()> _
  'Public Class TerraceTypePackageList
  '  Public terracesByType As New List(Of TerraceTypePackage)
  'End Class

  Public Class TerraceHelper
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private dataSchema As String = CommonVariables.ProjectProductionSchema
    Private workHelper As New Legacy.ProjWorkHelper

    ''' <summary>
    ''' Transfer terraces from fortran output.
    ''' </summary>
    Public Sub TransferTerraces(ByVal projectId As Long, ByRef callInfo As String)
      Dim debugInfo As String = ""
      Try
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId

        Dim localInfo As String = ""
        Dim workTable As DataTable = workHelper.GetProjectWorkTable(projectId, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        Dim pwr As Legacy.ProjWorkRecord
        Dim terrace As TerraceFull
        Dim terrRecs As New List(Of TerraceFull)
        Dim featureIds As New List(Of Legacy.TerraceFeature) From {Legacy.TerraceFeature.CONVENTIONALTERRACE, _
                                                                   Legacy.TerraceFeature.CUSTOMTERRACE, _
                                                                   Legacy.TerraceFeature.KEYTERRACE, _
                                                                   Legacy.TerraceFeature.PARALLELTERRACE}
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

          ' If keeping old ones, use this.
          'localInfo = "" 
          'DeleteByTerraceDate(projectId, usrId, maxTerrDate, localInfo)
          'If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

          For terrIx As Integer = 0 To workView.Count - 1
            localInfo = ""
            pwr = workHelper.ExtractProjWorkRecordFromTableRow(workView(terrIx).Row, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            debugInfo &= Environment.NewLine & " featid: " & terrIx & " id: " & pwr.PROJWORKID

            localInfo = ""
            terrace = ConvertTerrace(pwr, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

            Dim terrCoords As String() = terrace.Coords.Split(CommonVariables.pipeSeparator, StringSplitOptions.RemoveEmptyEntries)

            debugInfo &= Environment.NewLine & " coords: " & terrCoords.Count
            terrRecs = New List(Of TerraceFull)
            If terrCoords.Count > 1 Then
              localInfo = ""
              terrRecs = SplitTerraces(terrace, localInfo)
              If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            Else
              terrRecs.Add(terrace)
            End If
            debugInfo &= Environment.NewLine & " terrRecs: " & terrRecs.Count

            For Each terr As TerraceFull In terrRecs
              debugInfo &= Environment.NewLine & " orig terr: " & terr.Coords.Substring(0, 40)
              localInfo = ""
              terr.Coords = Fortran.DeFormatFortranCoords(terr.Coords, localInfo)
              If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

              debugInfo &= Environment.NewLine & " deformat terr: " & terr.Coords.Substring(0, 40)
              localInfo = ""
              terr.Coords = GIS.GISToolsAddl.ConvertUtmCoordsToLatLon(terr.Coords, 15, localInfo)
              If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

              debugInfo &= Environment.NewLine & " latlon terr: " & terr.Coords.Substring(0, 40)
              localInfo = ""
              CalcTerrace(terr, localInfo)
              If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

              debugInfo &= Environment.NewLine & " post-calc terr: " & " len: " & terr.Length & "  " & localInfo
              localInfo = ""
              Insert(projectId, usrId, terr, localInfo)
              If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
              debugInfo &= Environment.NewLine & " post-insert terr: " & localInfo
            Next
          Next
        Next

      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      End Try
      SendOzzy("Transfer terraces", debugInfo, Nothing)
    End Sub

    ''' <summary>
    ''' Convert legacy terrace to terloc terrace
    ''' </summary>
    Public Function ConvertTerrace(ByVal pwr As Legacy.ProjWorkRecord, ByRef callInfo As String) As TerraceFull
      Dim retVal As New TerraceFull
      Try
        With retVal
          .FeatureID = pwr.FEATUREID
          .Type = pwr.TYPETERRACE
          .FortranDate = pwr.TERRACEDATE
          .ScenarioType = pwr.SCENARIOTYPE
          .CostShareID = CostShareHelper.GetCostShareIdByCost(pwr.COSTSHARE) 
          .Coords = pwr.FEATURECOORDS
          .Ordinal = 1
        End With
      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Split legacy terrace into multiple terloc terraces.
    ''' </summary>
    Public Function SplitTerraces(ByVal terrace As TerraceFull, ByRef callInfo As String) As List(Of TerraceFull)
      Dim retVal As New List(Of TerraceFull)
      Dim terrRec As TerraceFull
      Try
        Dim terrCoords As String() = terrace.Coords.Split(CommonVariables.pipeSeparator, StringSplitOptions.RemoveEmptyEntries)
        Dim numTerrs As Integer = terrCoords.Count
        For terrIx As Integer = 0 To numTerrs - 1
          terrRec = New TerraceFull
          With terrRec
            .FeatureID = terrace.FeatureID
            .Type = terrace.Type
            .FortranDate = terrace.FortranDate
            .ScenarioType = terrace.ScenarioType
            .CostShareID = terrace.CostShareID 
            .Coords = terrCoords(terrIx)
            .Ordinal = terrIx + 1
            .Custom = False
          End With
          retVal.Add(terrRec)
        Next
      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Update terrace cost in database.
    ''' </summary>
    Public Sub UpdateTerraceCost(ByVal projectId As Long, ByVal featureId As Long, ByVal costId As Integer, ByRef callInfo As String)
      Dim localInfo As String = ""
      Dim pkgs As TerracePackageList
      Try
        Dim costShares As List(Of CostShare) = CostShareHelper.Fetch(localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        localInfo = ""
        pkgs = Me.Fetch(projectId, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        Dim pkg As TerracePackage = pkgs.terraces.Where(Function(x) x.terraceRecord.ObjectID = featureId).First()
        pkg.terraceRecord.CostShareID = costId

        localInfo = ""
        UpdateTerraceCostToDatabase(pkg.terraceRecord, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        'For Each pkg As TerracePackage In pkgs.terraces
        '  If pkg.terraceRecord.ObjectID <> featureId Then Continue For
        '  Dim terrace As TerraceFull = pkg.terraceRecord
        '  localInfo = ""
        '  CalcTerrace(terrace, localInfo)
        '  If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        '  terrace.CostShareID = costId 

        '  localInfo = ""
        '  UpdateTerraceCostToDatabase(terrace, localInfo)
        '  If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
        'Next
      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    ''' <summary>
    ''' Update custom terrace in database.
    ''' </summary>
    Public Sub UpdateTerraceCustom(ByVal projectId As Long, ByVal featureIds As List(Of Long), ByRef callInfo As String)
      Dim localInfo As String = ""
      Dim pkgs As TerracePackageList
      Dim feature As TerraceFull
      Try
        Dim costShares As List(Of CostShare) = CostShareHelper.Fetch(localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        localInfo = ""
        pkgs = Me.Fetch(projectId, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
         
        For Each pkg As TerracePackage In pkgs.terraces
          feature = pkg.terraceRecord
          If featureIds.IndexOf(feature.ObjectID) < 0 Then
            feature.Custom = False
          Else
            feature.Custom = True
          End If
            
          localInfo = ""
          UpdateTerraceCustomToDatabase(feature, localInfo)
          If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
        Next
      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

#Region "Edit"

    Public Sub UpdateTerraceCostToDatabase(ByVal feature As TerraceFull, ByRef callInfo As String) 
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            Dim flds As String() = New String() {"[CostShareID] "}
            Dim vals As String() = New String() {"@CostShareID "}
            Dim sql As New StringBuilder("UPDATE ")
             
            Try
              sql.Append("" & dataSchema & ".Terrace ")
              If flds.Length > 0 Then
                sql.Append(" SET ")
                For i As Integer = 0 To flds.Length - 1
                  sql.Append(flds(i) & "=")
                  sql.Append(vals(i))
                  If i <> flds.Length - 1 Then sql.Append(", ")
                Next
              End If
              sql = New StringBuilder(sql.ToString.TrimEnd(","c))

              sql.Append(" WHERE ObjectID = @ObjectID")

            Catch ex As Exception
              callInfo &= EH.GetCallerMethod() & " sql creation error: " & ex.Message
              Return
            End Try

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@CostShareID", SqlDbType.Money).Value = feature.CostShareID

            cmd.CommandText = sql.ToString
            If conn.State = ConnectionState.Closed Then conn.Open()
            cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Public Sub UpdateTerraceCustomToDatabase(ByVal feature As TerraceFull, ByRef callInfo As String)
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            Dim flds As String() = New String() {"[Custom] "}
            Dim vals As String() = New String() {"@Custom "}
            Dim sql As New StringBuilder("UPDATE ")

            Try
              sql.Append("" & dataSchema & ".Terrace ")
              If flds.Length > 0 Then
                sql.Append(" SET ")
                For i As Integer = 0 To flds.Length - 1
                  sql.Append(flds(i) & "=")
                  sql.Append(vals(i))
                  If i <> flds.Length - 1 Then sql.Append(", ")
                Next
              End If
              sql = New StringBuilder(sql.ToString.TrimEnd(","c))

              sql.Append(" WHERE ObjectID = @ObjectID")

            Catch ex As Exception
              callInfo &= EH.GetCallerMethod() & " sql creation error: " & ex.Message
              Return
            End Try

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@Custom", SqlDbType.Bit).Value = feature.Custom

            cmd.CommandText = sql.ToString
            If conn.State = ConnectionState.Closed Then conn.Open()
            cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

#End Region

#Region "Delete"

    Public Function DeleteAll(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim numRecs As Integer = 0
      Try
        Dim allTerraces As DataTable = GetTerracesTable(projectId, Nothing)
        Dim contIds As New List(Of Long)
        Dim contId As Long
        For contIx As Integer = 0 To allTerraces.Rows.Count - 1
          contId = NullSafeLong(allTerraces.Rows(contIx).Item("ObjectID"), -1)
          contIds.Add(contId)
        Next

        'If contIds.Count < 1 Then SendOzzy("Terrace " & ErrorHandler.GetCallerMethod, "no records", Nothing) ' ----- DEBUG
        If contIds.Count < 1 Then Return True

        Dim cmdIds = String.Join(",", contIds.ToArray)
        Dim cmdText As String = <a>
              DELETE FROM terloc.terloc.TABLENAME
              WHERE ObjectID IN (IDSTRING)
              </a>.Value.Replace("IDSTRING", cmdIds)

        ' SendOzzy("Terrace " & ErrorHandler.GetCallerMethod, cmdText & "    " & HtmlLineBreak & cmdIds, Nothing) ' ----- DEBUG
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using trans As SqlTransaction = conn.BeginTransaction
            Using cmd As SqlCommand = conn.CreateCommand()
              cmd.Transaction = trans
              Try
                'Should have cascade delete, but do both anyway
                cmd.CommandText = cmdText.Replace("TABLENAME", "Terrace")
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
        callInfo &= String.Format("TerraceHelper DeleteAll error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Delete records matching a terrace date so they can be reloaded from fortran table
    ''' </summary>
    Public Function DeleteByTerraceDate(ByVal projectId As Long, ByVal usrId As Guid _
                    , ByVal terraceDate As DateTime, ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim numRecs As Integer = 0
      Try
        Dim allTerraces As DataTable = GetTerracesTable(projectId, Nothing)
        Dim filterExp As String = "FORTRANDATE = '{0}'"
        Dim terrView As DataView = New DataView(allTerraces, _
                      String.Format(filterExp, terraceDate), "", DataViewRowState.OriginalRows)
        Dim contIds As New List(Of Long)
        Dim contId As Long
        For contIx As Integer = 0 To terrView.Count - 1
          contId = NullSafeLong(terrView(contIx).Row.Item("ObjectID"), -1)
          contIds.Add(contId)
        Next

        'If contIds.Count < 1 Then SendOzzy(ErrorHandler.GetCallerMethod, "no records", Nothing) ' ----- DEBUG
        If contIds.Count < 1 Then Return True

        Dim cmdIds = String.Join(",", contIds.ToArray)
        Dim cmdText As String = <a>
              DELETE FROM terloc.terloc.TABLENAME
              WHERE ObjectID IN (IDSTRING)
              </a>.Value.Replace("IDSTRING", cmdIds)

        'SendOzzy(ErrorHandler.GetCallerMethod, cmdText, Nothing) ' ----- DEBUG
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using trans As SqlTransaction = conn.BeginTransaction
            Using cmd As SqlCommand = conn.CreateCommand()
              cmd.Transaction = trans
              Try
                'Should have cascade delete, but do both anyway
                cmd.CommandText = cmdText.Replace("TABLENAME", "Terrace")
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
        callInfo &= String.Format("TerraceHelper DeleteAll error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#End Region

#Region "Fetch"

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As TerracePackageList
      Dim retVal As New TerracePackageList
      Dim terraces As New List(Of TerracePackage)
      Dim localInfo As String = ""
      Try
        localInfo = ""
        terraces = GetTerracesList(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        retVal.terraces = terraces
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function GetTerracesList(ByVal projectId As Long, ByRef callInfo As String) As List(Of TerracePackage)
      Dim retVal As List(Of TerracePackage) = Nothing
      Dim retTerrace As TerracePackage = Nothing
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetTerracesTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

          localInfo = ""
          features = UpdateNames(features, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw New Exception("TerracesTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retTerrace = ExtractTerraceFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

            If retTerrace IsNot Nothing Then
              If retVal Is Nothing Then retVal = New List(Of TerracePackage)
              retVal.Add(retTerrace)
            End If
          Catch ex As Exception
            Throw New Exception("Terrace (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    Public Function GetTerracesTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>Select  *   
              FROM {0}.Terrace as {1}   
              INNER JOIN {0}.ProjectDatum as {2} ON {2}.ObjectID = {1}.ObjectID 
              WHERE {2}.ProjectID = @projectId </a>.Value, dataSchema, "FT", "PD")

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

    Public Function ExtractTerraceFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As TerracePackage
      Dim retVal As New TerracePackage
      Dim feature As New TerraceFull
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Dim tmpDateTime As DateTime
        Dim geom As GGeom.IGeometry = Nothing
        Try
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .FeatureID = NullSafeInteger(dr.Item("FeatureID"), -1)
            .Shape = NullSafeString(dr.Item("Shape"), "")
            .Type = NullSafeString(dr.Item("Type"), "")
            If DateTime.TryParse(NullSafeString(dr.Item("FortranDate"), ""), tmpDateTime) Then .FortranDate = tmpDateTime
            .ScenarioType = NullSafeInteger(dr.Item("ScenarioType"), -1)
            .CostShareID = NullSafeInteger(dr.Item("CostShareID"), -1) 
            .Ordinal = NullSafeInteger(dr.Item("Ordinal"), -1)
            .Custom = NullSafeBoolean(dr.Item("Custom"))
            localInfo = ""
            geom = ConvertWkbToGeometry(.Shape, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            localInfo = ""
            .Coords = GetCoordsStringFromGeom(geom, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            localInfo = ""
            CalcTerrace(feature, localInfo) 'sets length
            If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          End With
        Catch ex As Exception
          Throw New Exception("TerraceFull (" & callInfo & ")", ex)
        End Try

        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .terraceRecord = feature
          .datumRecord = datum
          .info = ""
        End With

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

#End Region

#Region "Fetch by Type"

    'Public Function FetchByType(ByVal projectId As Long, ByRef callInfo As String) As TerraceTypePackageList
    '  Dim retVal As New TerraceTypePackageList
    '  Dim terraces As New List(Of TerraceTypePackage)
    '  Dim localInfo As String = ""
    '  Try
    '    retVal = GetTerracesByType(projectId, localInfo)
    '    If localInfo.Contains("error") Then callInfo &= localInfo
    '  Catch ex As Exception
    '    callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
    '  End Try
    '  Return retVal
    'End Function

    'Public Function GetTerracesByType(ByVal projectId As Long, ByRef callInfo As String) As TerraceTypePackageList
    '  Dim retVal As New TerraceTypePackageList
    '  Dim retTerraceTypePkg As TerraceTypePackage
    '  Dim retTerraces As List(Of TerracePackage)
    '  Dim retInfo As String = ""
    '  Dim localInfo As String = ""
    '  Try

    '    localInfo = ""
    '    retTerraces = GetTerracesList(projectId, localInfo)
    '    If localInfo.Contains("error") Then callInfo &= localInfo

    '    Dim terraceTypes As IEnumerable(Of String) = retTerraces.[Select](Function(x) x.terraceRecord.Type).Distinct()

    '    For Each terraceType As String In terraceTypes
    '      retTerraceTypePkg = New TerraceTypePackage
    '      retTerraceTypePkg.terraceType = terraceType
    '      retTerraceTypePkg.terraces = retTerraces.Where(Function(x) x.terraceRecord.Type = terraceType).ToList
    '      retVal.terracesByType.Add(retTerraceTypePkg)
    '    Next

    '  Catch ex As Exception
    '    callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.ToString)
    '  End Try
    '  Return retVal
    'End Function

#End Region

#Region "Insert"

    Public Function Insert(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featuredata As String, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Dim feature As New TerraceFull
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of TerraceFull)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (terrace deserialization): {1}", EH.GetCallerMethod(), ex.Message)
          Return Nothing
        End Try

        Try
          localInfo = ""
          CalcTerrace(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (terrace geom calc): {1}", EH.GetCallerMethod(), ex.Message)
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
                                , ByVal feature As TerraceRecord, ByRef callInfo As String) As Long
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
          Dim rowsAffected As Integer = Insert(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          If rowsAffected < 0 Then callInfo &= "  " & datumId & " error: no rows inserted "

          scope.Complete()
        End Using

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return datumId
    End Function

    Private Function Insert(ByVal feature As TerraceRecord, ByRef callInfo As String) As Integer
      Dim retVal As Integer = 0
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[FeatureID] " & _
              ",[Shape] " & _
              ",[Type] " & _
              ",[FortranDate] " & _
              ",[ScenarioType] " & _
              ",[CostShareID] " & _
              ",[Ordinal] " & _
              ",[Custom] "

            Dim insertValues As String = "@ObjectID" & _
              ",@FeatureID " & _
              ",@Shape " & _
              ",@Type " & _
              ",@FortranDate " & _
              ",@ScenarioType " & _
              ",@CostShareID " & _
              ",@Ordinal " & _
              ",@Custom "

            cmdText = "INSERT INTO " & dataSchema & ".Terrace (" & insertFields & ") Values (" & insertValues & ")"

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@FeatureID", SqlDbType.Int).Value = feature.FeatureID
            cmd.Parameters.Add("@Shape", SqlDbType.NVarChar).Value = feature.Shape
            cmd.Parameters.Add("@Type", SqlDbType.NVarChar).Value = feature.Type
            cmd.Parameters.Add("@FortranDate", SqlDbType.DateTime).Value = feature.FortranDate
            cmd.Parameters.Add("@ScenarioType", SqlDbType.Int).Value = feature.ScenarioType
            If feature.ScenarioType < 0 Then cmd.Parameters("@ScenarioType").Value = DBNull.Value
            cmd.Parameters.Add("@CostShareID", SqlDbType.Money).Value = feature.CostShareID
            If feature.CostShareID < 0 Then cmd.Parameters("@CostShareID").Value = DBNull.Value
            cmd.Parameters.Add("@Ordinal", SqlDbType.Int).Value = feature.Ordinal
            If feature.Ordinal < 0 Then cmd.Parameters("@Ordinal").Value = DBNull.Value
            cmd.Parameters.Add("@Custom", SqlDbType.Bit).Value = feature.Custom

            cmd.CommandText = cmdText
            retVal = cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

#End Region

    ''' <summary>
    ''' Gets shape and metrics for a feature containing coordinates in Lat/Lng
    ''' </summary> 
    Public Sub CalcTerrace(ByRef feature As TerraceFull, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        If String.IsNullOrWhiteSpace(feature.Coords) Then Return

        Dim coords As String = feature.Coords
        coords = HttpContext.Current.Server.UrlDecode(coords)

        localInfo = ""
        Dim geom As GGeom.IGeometry = CreateLineStringFromCoordString(coords, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim line As GGeom.ILineString = TryCast(geom, GGeom.ILineString)
        localInfo = ""
        Dim lineLen = GetLengthFromLatLngLinestring(line, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        If line IsNot Nothing Then
          feature.Length = lineLen
          localInfo = ""
          feature.Shape = ConvertGeometryToWkb(line, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        End If

      Catch ex As Exception
        callInfo &= ErrorHandler.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Public Sub Duplicate(ByVal origProjectId As Long, ByVal newProjectId As Long, Optional ByRef callInfo As String = "")
      Dim localInfo As String = ""
      Try
        Dim feat As TerraceRecord
        Dim pkg As TerracePackage
        Dim featureList As TerracePackageList

        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        featureList = Fetch(origProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim origFeatId As Long
        Dim newFeatId As Long
        For Each pkg In featureList.terraces
          feat = pkg.terraceRecord
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