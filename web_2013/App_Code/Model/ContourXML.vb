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

  Public Class ContourXmlRecord
    Private Const NULL_VALUE As Integer = -1

    Public ObjectID As Long = NULL_VALUE
    Public XmlType As String = ""
    Public XmlContour As String = ""
  End Class

  Public Class ContourXmlFull
    Inherits ContourXmlRecord
    Private Const NULL_VALUE As Integer = -1

    Public XmlDoc As XDocument
  End Class

  <Serializable()> _
  Public Class ContourXmlPackage
    Public contourXmlRecord As ContourXmlFull
    Public datumRecord As ProjectDatum
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  <Serializable()> _
  Public Class ContourXmlPackageList
    Public contourXmls As New List(Of ContourXmlPackage)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class ContourXmlHelper
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString 
    Private dataSchema As String = CommonVariables.ProjectProductionSchema

    Public Function DeleteAll(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim numRecs As Integer = 0
      Try
        Dim allContourXmls As DataTable = GetContourXmlTable(projectId, Nothing)
        Dim contIds As New List(Of Long)
        Dim contId As Long
        For contIx As Integer = 0 To allContourXmls.Rows.Count - 1
          contId = NullSafeLong(allContourXmls.Rows(contIx).Item("ObjectID"), -1)
          contIds.Add(contId)
        Next

        'If contIds.Count < 1 Then SendOzzy(ErrorHandler.GetCallerMethod, "no records", Nothing) ' ----- DEBUG
        If contIds.Count < 1 Then Return True

        Dim cmdIds = String.Join(",", contIds.ToArray)
        'SendOzzy(ErrorHandler.GetCallerMethod, cmdIds, Nothing) ' ----- DEBUG
        Dim cmdText As String = <a>
          DELETE FROM terloc.terloc.TABLENAME
          WHERE ObjectID IN (IDSTRING)
          </a>.Value.Replace("IDSTRING", cmdIds)

        'SendOzzy(ErrorHandler.GetCallerMethod, cmdText, Nothing) ' ----- DEBUG
        Using scope As New TransactionScope
          Using conn As New SqlConnection(dataConn)
            If conn.State = ConnectionState.Closed Then conn.Open()
            Using cmd As SqlCommand = conn.CreateCommand()
              Try
                cmd.CommandText = cmdText.Replace("TABLENAME", "ContourXml")
                numRecs += cmd.ExecuteNonQuery

                cmd.CommandText = cmdText.Replace("TABLENAME", "ProjectDatum")
                numRecs += cmd.ExecuteNonQuery

              Catch ex As Exception
                Throw
              End Try
            End Using
          End Using
          scope.Complete()
        End Using
      Catch ex As Exception
        callInfo &= String.Format("ContourXmlHelper DeleteAll error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function Delete(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByVal featureId As String, ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim numRecs As Integer = 0
      Try
        Dim cmdText As String = ""
        If featureId <> "" And IsNumeric(featureId) Then
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
                  numRecs += cmd.ExecuteNonQuery

                  cmdText = String.Format(<a>
                    DELETE FROM {0}.ContourXml WHERE ObjectID = @objId
                    </a>.Value, dataSchema)

                  cmd.CommandText = cmdText
                  numRecs += cmd.ExecuteNonQuery

                  If numRecs <> 2 Then Throw New Exception("Failed to execute both deletions.")

                Catch ex As Exception
                  Throw
                End Try
              End Using
            End Using
            scope.Complete()
          End Using
        End If
      Catch ex As Exception
        callInfo &= String.Format("ContourXmlHelper Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Edit"

    Public Sub Update(ByVal projectId As Long, ByVal usrId As Guid _
                , ByVal featureId As Long, ByVal xdoc As XDocument, ByRef callInfo As String)

      Dim feature As New ContourXmlFull
      Dim localInfo As String = ""
      Try
        feature.ObjectID = featureId
        feature.XmlType = "ORG"
        feature.XmlContour = xdoc.Declaration.ToString & xdoc.Root.Value
        feature.XmlDoc = xdoc

        localInfo = ""
        Update(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try

    End Sub

    Public Sub Update(ByVal projectId As Long, ByVal usrId As Guid _
               , ByVal featureId As String, ByVal featureData As String, ByRef callInfo As String)
      Dim feature As New ContourXmlFull
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Try
        Try
          feature = DeserializeJson(Of ContourXmlFull)(featureData)
          feature.ObjectID = CInt(featureId)
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (feature deserialization): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

        localInfo = ""
        Update(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Public Sub Update(ByVal projectId As Long, ByVal usrId As Guid _
                     , ByVal feature As ContourXmlFull, ByRef callInfo As String)
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
        UpdateContourXmlToDatabase(feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'scope.Complete()
        'End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
    End Sub

    Private Sub UpdateContourXmlToDatabase(ByVal feature As ContourXmlFull, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()
            Dim flds As String() = New String() {"[XmlType] ", "[XmlContour] "}
            Dim vals As String() = New String() {"@XmlType ", "@XmlContour "}
            Dim sql As New StringBuilder("UPDATE ")

            Try
              sql.Append("" & dataSchema & ".ContourXml ")
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
            cmd.Parameters.Add("@XmlType", SqlDbType.Char, 10).Value = feature.XmlType
            cmd.Parameters.Add("@XmlContour", SqlDbType.NVarChar).Value = feature.XmlContour

            cmd.CommandText = sql.ToString
            cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

#End Region

#Region "Insert"

    Public Function InsertContourXml(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal xdoc As XDocument, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Dim feature As New ContourXmlFull
      Dim localInfo As String = ""
      Try
        feature.XmlDoc = xdoc
        feature.XmlType = "ORG"
        feature.XmlContour = xdoc.Declaration.ToString & xdoc.Root.Value

        localInfo = ""
        retVal = InsertContourXml(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function InsertContourXml(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featuredata As String, ByRef callInfo As String) As Long
      Dim retVal As Long = -1
      Dim feature As New ContourXmlFull
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of ContourXmlFull)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (contourXml deserialization): {1}", EH.GetCallerMethod(), ex.Message)
          Return Nothing
        End Try

        localInfo = ""
        retVal = InsertContourXml(projectId, usrId, feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function InsertContourXml(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal feature As ContourXmlFull, ByRef callInfo As String) As Long
      Dim localInfo As String
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
          InsertContourXmlToDatabase(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

          scope.Complete()
        End Using

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return datumId
    End Function

    Private Sub InsertContourXmlToDatabase(ByVal feature As ContourXmlFull, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        Dim cleanedXml As String = CleanContourXml(feature.XmlContour, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        feature.XmlContour = cleanedXml

        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[XMLTYPE] " & _
              ",[XMLCONTOUR] "

            Dim insertValues As String = "@ObjectID" & _
              ",@XmlType " & _
              ",@XmlContour "

            cmdText = "INSERT INTO " & dataSchema & ".ContourXML (" & insertFields & ") Values (" & insertValues & ")"

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@XmlType", SqlDbType.Char, 10).Value = feature.XmlType
            cmd.Parameters.Add("@XmlContour", SqlDbType.NVarChar).Value = feature.XmlContour

            cmd.CommandText = cmdText
            cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
        Throw
      End Try
    End Sub

    Public Shared Function CleanContourXml(ByVal xml As String, ByRef callInfo As String) As String
      Dim retVal As String = xml
      Try
        'not sure this matters for fortran
        retVal = retVal.Replace("utf-8", "UTF-8") _
          .Replace(" standalone=""yes""", "") _
          .Replace(" standalone=""no""", "")
        'retVal = retVal.Replace("\r", "").Replace("\n", "") 'didn't work
        retVal = retVal.Replace(Environment.NewLine, "") 'works

        'take out extra spaces between tags, not sure if they matter or not for fortran
        Dim options As RegexOptions = RegexOptions.None
        Dim reg As New Regex(">[ ]{2,}<", options)
        retVal = reg.Replace(retVal, "><") 'stunningly worked on first try!!

        retVal = AddArcXmlTags(retVal, Nothing)
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Shared Function AddArcXmlTags(ByVal xml As String, ByRef callInfo As String) As String
      Dim retVal As String = xml
      Try
        retVal = RemoveArcXmlTags(retVal, Nothing) 'Make sure they're out before adding
        retVal = retVal.Replace("SHAPE", "#SHAPE#") _
          .Replace("ID", "#ID#")
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Shared Function RemoveArcXmlTags(ByVal xml As String, ByRef callInfo As String) As String
      Dim retVal As String = xml
      Try
        retVal = retVal.Replace("#SHAPE#", "SHAPE") _
          .Replace("#ID#", "ID")
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

#End Region

#Region "Fetch"

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As ContourXmlPackageList
      Dim retVal As New ContourXmlPackageList
      Dim contourXmls As New List(Of ContourXmlPackage)
      Dim localInfo As String = ""
      Try
        retVal = GetContourXmls(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function GetContourXmls(ByVal projectId As Long, ByRef callInfo As String) As ContourXmlPackageList
      Dim retVal As New ContourXmlPackageList
      Dim retContourXmls As List(Of ContourXmlPackage)
      Dim retInfo As String = ""
      Dim localInfo As String = ""
      Try

        localInfo = ""
        retContourXmls = GetContourXmlsList(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        retVal.contourXmls = retContourXmls
        retVal.info = retInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function GetContourXmlsList(ByVal projectId As Long, ByRef callInfo As String) As List(Of ContourXmlPackage)
      Dim retVal As List(Of ContourXmlPackage) = Nothing
      Dim retContourXml As ContourXmlPackage = Nothing
      Dim localInfo As String = ""
      Try
        Dim features As DataTable

        Try
          localInfo = ""
          features = GetContourXmlTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

          localInfo = ""
          features = UpdateNames(features, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw New Exception("ContourXmlsTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In features.Rows
          Try
            localInfo = ""
            retContourXml = ExtractContourXmlFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

            If retContourXml IsNot Nothing Then
              If retVal Is Nothing Then retVal = New List(Of ContourXmlPackage)
              retVal.Add(retContourXml)
            End If
          Catch ex As Exception
            Throw New Exception("ContourXml (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    Public Function GetContourXmlTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>Select  *   
          FROM {0}.ContourXml as {1}   
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

    Public Function ExtractContourXmlFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As ContourXmlPackage
      Dim retVal As New ContourXmlPackage
      Dim feature As New ContourXmlFull
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try 
        Try 
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .XmlType = NullSafeString(dr.Item("XmlType"), "")
            .XmlContour = NullSafeString(dr.Item("XmlContour"), "")
            .XmlDoc = Nothing
            If Not String.IsNullOrWhiteSpace(.XmlContour) Then
              Dim clean As String = RemoveArcXmlTags(.XmlContour, Nothing)
              Dim doc As XDocument = XDocument.Parse(clean)
              .XmlDoc = doc
            End If
          End With
        Catch ex As Exception
          Throw New Exception("ContourXmlFull (" & callInfo & ")", ex)
        End Try
        
        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
         
        With retVal
          .contourXmlRecord = feature
          .datumRecord = datum
          .info = ""
        End With

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2})", EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

#End Region

  End Class

End Namespace