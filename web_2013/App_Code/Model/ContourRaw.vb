Option Explicit On
Option Strict On

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

  Public Class ContourRawRecord
    Private Const NULL_VALUE As Integer = -1

    Public ObjectID As Long = NULL_VALUE
    Public Contour As Integer = 1
    Public Shape As String = ""
    Public Type As String = "" 'SMO for smooth
  End Class

  Public Class ContourRawFull
    Inherits ContourRawRecord
    Private Const NULL_VALUE As Integer = -1

    Public Length As Double = NULL_VALUE
    Public Coords As String = ""
  End Class

  <Serializable()> _
  Public Class ContourRawPackage
    Public contourRecord As ContourRawFull
    Public datumRecord As ProjectDatum
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  <Serializable()> _
  Public Class ContourRawPackageList
    Public contours As New List(Of ContourRawPackage)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class ContourRawHelper
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString 
    Private dataSchema As String = CommonVariables.ProjectProductionSchema

    Public Function DeleteAll(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim numRecs As Integer = 0
      Try
        Dim allContours As DataTable = GetContoursTable(projectId, Nothing)
        Dim contIds As New List(Of Long)
        Dim contId As Long
        For contIx As Integer = 0 To allContours.Rows.Count - 1
          contId = NullSafeLong(allContours.Rows(contIx).Item("ObjectID"), -1)
          contIds.Add(contId)
        Next

        'If contIds.Count < 1 Then SendOzzy(EH.GetCallerMethod(), "no records", Nothing) ' ----- DEBUG
        If contIds.Count < 1 Then Return True

        Dim cmdIds = String.Join(",", contIds.ToArray)
        Dim cmdText As String = <a>
          DELETE FROM terloc.terloc.TABLENAME
          WHERE ObjectID IN (IDSTRING)
          </a>.Value.Replace("IDSTRING", cmdIds)

        'SendOzzy(EH.GetCallerMethod(), cmdText, Nothing) ' ----- DEBUG
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using trans As SqlTransaction = conn.BeginTransaction
            Using cmd As SqlCommand = conn.CreateCommand()
              cmd.Transaction = trans
              Try
                'Should have cascade delete, but do both anyway
                cmd.CommandText = cmdText.Replace("TABLENAME", "ContourRaw")
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
        callInfo &= String.Format("ContourRawHelper DeleteAll error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function DeleteAllByType(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByVal orgOrSmo As String, ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim numRecs As Integer = 0
      Try
        Dim allContours As DataTable = GetContoursTable(projectId, Nothing)
        Dim contIds As New List(Of Long)
        Dim contId As Long
        Dim contRw As DataRow
        For contIx As Integer = 0 To allContours.Rows.Count - 1
          contRw = allContours.Rows(contIx)
          If orgOrSmo.ToUpper = "SMO" AndAlso NullSafeString(contRw.Item("Type"), "").ToUpper = "SMO" Then
            contId = NullSafeLong(contRw.Item("ObjectID"), -1)
            contIds.Add(contId)
          ElseIf orgOrSmo.ToUpper = "ORG" AndAlso NullSafeString(contRw.Item("Type"), "").ToUpper <> "SMO" Then
            'Include NULL values in the ORG category
            contId = NullSafeLong(contRw.Item("ObjectID"), -1)
            contIds.Add(contId)
          End If
        Next

        If contIds.Count < 1 Then Return True

        Dim cmdIds = String.Join(",", contIds.ToArray)
        Dim cmdText As String = <a>
          DELETE FROM terloc.terloc.TABLENAME
          WHERE ObjectID IN (IDSTRING)
          </a>.Value.Replace("IDSTRING", cmdIds)

        'SendOzzy(EH.GetCallerMethod(), cmdText, Nothing) ' ----- DEBUG
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using trans As SqlTransaction = conn.BeginTransaction
            Using cmd As SqlCommand = conn.CreateCommand()
              cmd.Transaction = trans
              Try
                'Should have cascade delete, but do both anyway
                cmd.CommandText = cmdText.Replace("TABLENAME", "ContourRaw")
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
        callInfo &= String.Format("ContourRawHelper DeleteAll error: {0}", ex.ToString)
      End Try
      Return retVal
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
                  cmd.Parameters.Add("@objId", SqlDbType.BigInt).Value = CLng(featureId)

                  cmd.CommandText = cmdText
                  cmd.ExecuteNonQuery()

                  'Cascade delete is in place, but just in case.
                  cmdText = String.Format(<a>
                    DELETE FROM {0}.ContourRaw WHERE ObjectID = @objId
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
        callInfo &= String.Format("ContourRawHelper Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Edit"

    Public Sub Update(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featureId As String, ByVal featureData As String, ByRef callInfo As String)
      Dim feature As New ContourRawFull
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Try
        Try
          feature = DeserializeJson(Of ContourRawFull)(featureData)
          feature.ObjectID = CInt(featureId)
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (feature deserialization): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

        localInfo = ""
        EditContour(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Public Sub EditContour(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal feature As ContourRawFull, ByRef callInfo As String)
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
        UpdateContourToDatabase(feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'scope.Complete()
        'End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Private Sub UpdateContourToDatabase(ByVal feature As ContourRawFull, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()
            Dim flds As String() = New String() {"[Contour] ", "[Shape] "}
            Dim vals As String() = New String() {"@Contour ", "@Shape "}
            Dim sql As New StringBuilder("UPDATE ")

            CalcContour(feature, localInfo)
            Try
              sql.Append("" & dataSchema & ".ContourRaw ")
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
            cmd.Parameters.Add("@Contour", SqlDbType.Int).Value = feature.Contour
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

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As ContourRawPackageList
      Dim retVal As New ContourRawPackageList
      Dim contours As New List(Of ContourRawPackage)
      Dim localInfo As String = ""
      Try
        retVal = GetContours(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function GetContours(ByVal projectId As Long, ByRef callInfo As String) As ContourRawPackageList
      Dim retVal As New ContourRawPackageList
      Dim retContours As List(Of ContourRawPackage)
      Dim retInfo As String = ""
      Dim localInfo As String = ""
      Try

        localInfo = ""
        retContours = GetContoursList(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        retVal.contours = retContours
        retVal.info = retInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function GetContoursList(ByVal projectId As Long, ByRef callInfo As String) As List(Of ContourRawPackage)
      Dim retVal As List(Of ContourRawPackage) = Nothing
      Dim retContour As ContourRawPackage = Nothing
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetContoursTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

          localInfo = ""
          features = UpdateNames(features, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw New Exception("ContoursTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retContour = ExtractContourFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

            If retContour IsNot Nothing Then
              If retVal Is Nothing Then retVal = New List(Of ContourRawPackage)
              retVal.Add(retContour)
            End If
          Catch ex As Exception
            Throw New Exception("ContourRaw (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    Public Function GetContoursTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>Select  *   
          FROM {0}.ContourRaw as {1}   
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

    Public Function ExtractContourFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As ContourRawPackage
      Dim retVal As New ContourRawPackage
      Dim feature As New ContourRawFull
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Dim geom As GGeom.IGeometry = Nothing
        Try
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .Contour = NullSafeInteger(dr.Item("Contour"), -1)
            .Shape = NullSafeString(dr.Item("Shape"), "")
            localInfo = ""
            geom = ConvertWkbToGeometry(.Shape, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            localInfo = ""
            .Coords = GetCoordsStringFromGeom(geom, localInfo)
            If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
            localInfo = ""
            CalcContour(feature, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          End With
        Catch ex As Exception
          Throw New Exception("ContourRawFull (" & callInfo & ")", ex)
        End Try

        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .contourRecord = feature
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
      Dim feature As New ContourRawFull
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of ContourRawFull)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (contour raw deserialization): {1}", EH.GetCallerMethod(), ex.Message)
          Return Nothing
        End Try

        Try
          localInfo = ""
          CalcContour(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (contour raw geom calc): {1}", EH.GetCallerMethod(), ex.Message)
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
                                , ByVal feature As ContourRawRecord, ByRef callInfo As String) As Long
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
          InsertContourToDatabase(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

          scope.Complete()
        End Using

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return datumId
    End Function

    Private Sub InsertContourToDatabase(ByVal feature As ContourRawRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[Contour] " & _
              ",[Shape] "

            Dim insertValues As String = "@ObjectID" & _
              ",@Contour " & _
              ",@Shape "

            cmdText = "INSERT INTO " & dataSchema & ".ContourRaw (" & insertFields & ") Values (" & insertValues & ")"

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@Contour", SqlDbType.Int).Value = feature.Contour
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

    ''' <summary>
    ''' Remove existing contours and import new raw contours.
    ''' </summary> 
    Public Sub ImportRawContours(ByVal projectId As Long, ByVal fileName As String, _
                               ByVal contourCol As String, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        'Remove old stuff
        Try
          Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId

          localInfo = ""
          Me.DeleteAll(projectId, usrId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

          Dim myContourHelper As New ContourHelper
          localInfo = ""
          myContourHelper.DeleteAll(projectId, usrId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

          Dim myContourXmlHelper As New ContourXmlHelper
          localInfo = ""
          myContourXmlHelper.DeleteAll(projectId, usrId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

          Dim myLegacyContourXmlHelper As New Legacy.XmlContourHelper
          localInfo = ""
          myLegacyContourXmlHelper.DeleteAll(projectId, usrId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= EH.GetCallerMethod() & " delete error: " & ex.ToString
        End Try

        'Bring in new stuff (raw)
        Try
          localInfo = ""
          UploadTools.ImportGisContours(projectId, fileName, contourCol, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= EH.GetCallerMethod() & " import error: " & ex.ToString
        End Try

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    ''' <summary>
    ''' Gets shape and metrics for a feature containing coordinates in Lat/Lng.
    ''' </summary> 
    Public Sub CalcContour(ByRef feature As ContourRawFull, ByRef callInfo As String)
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
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Public Sub Duplicate(ByVal origProjectId As Long, ByVal newProjectId As Long, Optional ByRef callInfo As String = "")
      Dim localInfo As String = ""
      Try
        Dim feat As ContourRawRecord
        Dim pkg As ContourRawPackage
        Dim featureList As ContourRawPackageList

        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        featureList = Fetch(origProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim origFeatId As Long
        Dim newFeatId As Long
        For Each pkg In featureList.contours
          feat = pkg.contourRecord
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