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

  Public Class CostShare
    Private Const NULL_VALUE As Integer = -1

    Public ObjectID As Integer = NULL_VALUE
    Public DescShort As String = ""
    Public Description As String = ""
    Public CostPerFt As Decimal = NULL_VALUE
  End Class
    
  Public Class CostShareHelper
    Private Shared dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private Shared dataSchema As String = CommonVariables.ProjectProductionSchema

#Region "Fetch"

    Public Shared Function GetCostShareIdByCost(cost As Single) As Integer
      Dim retVal As Integer = -1
      Dim localInfo As String = ""
      Try
        Dim costShares As List(Of CostShare) = Fetch(localInfo)
        For Each cs As CostShare In costShares
          If cs.CostPerFt.ToString("F2") = cost.ToString("F2") Then
            retVal = cs.ObjectID
            Exit Try
          End If
        Next
      Catch ex As Exception

      End Try
      Return retVal
    End Function

    Public Shared Function Fetch(ByRef callInfo As String) As List(Of CostShare)
      Dim retVal As New List(Of CostShare)
      Dim feature As CostShare
      Dim localInfo As String = ""
      Try 
        Dim features As DataTable
        Try
          localInfo = ""
          features = GetCostSharesTable(localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

        Catch ex As Exception
          Throw New Exception("CostSharesTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            feature = ExtractCostShareFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

            If feature IsNot Nothing Then
              retVal.Add(feature)
            End If
          Catch ex As Exception
            Throw New Exception("CostShare (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Get cost share table.
    ''' </summary> 
    Public Shared Function GetCostSharesTable(ByRef callInfo As String) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>SELECT *
          FROM {0}.[CostShare] ORDER BY Description</a>.Value, dataSchema)

        localInfo = ""
        retVal = GetDataTable(dataConn, cmdText, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Private Shared Function ExtractCostShareFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As CostShare
      Dim retVal As New CostShare
      Dim localInfo As String = ""
      Try
        With retVal
          .ObjectID = NullSafeInteger(dr.Item("ObjectID"), -1)
          .DescShort = NullSafeString(dr.Item("DescShort"), "")
          .Description = NullSafeString(dr.Item("Description"), "")
          .CostPerFt = NullSafeDecimal(dr.Item("CostPerFt"), -1)
        End With

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

#End Region

  End Class

End Namespace