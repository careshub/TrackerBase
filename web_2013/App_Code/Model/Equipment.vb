Option Explicit On
Option Strict On

Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports System.Transactions
Imports System.Web.Script.Serialization
Imports EH = ErrorHandler
Imports CommonFunctions
Imports CommonVariables

Namespace TerLoc.Model

  Public Class EquipmentRecord
    Private NULL_VALUE As Short = -1
    Public ObjectID As Long = NULL_VALUE
    Public NumberOfMachines As Integer = NULL_VALUE
    Public MachineRowWidth As Integer = NULL_VALUE
    Public NumberOfRows As Integer = NULL_VALUE
  End Class

  <Serializable()> _
  Public Class EquipmentPackage
    Public EquipmentRecord As EquipmentRecord
    Public DatumRecord As ProjectDatum
    Public Info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class EquipmentHelper

    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private dataSchema As String = CommonVariables.ProjectProductionSchema

    Public Function Delete(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByVal featureId As String, ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Try
        Dim cmdText As String = ""
        If Not (featureId <> "" And IsNumeric(featureId)) Then Return retVal

        Using scope As New TransactionScope
          Using conn As New SqlConnection(dataConn)
            If conn.State = ConnectionState.Closed Then conn.Open()
            Using cmd As SqlCommand = conn.CreateCommand()
              Try
                cmdText = String.Format(<a>
                    DELETE FROM {0}.ProjectDatum WHERE ObjectID = @objId
                    </a>.Value, dataSchema)
                cmd.Parameters.Add("@objId", SqlDbType.BigInt).Value = CLng(featureId)

                cmd.CommandText = cmdText
                cmd.ExecuteNonQuery()

                'Cascade delete is in place, but just in case.
                cmdText = String.Format(<a>
                    DELETE FROM {0}.Equipment WHERE ObjectID = @objId
                    </a>.Value, dataSchema)

                cmd.CommandText = cmdText
                cmd.ExecuteNonQuery()

                retVal = True
              Catch ex As Exception
                Throw
              End Try
            End Using
          End Using
          scope.Complete()
        End Using
      Catch ex As Exception
        callInfo &= String.Format("Equipment Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Fetch"

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As EquipmentPackage
      Dim retVal As New EquipmentPackage
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

          localInfo = ""
          features = UpdateNames(features, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw New Exception("EquipmentTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retVal = ExtractFromRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
          Catch ex As Exception
            Throw New Exception("Equipment (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      If retVal.EquipmentRecord Is Nothing Then retVal = Nothing
      Return retVal
    End Function

    Private Function GetTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>Select  *   
          FROM {0}.Equipment as {1}   
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

    Private Function ExtractFromRow(ByVal dr As DataRow, ByRef callInfo As String) As EquipmentPackage
      Dim retVal As New EquipmentPackage
      Dim feature As New EquipmentRecord
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Try
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .MachineRowWidth = NullSafeInteger(dr.Item("MachineRowWidth"), -1)
            .NumberOfMachines = NullSafeInteger(dr.Item("NumberOfMachines"), -1)
            .NumberOfRows = NullSafeInteger(dr.Item("NumberOfRows"), -1)
          End With
        Catch ex As Exception
          Throw New Exception("EquipmentRecord (" & callInfo & ")", ex)
        End Try

        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .EquipmentRecord = feature
          .DatumRecord = datum
          .Info = ""
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
      Dim feature As New EquipmentRecord
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of EquipmentRecord)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (equipment deserialization): {1}", EH.GetCallerMethod(), ex.Message)
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
                                , ByVal feature As EquipmentRecord, ByRef callInfo As String) As Long
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

    Private Sub Insert(ByVal feature As EquipmentRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[NumberOfMachines] " & _
              ",[MachineRowWidth] " & _
              ",[NumberOfRows] "

            Dim insertValues As String = "@ObjectID" & _
              ",@NumberOfMachines " & _
              ",@MachineRowWidth " & _
              ",@NumberOfRows "

            cmdText = "INSERT INTO " & dataSchema & ".Equipment (" & insertFields & ") Values (" & insertValues & ")"

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@NumberOfMachines", SqlDbType.Int).Value = feature.NumberOfMachines
            cmd.Parameters.Add("@MachineRowWidth", SqlDbType.Int).Value = feature.MachineRowWidth
            cmd.Parameters.Add("@NumberOfRows", SqlDbType.Int).Value = feature.NumberOfRows

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

#Region "Update"

    Public Function Update(ByVal projectId As Long, ByVal usrId As Guid _
                        , ByVal featureId As Long, ByVal featuredata As String, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Dim feature As New EquipmentRecord
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of EquipmentRecord)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (equipment deserialization): {1}", EH.GetCallerMethod(), ex.Message)
          Return Nothing
        End Try

        feature.ObjectID = featureId
        localInfo = ""
        retVal = Update(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function Update(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal feature As EquipmentRecord, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Dim localInfo As String = ""
      Try
        Using scope As New TransactionScope
          localInfo = ""
          Dim pdUpdated As Integer = UpdateProjectDatumByDatumId(feature.ObjectID, usrId, Nothing, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

          localInfo = ""
          retVal = Update(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          scope.Complete()
        End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Private Function Update(ByVal feature As EquipmentRecord, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Dim localInfo As String = ""
      Try
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()
            Dim flds As String() = New String() {"[NumberOfMachines] ", "[MachineRowWidth] ", "[NumberOfRows] "}
            Dim vals As String() = New String() {"@NumberOfMachines ", "@MachineRowWidth ", "@NumberOfRows "}
            Dim sql As New StringBuilder("UPDATE ")

            Try
              sql.Append("" & dataSchema & ".Equipment ")
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
              Return retVal
            End Try

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@NumberOfMachines", SqlDbType.Int).Value = feature.NumberOfMachines
            cmd.Parameters.Add("@MachineRowWidth", SqlDbType.Int).Value = feature.MachineRowWidth
            cmd.Parameters.Add("@NumberOfRows", SqlDbType.Int).Value = feature.NumberOfRows

            cmd.CommandText = sql.ToString
            retVal = cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
        Throw
      End Try
      Return retVal
    End Function

#End Region

    Public Sub Duplicate(ByVal origProjectId As Long, ByVal newProjectId As Long, Optional ByRef callInfo As String = "")
      Dim localInfo As String = ""
      Try
        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        Dim feature As EquipmentPackage = Fetch(origProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If feature Is Nothing OrElse feature.EquipmentRecord Is Nothing Then Return

        Dim origFeatId As Long
        Dim newFeatId As Long

        origFeatId = feature.EquipmentRecord.ObjectID

        localInfo = ""
        newFeatId = Insert(newProjectId, usrId, feature.EquipmentRecord, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & "orig id " & origFeatId & ", new id " & newFeatId & ": " & localInfo

      Catch ex As Exception
        callInfo &= ErrorHandler.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

  End Class

End Namespace
