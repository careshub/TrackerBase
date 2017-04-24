Option Explicit On
Option Strict On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports System.Transactions
Imports CommonFunctions
Imports CommonVariables
Imports EH = ErrorHandler

Namespace TerLoc.Model

  Public Class OperationRecord
    Private NULL_VALUE As Short = -1

    Public ObjectID As Long = NULL_VALUE
    Public OperationName As String = ""
    Public Address As String = ""
    Public City As String = ""
    Public State As String = ""
    Public Zip As String = ""
    Public Contact As String = ""
    Public ContactOfficePhone As String = ""
    Public ContactHomePhone As String = ""
    Public ContactEmail As String = ""
    Public CountyCode As Short = NULL_VALUE
    Public CountyName As String = ""
    Public StartCalYear As Short = NULL_VALUE
    Public StartCalMonth As Short = NULL_VALUE
    Public StartCropYear As Short = NULL_VALUE
  End Class

  Public Class OperationPackage
    Public OperationRecord As OperationRecord
    Public DatumRecord As ProjectDatum
    Public Info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class OperationHelper

    Private Shared dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private Shared dataSchema As String = CommonVariables.ProjectProductionSchema

    Public Shared Function Delete(ByVal projectId As Long, ByVal usrId As Guid _
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
                    DELETE FROM {0}.Operation WHERE ObjectID = @objId
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
        callInfo &= String.Format("Operation Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Fetch"

    Public Shared Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As OperationPackage
      Dim retVal As New OperationPackage
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
          Throw New Exception("OperationTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retVal = ExtractFromRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
          Catch ex As Exception
            Throw New Exception("Operation (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      If retVal.OperationRecord Is Nothing Then retVal = Nothing
      Return retVal
    End Function

    Public Shared Function GetTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>Select *   
          FROM {0}.Operation as {1}   
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

    Public Shared Function ExtractFromRow(ByVal dr As DataRow, ByRef callInfo As String) As OperationPackage
      Dim retVal As New OperationPackage
      Dim feature As New OperationRecord
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Try
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .OperationName = NullSafeString(dr.Item("OperationName"), "")
            .Address = NullSafeString(dr.Item("Address"), "")
            .City = NullSafeString(dr.Item("City"), "")
            .State = NullSafeString(dr.Item("State"), "")
            .Zip = NullSafeString(dr.Item("Zip"), "")
            .Contact = NullSafeString(dr.Item("Contact"), "")
            .ContactOfficePhone = NullSafeString(dr.Item("ContactOfficePhone"), "")
            .ContactHomePhone = NullSafeString(dr.Item("ContactHomePhone"), "")
            .ContactEmail = NullSafeString(dr.Item("ContactEmail"), "")
            .CountyCode = NullSafeShort(dr.Item("CountyCode"), -1)
            .CountyName = NullSafeString(dr.Item("CountyName"), "")
            .StartCalYear = NullSafeShort(dr.Item("StartCalYear"), -1)
            .StartCalMonth = NullSafeShort(dr.Item("StartCalMonth"), -1)
            .StartCropYear = NullSafeShort(dr.Item("StartCropYear"), -1)
          End With
        Catch ex As Exception
          Throw New Exception("OperationRecord (" & callInfo & ")", ex)
        End Try

        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .OperationRecord = feature
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

    Public Shared Function Insert(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featuredata As String, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Dim feature As New OperationRecord
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of OperationRecord)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (Operation deserialization): {1}", EH.GetCallerMethod(), ex.Message)
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

    Public Shared Function Insert(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal feature As OperationRecord, ByRef callInfo As String) As Long
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

    Private Shared Sub Insert(ByVal feature As OperationRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = <a>
             [ObjectID]
            ,[OperationName]
            ,[Address]
            ,[City]
            ,[State]
            ,[Zip]
            ,[Contact]
            ,[ContactOfficePhone]
            ,[ContactHomePhone]
            ,[ContactEmail]
            ,[CountyCode]
            ,[CountyName]
            ,[StartCalYear]
            ,[StartCalMonth]
            ,[StartCropYear] 
              </a>.Value

            Dim insertValues As String = <a>
             @ObjectID
            ,@OperationName
            ,@Address
            ,@City
            ,@State
            ,@Zip
            ,@Contact
            ,@ContactOfficePhone
            ,@ContactHomePhone
            ,@ContactEmail
            ,@CountyCode
            ,@CountyName
            ,@StartCalYear
            ,@StartCalMonth
            ,@StartCropYear 
              </a>.Value

            cmdText = "INSERT INTO " & dataSchema & ".Operation (" & insertFields & ") Values (" & insertValues & ")"

            With cmd.Parameters
              .Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
              .Add("@OperationName", SqlDbType.NVarChar, 30).Value = NullSafeSqlString(feature.OperationName)
              .Add("@Address", SqlDbType.NVarChar, 30).Value = NullSafeSqlString(feature.Address)
              .Add("@City", SqlDbType.NVarChar, 20).Value = NullSafeSqlString(feature.City)
              .Add("@State", SqlDbType.NVarChar, 2).Value = NullSafeSqlString(feature.State)
              .Add("@Zip", SqlDbType.NVarChar, 10).Value = NullSafeSqlString(feature.Zip)
              .Add("@Contact", SqlDbType.NVarChar, 30).Value = NullSafeSqlString(feature.Contact)
              .Add("@ContactOfficePhone", SqlDbType.NVarChar, 14).Value = NullSafeSqlString(feature.ContactOfficePhone)
              .Add("@ContactHomePhone", SqlDbType.NVarChar, 14).Value = NullSafeSqlString(feature.ContactHomePhone)
              .Add("@ContactEmail", SqlDbType.NVarChar, 40).Value = NullSafeSqlString(feature.ContactEmail)
              .Add("@CountyCode", SqlDbType.SmallInt).Value = NullSafeSqlShort(feature.CountyCode, -1)
              .Add("@CountyName", SqlDbType.NVarChar, 50).Value = NullSafeSqlString(feature.CountyName)
              .Add("@StartCalYear", SqlDbType.SmallInt).Value = NullSafeSqlShort(feature.StartCalYear, -1)
              .Add("@StartCalMonth", SqlDbType.SmallInt).Value = NullSafeSqlShort(feature.StartCalMonth, -1)
              .Add("@StartCropYear", SqlDbType.SmallInt).Value = NullSafeSqlShort(feature.StartCropYear, -1)
            End With

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

    Public Shared Function Update(ByVal projectId As Long, ByVal usrId As Guid _
                        , ByVal featureId As Long, ByVal featuredata As String, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Dim feature As New OperationRecord
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of OperationRecord)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (Operation deserialization): {1}", EH.GetCallerMethod(), ex.Message)
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

    Public Shared Function Update(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal feature As OperationRecord, ByRef callInfo As String) As Integer
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

    Private Shared Function Update(ByVal feature As OperationRecord, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Dim localInfo As String = ""
      Try
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()
            Dim flds As String() = <a>
             [OperationName]
            ,[Address]
            ,[City]
            ,[State]
            ,[Zip]
            ,[Contact]
            ,[ContactOfficePhone]
            ,[ContactHomePhone]
            ,[ContactEmail]
            ,[CountyCode]
            ,[CountyName]
            ,[StartCalYear]
            ,[StartCalMonth]
            ,[StartCropYear] 
              </a>.Value.Split(CommonVariables.commaSeparator)

            Dim vals As String() = <a>
             @OperationName
            ,@Address
            ,@City
            ,@State
            ,@Zip
            ,@Contact
            ,@ContactOfficePhone
            ,@ContactHomePhone
            ,@ContactEmail
            ,@CountyCode
            ,@CountyName
            ,@StartCalYear
            ,@StartCalMonth
            ,@StartCropYear 
              </a>.Value.Split(CommonVariables.commaSeparator)

            Dim sql As New StringBuilder("UPDATE ")

            Try
              sql.Append("" & dataSchema & ".Operation ")
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

            With cmd.Parameters
              .Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
              .Add("@OperationName", SqlDbType.NVarChar, 30).Value = NullSafeSqlString(feature.OperationName)
              .Add("@Address", SqlDbType.NVarChar, 30).Value = NullSafeSqlString(feature.Address)
              .Add("@City", SqlDbType.NVarChar, 20).Value = NullSafeSqlString(feature.City)
              .Add("@State", SqlDbType.NVarChar, 2).Value = NullSafeSqlString(feature.State)
              .Add("@Zip", SqlDbType.NVarChar, 10).Value = NullSafeSqlString(feature.Zip)
              .Add("@Contact", SqlDbType.NVarChar, 30).Value = NullSafeSqlString(feature.Contact)
              .Add("@ContactOfficePhone", SqlDbType.NVarChar, 14).Value = NullSafeSqlString(feature.ContactOfficePhone)
              .Add("@ContactHomePhone", SqlDbType.NVarChar, 14).Value = NullSafeSqlString(feature.ContactHomePhone)
              .Add("@ContactEmail", SqlDbType.NVarChar, 40).Value = NullSafeSqlString(feature.ContactEmail)
              .Add("@CountyCode", SqlDbType.SmallInt).Value = NullSafeSqlShort(feature.CountyCode)
              .Add("@CountyName", SqlDbType.NVarChar, 50).Value = NullSafeSqlString(feature.CountyName)
              .Add("@StartCalYear", SqlDbType.SmallInt).Value = NullSafeSqlShort(feature.StartCalYear)
              .Add("@StartCalMonth", SqlDbType.SmallInt).Value = NullSafeSqlShort(feature.StartCalMonth)
              .Add("@StartCropYear", SqlDbType.SmallInt).Value = NullSafeSqlShort(feature.StartCropYear)
            End With

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

    ''' <summary>
    ''' Returns ObjectId from Operation table for given ProjectId.
    ''' </summary>
    Public Shared Function GetOperationIdByProjectId(ByVal projectId As Long, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Dim localInfo As String = ""
      Try
        'Dim pkg As OperationPackage = Fetch(projectId, localInfo)
        'If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        'retVal = pkg.OperationRecord.ObjectID

        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = "SELECT Operation.ObjectID FROM " & _
                      dataSchema & ".Operation INNER JOIN " & dataSchema & ".ProjectDatum ON " & _
                      dataSchema & ".Operation.ObjectID=" & dataSchema & ".ProjectDatum.ObjectID " & _
                      " WHERE ProjectDatum.ProjectId='" & projectId & "'"

            If conn.State = ConnectionState.Closed Then conn.Open()
            Using readr As SqlDataReader = cmd.ExecuteReader
              While readr.Read
                retVal = NullSafeLong(readr("ObjectID"), -1)
              End While
            End Using
          End Using
        End Using

      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns county code from Operation table without state code.
    ''' </summary> 
    Public Shared Function GetCountyCodeForProject(ByVal projectId As Long, ByRef callInfo As String) As Short
      Dim retVal As Short
      Dim localInfo As String = ""
      Try
        'Dim pkg As OperationPackage = Fetch(projectId, localInfo)
        'If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        'retVal = pkg.OperationRecord.CountyCode

        Using conn As New SqlConnection(dataConn)
          Dim cmdText As String = String.Format(" SELECT {1}.CountyCode " & _
                    " FROM {0}.Operation as {1} " & _
                    " INNER JOIN {0}.ProjectDatum as {2} ON {1}.ObjectID = {2}.ObjectID " & _
                    " WHERE (PD.ProjectId = " & projectId & ")", dataSchema, "OP", "PD")

          Dim da As New SqlDataAdapter(cmdText, conn)
          Dim dt As New DataTable()
          da.Fill(dt)

          For Each dr As DataRow In dt.Rows
            retVal = NullSafeShort(dr("CountyCode"), -1)
            Exit For 'assume only 1 row and first row is correct
          Next
          If retVal > -1 Then
            If retVal > 1000 Then
              'strip state
              Dim tmpCnty As String = retVal.ToString
              tmpCnty = tmpCnty.Substring(tmpCnty.Length - 3)
              Short.TryParse(tmpCnty, retVal)
              If 0 = retVal Then retVal = -1 'return -1 as standard for something not found
            End If
          End If
        End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns state from operation table.
    ''' </summary> 
    Public Shared Function GetState(ByVal projectId As Long, ByRef callInfo As String) As String
      Dim retVal As String = ""
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = String.Format(" SELECT {1}.State " & _
                      " FROM {0}.Operation as {1} " & _
                      " INNER JOIN {0}.ProjectDatum as {2} ON {1}.ObjectID = {2}.ObjectID " & _
                      " WHERE (PD.ProjectId = " & projectId & ")", dataSchema, "OP", "PD")

            If conn.State = ConnectionState.Closed Then conn.Open()
            Using readr As SqlDataReader = cmd.ExecuteReader
              While readr.Read
                retVal = NullSafeString(readr("State"), "")
              End While
            End Using
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

  End Class

End Namespace
