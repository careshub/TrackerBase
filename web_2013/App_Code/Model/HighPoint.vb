Option Explicit On
Option Strict On

Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports System.Web.Script.Serialization
Imports System.Transactions
Imports EH = ErrorHandler
Imports CommonFunctions

Namespace TerLoc.Model

  <Serializable()> _
  Public Class HighPointPackage
    Public highPointRecord As HighPoint
    Public datumRecord As ProjectDatum
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  <Serializable()> _
  Public Class HighPointPackageList
    Public highPoints As New List(Of HighPointPackage)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class HighPoint
    Private Const NULL_VALUE As Integer = -1

    Public ObjectID As Long = NULL_VALUE
    Public Elevation As Double = NULL_VALUE
    Public Latitude As Double = NULL_VALUE
    Public Longitude As Double = NULL_VALUE
    Public Shape As String = ""
  End Class

  Public Class HighPointHelper
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private dataSchema As String = CommonVariables.ProjectProductionSchema
    Private serializer As New JavaScriptSerializer

    Public Function Deserialize(ByVal featureData As String, ByVal callInfo As String) As HighPoint
      Dim feat As HighPoint
      Try
        feat = CType(serializer.Deserialize(featureData, GetType(HighPoint)), HighPoint)
      Catch ex As Exception
        callInfo &= String.Format("HighPointHelper Convert error: {0}", ex.ToString)
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
                    DELETE FROM {0}.HighPoint WHERE ObjectID = @objId
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
        callInfo &= String.Format("HighPointHelper Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Edit"

    Public Sub Update(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featureId As String, ByVal featureData As String, ByRef callInfo As String)
      Dim feature As New HighPoint
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim isOkToUpdate = True
      Try
        Try
          feature = CType(serializer.Deserialize(featureData, GetType(HighPoint)), HighPoint)
          feature.ObjectID = CInt(featureId)
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (feature deserialization): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

        localInfo = ""
        EditHighPoint(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Public Sub EditHighPoint(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal feature As HighPoint, ByRef callInfo As String)
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
        UpdateHighPointToDatabase(feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'scope.Complete()
        'End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Private Sub UpdateHighPointToDatabase(ByVal feature As HighPoint, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()
            Dim flds As String() = New String() {"[Latitude] ", "[Longitude] "}
            Dim vals As String() = New String() {"@Latitude ", "@Longitude "}
            Dim sql As New StringBuilder("UPDATE ")

            Try
              sql.Append("" & dataSchema & ".HighPoint ")
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
            cmd.Parameters.Add("@Latitude", SqlDbType.Real).Value = feature.Latitude
            cmd.Parameters.Add("@Longitude", SqlDbType.Real).Value = feature.Longitude

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

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As HighPointPackageList
      Dim retVal As New HighPointPackageList
      Dim localInfo As String = ""
      Try
        retVal = GetHighPoints(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function GetHighPoints(ByVal projectId As Long, ByRef callInfo As String) As HighPointPackageList
      Dim retVal As New HighPointPackageList
      Dim retHighPoints As List(Of HighPointPackage)
      Dim retInfo As String = ""
      Dim localInfo As String = ""
      Try

        localInfo = ""
        retHighPoints = GetHighPointsList(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        retVal.highPoints = retHighPoints
        retVal.info = retInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function GetHighPointsList(ByVal projectId As Long, ByRef callInfo As String) As List(Of HighPointPackage)
      Dim retVal As New List(Of HighPointPackage)
      Dim retHighPoint As HighPointPackage = Nothing
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetHighPointsTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

          localInfo = ""
          features = UpdateNames(features, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw New Exception("HighPointsTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retHighPoint = ExtractHighPointFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

            If retHighPoint IsNot Nothing Then
              retVal.Add(retHighPoint)
            End If
          Catch ex As Exception
            Throw New Exception("HighPoint (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    Public Function GetHighPointsTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>Select  *   
          FROM {0}.HighPoint as {1}   
          INNER JOIN {0}.ProjectDatum as {2} ON {2}.ObjectID = {1}.ObjectID 
          WHERE {2}.ProjectID = @projectId </a>.Value, dataSchema, "FT", "PD")

        Dim parms As New List(Of SqlParameter)
        Dim parm As New SqlParameter("@projectId", SqlDbType.BigInt)
        parm.Value = projectId
        parms.Add(parm)

        localInfo = ""
        retVal = GetDataTable(CommonFunctions.GetBaseDatabaseConnString, cmdText, parms.ToArray, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function ExtractHighPointFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As HighPointPackage
      Dim retVal As New HighPointPackage
      Dim feature As New HighPoint
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Try
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .Latitude = NullSafeDouble(dr.Item("Latitude"), -1)
            .Longitude = NullSafeDouble(dr.Item("Longitude"), -1)
            .Elevation = NullSafeDouble(dr.Item("Elevation"), -1)
          End With
        Catch ex As Exception
          Throw New Exception("HighPointStructure (" & callInfo & ")", ex)
        End Try

        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .highPointRecord = feature
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
                                , ByVal featureData As String, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Dim feature As New HighPoint
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = CType(serializer.Deserialize(featureData, GetType(HighPoint)), HighPoint)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (high point deserialization): {1}", EH.GetCallerMethod(), ex.Message)
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
                                , ByVal feature As HighPoint, ByRef callInfo As String) As Long
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

    Private Sub Insert(ByVal feature As HighPoint, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[Latitude] " & _
              ",[Longitude] "

            Dim insertValues As String = "@ObjectID" & _
              ",@Latitude " & _
              ",@Longitude "

            cmdText = "INSERT INTO " & dataSchema & ".HighPoint (" & insertFields & ") Values (" & insertValues & ")"

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@Latitude", SqlDbType.Real).Value = feature.Latitude
            cmd.Parameters.Add("@Longitude", SqlDbType.Real).Value = feature.Longitude

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

    Public Sub Duplicate(ByVal origProjectId As Long, ByVal newProjectId As Long, Optional ByRef callInfo As String = "")
      Dim localInfo As String = ""
      Try
        Dim feat As HighPoint
        Dim pkg As HighPointPackage
        Dim featureList As HighPointPackageList

        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        featureList = Fetch(origProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim origFeatId As Long
        Dim newFeatId As Long
        For Each pkg In featureList.highPoints
          feat = pkg.highPointRecord
          origFeatId = feat.ObjectID

          localInfo = ""
          newFeatId = Insert(newProjectId, usrId, feat, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & "orig id " & origFeatId & ", new id " & newFeatId & ": " & localInfo
        Next
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    ''' <summary>
    ''' Use until Shape field is working part of db. Makes a wkb out of x and y
    ''' </summary>
    Public Function MakeWkb(ByVal feat As DataRow, ByRef callInfo As String) As String
      Dim retVal As String = ""
      Dim localInfo As String = ""
      Try
        Dim x As Double = NullSafeDouble(feat.Item("Longitude"), Double.MinValue)
        If x = Double.MinValue Then Return ""
        Dim y As Double = NullSafeDouble(feat.Item("Latitude"), Double.MinValue)
        If y = Double.MinValue Then Return ""

        Dim coords As String = x & CommonVariables.CoordinateSplitter & y

        localInfo = ""
        Dim point As GeoAPI.Geometries.IPoint = GIS.GISToolsAddl.CreatePointFromCoordString(coords, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        retVal = GIS.GISToolsAddl.ConvertGeometryToWkb(point, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

  End Class

End Namespace