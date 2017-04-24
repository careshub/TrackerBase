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

  Public Class Ridgeline
    Private Const NULL_VALUE As Integer = -1

    Public ObjectID As Long = NULL_VALUE
    Public Length As Double = NULL_VALUE
    Public Shape As String = ""
    Public Coords As String = "" 'not in db
  End Class

  <Serializable()> _
  Public Class RidgelinePackage
    Public ridgelineRecord As Ridgeline
    Public datumRecord As ProjectDatum
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  <Serializable()> _
  Public Class RidgelinePackageList
    Public ridgelines As New List(Of RidgelinePackage)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class RidgelineHelper 
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private dataSchema As String = CommonVariables.ProjectProductionSchema

    Public Function Deserialize(ByVal featureData As String, ByVal callInfo As String) As Ridgeline
      Dim feat As Ridgeline
      Try
        feat = DeserializeJson(Of Ridgeline)(featureData)
      Catch ex As Exception
        callInfo &= String.Format("RidgelineHelper Convert error: {0}", ex.ToString)
        feat = Nothing
      End Try
      Return feat
    End Function

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
                  cmd.Parameters.Add("@objId", SqlDbType.BigInt).Value = featureId

                  cmd.CommandText = cmdText
                  cmd.ExecuteNonQuery()

                  'Cascade delete is in place, but just in case.
                  cmdText = String.Format(<a>
                    DELETE FROM {0}.Ridgeline WHERE ObjectID = @objId
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
        callInfo &= String.Format("RidgelineHelper Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Edit"

    Public Sub Update(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featureId As String, ByVal featureData As String, ByRef callInfo As String)
      Dim feature As New Ridgeline
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Try
        Try
          feature = DeserializeJson(Of Ridgeline)(featureData)
          feature.ObjectID = CInt(featureId)
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (feature deserialization): {1}  ", ErrorHandler.GetCallerMethod(), ex.Message)
          Return
        End Try

        localInfo = ""
        EditRidgeline(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", ErrorHandler.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Public Sub EditRidgeline(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal feature As Ridgeline, ByRef callInfo As String)
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
        UpdateRidgelineToDatabase(feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'scope.Complete()
        'End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Public Sub UpdateRidgelineToDatabase(ByVal feature As Ridgeline, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()
            Dim flds As String() = New String() {"[Shape] "}
            Dim vals As String() = New String() {"@Shape "}
            Dim sql As New StringBuilder("UPDATE ")

            CalcRidgeline(feature, localInfo)
            Try
              sql.Append("" & dataSchema & ".Ridgeline ")
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
            cmd.Parameters.Add("@Shape", SqlDbType.NVarChar).Value = feature.Shape

            cmd.CommandText = sql.ToString
            cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

#End Region

#Region "Fetch"

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As RidgelinePackageList
      Dim retVal As New RidgelinePackageList
      Dim ridgelines As New List(Of RidgelinePackage)
      Dim localInfo As String = ""
      Try
        retVal = GetRidgelines(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function GetRidgelines(ByVal projectId As Long, ByRef callInfo As String) As RidgelinePackageList
      Dim retVal As New RidgelinePackageList
      Dim retRidgelines As List(Of RidgelinePackage)
      Dim retInfo As String = ""
      Dim localInfo As String = ""
      Try

        localInfo = ""
        retRidgelines = GetRidgelinesList(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        retVal.ridgelines = retRidgelines
        retVal.info = retInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function GetRidgelinesList(ByVal projectId As Long, ByRef callInfo As String) As List(Of RidgelinePackage)
      Dim retVal As New List(Of RidgelinePackage)
      Dim retRidgeline As RidgelinePackage = Nothing
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetRidgelinesTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

          localInfo = ""
          features = UpdateNames(features, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw New Exception("RidgelinesTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retRidgeline = ExtractRidgelineFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

            If retRidgeline IsNot Nothing Then
              If retVal Is Nothing Then retVal = New List(Of RidgelinePackage)
              retVal.Add(retRidgeline)
            End If
          Catch ex As Exception
            Throw New Exception("Ridgeline (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    Public Function GetRidgelinesTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>Select  *   
          FROM {0}.Ridgeline as {1}   
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

    Public Function ExtractRidgelineFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As RidgelinePackage
      Dim retVal As New RidgelinePackage
      Dim feature As New Ridgeline
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Dim geom As GGeom.IGeometry = Nothing
        Try
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .Shape = NullSafeString(dr.Item("Shape"), "")
            localInfo = ""
            geom = ConvertWkbToGeometry(.Shape, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            localInfo = ""
            .Coords = GetCoordsStringFromGeom(geom, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            localInfo = ""
            CalcRidgeline(feature, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
          End With
        Catch ex As Exception
          Throw New Exception("RidgelineStructure (" & callInfo & ")", ex)
        End Try

        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .ridgelineRecord = feature
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
      Dim feature As New Ridgeline
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of Ridgeline)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (ridgeline deserialization): {1}", EH.GetCallerMethod(), ex.Message)
          Return Nothing
        End Try

        Try
          localInfo = ""
          CalcRidgeline(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (ridgeline geom calc): {1}", EH.GetCallerMethod(), ex.Message)
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
                                , ByVal feature As Ridgeline, ByRef callInfo As String) As Long
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

    Private Sub Insert(ByVal feature As Ridgeline, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        callInfo &= feature.Coords & Environment.NewLine
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[Shape] "

            Dim insertValues As String = "@ObjectID" & _
              ",@Shape "

            cmdText = "INSERT INTO " & dataSchema & ".Ridgeline (" & insertFields & ") Values (" & insertValues & ")"

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@Shape", SqlDbType.NVarChar).Value = feature.Shape

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

    Public Sub CalcRidgeline(ByRef feature As Ridgeline, ByRef callInfo As String)
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
        Dim feat As Ridgeline
        Dim pkg As RidgelinePackage
        Dim featureList As RidgelinePackageList

        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        featureList = Fetch(origProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim origFeatId As Long
        Dim newFeatId As Long
        For Each pkg In featureList.ridgelines
          feat = pkg.ridgelineRecord
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