Option Explicit On
Option Strict On

Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports System.Web.Script.Serialization
Imports System.Transactions
Imports EH = ErrorHandler
Imports CommonFunctions
Imports GIS.GISToolsAddl
Imports GGeom = GeoAPI.Geometries

Namespace TerLoc.Model

  <Serializable()> _
  Public Class WaterwayPackage
    Implements IComparable(Of WaterwayPackage)

    Public waterwayRecord As Waterway
    Public datumRecord As ProjectDatum
    Public info As String 'Use for error messages, stack traces, etc.

    Public Function CompareTo(other As WaterwayPackage) As Integer Implements System.IComparable(Of WaterwayPackage).CompareTo 'BASE comparison
      Dim retVal As Integer = 0
      retVal = Me.waterwayRecord.Ordinal.CompareTo(other.waterwayRecord.Ordinal) * 1 '1=asc,-1=desc
      'If retVal <> 0 Then Return retVal
      'Return Me.category.CompareTo(other.category) * 1 'sort asc on category if percent is same
      Return retVal
    End Function

  End Class

  <Serializable()> _
  Public Class WaterwayPackageList
    Public waterways As New List(Of WaterwayPackage)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class Waterway
    Private Const NULL_VALUE As Integer = -1

    Public ObjectID As Long = NULL_VALUE
    Public Ordinal As Integer = 1
    Public Shape As String = ""
    Public Coords As String = "" 'not in db
    Public Length As Double = NULL_VALUE 'not in db
  End Class

  Public Class WaterwayOrder
    Public ObjectID As Long = -1
    Public Ordinal As Integer = 0
  End Class

  Public Class WaterwayHelper
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private dataSchema As String = CommonVariables.ProjectProductionSchema

    Public Function Delete(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByVal featureId As String, ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Try
        Dim cmdText As String = ""
        If featureId <> "" And IsNumeric(featureId) Then

          Using conn As New SqlConnection(dataConn)
            If conn.State = ConnectionState.Closed Then conn.Open()
            Using trans As SqlTransaction = conn.BeginTransaction
              Using cmd As SqlCommand = conn.CreateCommand()
                cmd.Transaction = trans
                Try
                  cmdText = String.Format(<a>
                    DELETE FROM {0}.ProjectDatum WHERE ObjectID = @objId
                    </a>.Value, dataSchema)
                  cmd.Parameters.Add("@objId", SqlDbType.BigInt).Value = CLng(featureId)

                  cmd.CommandText = cmdText
                  cmd.ExecuteNonQuery()

                  'Cascade delete is in place, but just in case.
                  cmdText = String.Format(<a>
                    DELETE FROM {0}.Waterway WHERE ObjectID = @objId
                    </a>.Value, dataSchema)

                  cmd.CommandText = cmdText
                  cmd.ExecuteNonQuery()

                  trans.Commit()
                  retVal = True
                Catch ex As Exception
                  trans.Rollback()
                  Throw
                End Try
              End Using
            End Using
          End Using
        End If
      Catch ex As Exception
        callInfo &= String.Format("WaterwayHelper Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Edit"

    Public Sub UpdateWaterwayAlignment(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featureData As String, ByRef callInfo As String)
      Dim feature As List(Of Waterway)
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Try
        Try
          feature = DeserializeJson(Of List(Of Waterway))(featureData)
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (feature deserialization): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

        Dim sql As String = " UPDATE " & dataSchema & ".Waterway SET Shape = CASE ObjectId "
        Dim ids As New List(Of Long)
        Dim parms As New List(Of SqlParameter)
        Dim parm As SqlParameter
        Dim parmName As String = ""
        For Each feat As Waterway In feature
          CalcWaterway(feat, Nothing)
          parmName = "@shape" & feat.ObjectID
          sql &= " WHEN " & feat.ObjectID & " THEN " & parmName & " "
          parm = New SqlParameter(parmName, SqlDbType.NVarChar)
          parm.Value = feat.Shape
          parms.Add(parm)
          ids.Add(feat.ObjectID)
        Next
        sql &= " END "
        sql &= " WHERE ObjectId IN (" & String.Join(",", ids.ToArray) & ") "

        Dim rowsAffected As Integer = ExecuteSqlNonQuery(New SqlConnection(dataConn), sql, parms.ToArray, localInfo)
        If rowsAffected <> feature.Count Or Not String.IsNullOrWhiteSpace(localInfo) Then
          Dim msg As String = "Rows affected: " & rowsAffected & "   <br /><br /> " & sql
          SendOzzy("UpdateWaterwayAlignment " & feature.Count, sql, Nothing)
        End If

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
    End Sub

    Public Sub UpdateWaterwayOrdinals(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featureData As String, ByRef callInfo As String)
      Dim feature As List(Of WaterwayOrder)
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Try
        Try
          feature = DeserializeJson(Of List(Of WaterwayOrder))(featureData)
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (feature deserialization): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

        Dim sql As String = " UPDATE " & dataSchema & ".Waterway SET Ordinal = CASE ObjectId "
        Dim ids As New List(Of Long)
        For Each feat As WaterwayOrder In feature
          sql &= " WHEN " & feat.ObjectID & " THEN " & feat.Ordinal & " "
          ids.Add(feat.ObjectID)
        Next
        sql &= " END "
        sql &= " WHERE ObjectId IN (" & String.Join(",", ids.ToArray) & ") "

        Dim rowsAffected As Integer = ExecuteSqlNonQuery(New SqlConnection(dataConn), sql, localInfo)
        If rowsAffected <> feature.Count Or Not String.IsNullOrWhiteSpace(localInfo) Then
          Dim msg As String = "Rows affected: " & rowsAffected & "   <br /><br /> " & sql
          SendOzzy("UpdateWaterwayOrdering " & feature.Count, sql, Nothing)
        End If

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
    End Sub

    Public Sub Update(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featureId As String, ByVal featureData As String, ByRef callInfo As String)
      Dim feature As New Waterway
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Try
        Try
          feature = DeserializeJson(Of Waterway)(featureData)
          feature.ObjectID = CInt(featureId)
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (feature deserialization): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

        localInfo = ""
        EditWaterway(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Public Sub EditWaterway(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal feature As Waterway, ByRef callInfo As String)
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim isOkToUpdate = True
      Try
        'Skip scope here. Feature/datum not dependent here.
        'Using scope As New TransactionScope

        localInfo = ""
        Dim pdUpdated As Integer = UpdateProjectDatumByDatumId(feature.ObjectID, usrId, Nothing, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        UpdateWaterwayToDatabase(feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'scope.Complete()
        'End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Public Sub UpdateWaterwayToDatabase(ByVal feature As Waterway, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            Dim flds As String() = New String() {"[Ordinal] ", "[Shape] "}
            Dim vals As String() = New String() {"@Ordinal ", "@Shape "}
            Dim sql As New StringBuilder("UPDATE ")

            CalcWaterway(feature, localInfo)
            Try
              sql.Append("" & dataSchema & ".Waterway ")
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
            cmd.Parameters.Add("@Ordinal", SqlDbType.Int).Value = feature.Ordinal
            cmd.Parameters.Add("@Shape", SqlDbType.NVarChar).Value = feature.Shape

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

#Region "Fetch"

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As WaterwayPackageList
      Dim retVal As New WaterwayPackageList
      Dim waterways As New List(Of WaterwayPackage)
      Dim localInfo As String = ""
      Try
        retVal = GetWaterways(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function GetWaterways(ByVal projectId As Long, ByRef callInfo As String) As WaterwayPackageList
      Dim retVal As New WaterwayPackageList
      Dim retWaterways As List(Of WaterwayPackage)
      Dim retInfo As String = ""
      Dim localInfo As String = ""
      Try

        localInfo = ""
        retWaterways = GetWaterwaysList(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        retVal.waterways = retWaterways
        retVal.info = retInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function GetWaterwaysList(ByVal projectId As Long, ByRef callInfo As String) As List(Of WaterwayPackage)
      Dim retVal As New List(Of WaterwayPackage)
      Dim retWaterway As WaterwayPackage = Nothing
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetWaterwaysTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

          localInfo = ""
          features = UpdateNames(features, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw New Exception("WaterwaysTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retWaterway = ExtractWaterwayFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

            If retWaterway IsNot Nothing Then
              retVal.Add(retWaterway)
            End If
          Catch ex As Exception
            Throw New Exception("Waterway (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    Public Function GetWaterwaysTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>Select {1}.ObjectID, {1}.[Ordinal], {1}.Shape, {2}.*
          FROM {0}.Waterway as {1}
          INNER JOIN {0}.ProjectDatum as {2} ON {2}.ObjectID = {1}.ObjectID 
          WHERE {2}.ProjectID = @projectId
          ORDER BY {1}.Ordinal </a>.Value, dataSchema, "FT", "PD")

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

    Public Function ExtractWaterwayFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As WaterwayPackage
      Dim retVal As New WaterwayPackage
      Dim feature As New Waterway
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Dim geom As GGeom.IGeometry = Nothing
        Try
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .Ordinal = NullSafeInteger(dr.Item("Ordinal"), -1)
            .Shape = NullSafeString(dr.Item("Shape"), "")
            localInfo = ""
            geom = ConvertWkbToGeometry(.Shape, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            localInfo = ""
            .Coords = GetCoordsStringFromGeom(geom, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            localInfo = ""
            CalcWaterway(feature, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          End With
        Catch ex As Exception
          Throw New Exception("WaterwayStructure (" & callInfo & ")", ex)
        End Try

        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .waterwayRecord = feature
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
      Dim feature As New Waterway
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of Waterway)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (waterway deserialization): {1}", EH.GetCallerMethod(), ex.Message)
          Return Nothing
        End Try

        Try
          localInfo = ""
          CalcWaterway(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (waterway geom calc): {1}", EH.GetCallerMethod(), ex.Message)
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
                                , ByVal feature As Waterway, ByRef callInfo As String) As Long
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

    Private Sub Insert(ByVal feature As Waterway, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        callInfo &= feature.Coords & Environment.NewLine
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[Ordinal] " & _
              ",[Shape] "

            Dim insertValues As String = "@ObjectID" & _
              ",@Ordinal " & _
              ",@Shape "

            cmdText = "INSERT INTO " & dataSchema & ".Waterway (" & insertFields & ") Values (" & insertValues & ")"

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@Ordinal", SqlDbType.Int).Value = feature.Ordinal
            cmd.Parameters.Add("@Shape", SqlDbType.NVarChar).Value = feature.Shape

            cmd.CommandText = cmdText
            If cmd.Connection.State <> ConnectionState.Open Then conn.Open()
            cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
        Throw
      End Try
    End Sub

    Public Sub IncrementOtherWaterways(ByVal projectId As Long, ByVal alteredOid As Long, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        localInfo = ""
        Dim allFeats As WaterwayPackageList = Me.Fetch(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim feat As Waterway
        Dim alteredFeature As Waterway = Nothing
        For Each featPkg As WaterwayPackage In allFeats.waterways
          feat = featPkg.waterwayRecord
          If feat.ObjectID = alteredOid Then
            alteredFeature = feat
            Exit For
          End If
        Next

        If alteredFeature Is Nothing Then Exit Try 'abort, not found

        For Each featPkg As WaterwayPackage In allFeats.waterways
          feat = featPkg.waterwayRecord
          If feat.ObjectID <> alteredFeature.ObjectID AndAlso feat.Ordinal >= alteredFeature.Ordinal Then
            feat.Ordinal += 1
            localInfo = ""
            Me.UpdateWaterwayToDatabase(feat, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          End If
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
        Throw
      End Try
    End Sub

#End Region

    Public Sub CalcWaterway(ByRef feature As Waterway, ByRef callInfo As String)
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
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Public Sub Duplicate(ByVal origProjectId As Long, ByVal newProjectId As Long, Optional ByRef callInfo As String = "")
      Dim localInfo As String = ""
      Try
        Dim feat As Waterway
        Dim pkg As WaterwayPackage
        Dim featureList As WaterwayPackageList

        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        featureList = Fetch(origProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim origFeatId As Long
        Dim newFeatId As Long
        For Each pkg In featureList.waterways
          feat = pkg.waterwayRecord
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