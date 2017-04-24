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

Namespace TerLoc.Model.Legacy

  Public Class XmlContourRecord
    Private Const NULL_VALUE As Integer = -1

    Public ContID As Long = NULL_VALUE
    Public ProjID As Long = NULL_VALUE
    Public XmlContourOld As String = ""
    Public XmlType As String = ""
    Public XmlContour As String = ""
  End Class

  Public Class XmlContourFull
    Inherits XmlContourRecord
    Private Const NULL_VALUE As Integer = -1

    Public XmlDoc As XDocument
  End Class

  <Serializable()> _
  Public Class XmlContourPackage
    Public xmlContourRecord As XmlContourFull
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  <Serializable()> _
  Public Class XmlContourPackageList
    Public xmlContours As New List(Of XmlContourPackage)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class XmlContourHelper
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString 
    Private dataSchema As String = CommonVariables.ProjectProductionSchema

    Public Function DeleteAll(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = String.Format(<a>
          DELETE FROM {0}.TERRACE_XMLCONTOUR
          WHERE PROJID = @projectId
        </a>.Value, dataSchema)

        Dim parm As New SqlParameter("@projectId", SqlDbType.VarChar)
        parm.Value = projectId.ToString

        Using conn As New SqlConnection(dataConn)
          If Not conn.State = ConnectionState.Open Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()
            Try
              cmd.CommandText = cmdText
              cmd.Parameters.Add(parm)
              cmd.ExecuteNonQuery()
              retVal = True
            Catch ex As Exception
              Throw
            End Try
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("XmlContourHelper DeleteAll error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function Delete(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByVal featureId As String, ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Try
        Dim cmdText As String = ""
        If featureId <> "" And IsNumeric(featureId) Then
          Using conn As New SqlConnection(dataConn)
            If Not conn.State = ConnectionState.Open Then conn.Open()
            Using cmd As SqlCommand = conn.CreateCommand()
              Try

                cmdText = String.Format(<a>
                    DELETE FROM {0}.TERRACE_XMLCONTOUR WHERE CONTID = @objId
                    </a>.Value, dataSchema)

                cmd.Parameters.Add("@objId", SqlDbType.BigInt).Value = CLng(featureId)
                cmd.CommandText = cmdText
                cmd.ExecuteNonQuery()
                retVal = True
              Catch ex As Exception
                Throw
              End Try
            End Using
          End Using
        End If
      Catch ex As Exception
        callInfo &= String.Format("XmlContourHelper Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Insert"

    Public Function InsertXmlContour(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal xdoc As XDocument, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Dim feature As New XmlContourFull
      Dim localInfo As String = ""
      Try
        feature.XmlDoc = xdoc
        feature.XmlType = "ORG"
        feature.XmlContour = xdoc.Declaration.ToString & xdoc.Root.Value

        localInfo = ""
        InsertXmlContour(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        retVal = feature.ContID
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function InsertXmlContour(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featuredata As String, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Dim feature As New XmlContourFull
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of XmlContourFull)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (contourXml deserialization): {1}", EH.GetCallerMethod(), ex.Message)
          Return Nothing
        End Try

        localInfo = ""
        InsertXmlContour(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        retVal = feature.ContID
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Sub InsertXmlContour(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByRef feature As XmlContourFull, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        InsertXmlContourToDatabase(feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Private Sub InsertXmlContourToDatabase(ByRef feature As XmlContourFull, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""

        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "[PROJID] " & _
              ",[XMLTYPE] " & _
              ",[XMLCONTOUR] "

            Dim insertValues As String = "@ProjectId" & _
              ",@XmlType " & _
              ",@XmlContour "

            cmdText = "INSERT INTO " & dataSchema & ".TERRACE_XMLCONTOUR (" & insertFields & _
              ") Values (" & insertValues & ")  SET @newOid = SCOPE_IDENTITY()"

            cmd.Parameters.Add("@ProjectId", SqlDbType.BigInt).Value = feature.ProjID
            cmd.Parameters.Add("@XmlType", SqlDbType.VarChar).Value = feature.XmlType
            cmd.Parameters.Add("@XmlContour", SqlDbType.NVarChar).Value = feature.XmlContour

            cmd.CommandText = cmdText
            Dim newOidParameter As New SqlParameter("@newOid", System.Data.SqlDbType.BigInt)
            newOidParameter.Direction = System.Data.ParameterDirection.Output
            cmd.Parameters.Add(newOidParameter)
            cmd.ExecuteNonQuery()
            feature.ContID = CLng(newOidParameter.Value)
          End Using
        End Using
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
        Throw
      End Try
    End Sub

#End Region

#Region "Fetch"

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As XmlContourPackageList
      Dim retVal As New XmlContourPackageList
      Dim localInfo As String = ""
      Try
        retVal = GetXmlContours(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function GetXmlContours(ByVal projectId As Long, ByRef callInfo As String) As XmlContourPackageList
      Dim retVal As New XmlContourPackageList
      Dim retXmlContours As List(Of XmlContourPackage)
      Dim localInfo As String = ""
      Try

        localInfo = ""
        retXmlContours = GetXmlContoursList(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        retVal.xmlContours = retXmlContours
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function GetXmlContoursList(ByVal projectId As Long, ByRef callInfo As String) As List(Of XmlContourPackage)
      Dim retVal As List(Of XmlContourPackage) = Nothing
      Dim retXmlContour As XmlContourPackage = Nothing
      Dim localInfo As String = ""
      Try
        Dim features As DataTable
        Try
          localInfo = ""
          features = GetXmlContoursTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        Catch ex As Exception
          Throw
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retXmlContour = ExtractXmlContourFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

            If retXmlContour IsNot Nothing Then
              If retVal Is Nothing Then retVal = New List(Of XmlContourPackage)
              retVal.Add(retXmlContour)
            End If
          Catch ex As Exception
            Throw
          End Try
        Next

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function GetXmlContoursTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>SELECT [CONTID]
              ,[PROJID]
              ,[XMLCONTOUROLD]
              ,[XMLTYPE]
              ,[XMLCONTOUR]
          FROM {0}.[TERRACE_XMLCONTOUR] 
          WHERE PROJID = @projectId</a>.Value, dataSchema)

        Dim parms As New List(Of SqlParameter)
        Dim parm As New SqlParameter("@projectId", SqlDbType.BigInt)
        parm.Value = projectId
        parms.Add(parm)

        localInfo = ""
        retVal = GetDataTable(dataConn, cmdText, parms.ToArray, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function ExtractXmlContourFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As XmlContourPackage
      Dim retVal As XmlContourPackage = Nothing
      Dim feature As XmlContourFull
      Dim localInfo As String = ""
      Try
        Try
          feature = New XmlContourFull
          With feature
            .ContID = NullSafeLong(dr.Item("CONTID"), -1)
            .ProjID = NullSafeLong(dr.Item("PROJID"), -1)
            .XmlType = NullSafeString(dr.Item("XMLTYPE"), "")
            .XmlContour = NullSafeString(dr.Item("XMLCONTOUR"), "")
            .XmlDoc = If(Not String.IsNullOrWhiteSpace(.XmlContour), New XDocument(.XmlContour), Nothing)
          End With
        Catch ex As Exception
          Throw New Exception("Extract XmlContourFull (" & callInfo & ")", ex)
        End Try

        retVal = New XmlContourPackage
        With retVal
          .xmlContourRecord = feature
          .info = ""
        End With

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

#End Region

  End Class

End Namespace