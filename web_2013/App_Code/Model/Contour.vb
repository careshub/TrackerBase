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
Imports MDL = TerLoc.Model
Imports GGeom = GeoAPI.Geometries

Namespace TerLoc.Model

  Public Class ContourRecord
    Private Const NULL_VALUE As Integer = -1

    Public ObjectID As Long = NULL_VALUE
    Public Contour As Integer = 1
    Public Shape As String = ""
    Public Type As String = "" 'SMO for smooth
  End Class

  Public Class ContourFull
    Inherits ContourRecord
    Private Const NULL_VALUE As Integer = -1

    Public Length As Double = NULL_VALUE
    Public Coords As String = ""
  End Class

  <Serializable()> _
  Public Class ContourPackage
    Public contourRecord As ContourFull
    Public datumRecord As ProjectDatum
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  <Serializable()> _
  Public Class ContourPackageList
    Public contours As New List(Of ContourPackage)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class ContourHelper
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
                cmd.CommandText = cmdText.Replace("TABLENAME", "Contour")
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
        callInfo &= String.Format("ContourHelper DeleteAll error: {0}", ex.ToString)
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
                cmd.CommandText = cmdText.Replace("TABLENAME", "Contour")
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
        callInfo &= String.Format("ContourHelper DeleteAll error: {0}", ex.ToString)
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
                    DELETE FROM {0}.Contour WHERE ObjectID = @objId
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
        callInfo &= String.Format("ContourHelper Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Edit"

    Public Sub Update(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featureId As String, ByVal featureData As String, ByRef callInfo As String)
      Dim feature As New ContourFull
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Try
        Try
          feature = DeserializeJson(Of ContourFull)(featureData)
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
                                , ByVal feature As ContourFull, ByRef callInfo As String)
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

    Private Sub UpdateContourToDatabase(ByVal feature As ContourFull, ByRef callInfo As String)
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
              sql.Append("" & dataSchema & ".Contour ")
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

    Public Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As ContourPackageList
      Dim retVal As New ContourPackageList
      Dim contours As New List(Of ContourPackage)
      Dim localInfo As String = ""
      Try
        retVal = GetContours(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Function GetContours(ByVal projectId As Long, ByRef callInfo As String) As ContourPackageList
      Dim retVal As New ContourPackageList
      Dim retContours As List(Of ContourPackage)
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

    Public Function GetContoursList(ByVal projectId As Long, ByRef callInfo As String) As List(Of ContourPackage)
      Dim retVal As List(Of ContourPackage) = Nothing
      Dim retContour As ContourPackage = Nothing
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
              If retVal Is Nothing Then retVal = New List(Of ContourPackage)
              retVal.Add(retContour)
            End If
          Catch ex As Exception
            Throw New Exception("Contour (" & callInfo & ")", ex)
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
          FROM {0}.Contour as {1}   
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

    Public Function ExtractContourFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As ContourPackage
      Dim retVal As New ContourPackage
      Dim feature As New ContourFull
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Dim geom As GGeom.IGeometry = Nothing
        Try
          With feature
            .ObjectID = NullSafeLong(dr.Item("ObjectID"), -1)
            .Contour = NullSafeInteger(dr.Item("Contour"), -1)
            .Shape = NullSafeString(dr.Item("Shape"), "")
            .Type = NullSafeString(dr.Item("Type"), "")
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
          Throw New Exception("ContourFull (" & callInfo & ")", ex)
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
      Dim feature As New ContourFull
      Dim localInfo As String = ""
      Try
        Try
          localInfo = ""
          feature = DeserializeJson(Of ContourFull)(featuredata)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (contour deserialization): {1}", EH.GetCallerMethod(), ex.Message)
          Return Nothing
        End Try

        Try
          localInfo = ""
          CalcContour(feature, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Catch ex As Exception
          callInfo &= String.Format("{0} error (contour geom calc): {1}", EH.GetCallerMethod(), ex.Message)
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
                                , ByVal feature As ContourRecord, ByRef callInfo As String) As Long
      Dim localInfo As String = ""
      Dim datumId As Long = -1
      Try
        Using scope As New TransactionScope
          localInfo = ""
          datumId = MDL.ProjectDatumHelper.CreateNewProjectDatum(projectId, usrId, "", localInfo)
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

    Private Sub Insert(ByVal feature As ContourRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[Contour] " & _
              ",[Shape] " & _
              ",[Type] "

            Dim insertValues As String = "@ObjectID" & _
              ",@Contour " & _
              ",@Shape " & _
              ",@Type "

            cmdText = "INSERT INTO " & dataSchema & ".Contour (" & insertFields & ") Values (" & insertValues & ")"

            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = feature.ObjectID
            cmd.Parameters.Add("@Contour", SqlDbType.Int).Value = feature.Contour
            cmd.Parameters.Add("@Shape", SqlDbType.NVarChar).Value = feature.Shape
            cmd.Parameters.Add("@Type", SqlDbType.Char).Value = feature.Type

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
    ''' Gets shape and metrics for a feature containing coordinates in Lat/Lng
    ''' </summary> 
    Public Sub CalcContour(ByRef feature As ContourFull, ByRef callInfo As String)
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

    ''' <summary>
    ''' Get data row for Fortran's smooth xml output
    ''' </summary> 
    Public Function GetTerraceXmlContours(ByVal projectId As Long, ByRef callInfo As String) As DataTable
      Dim retVal As DataTable = Nothing
      Dim localInfo As String = ""
      Try
        Dim cmdText As String
        Dim params As New List(Of SqlParameter)
        Dim param As SqlParameter

        cmdText = <a>SELECT * FROM terloc.TERRACE_XMLCONTOUR 
            WHERE [PROJID]=@id</a>.Value

        param = New SqlParameter("@id", SqlDbType.VarChar)
        param.Value = projectId.ToString
        params.Add(param)

        retVal = GetDataTable(dataConn, cmdText, params.ToArray, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Convert smooth xml into contour features
    ''' </summary>
    Public Sub ConvertSmoothXml(ByVal projectId As Long, ByRef callInfo As String)
      Dim localInfo As String = ""
      Dim debugInfo As String = "debug: "
      Try
        'EXAMPLE
        '<?xml version="1.0" encoding="UTF-8"?>
        '<ARCXML version="1.1">
        '<RESPONSE>
        '<FEATURES>
        '  <FEATURE>
        '    <ENVELOPE minx="333501.577646929" miny="217343.892870548" maxx="333583.864502804" maxy="217444.799958395"/>
        '    <FIELDS SHAPE_LEN="934.04" CONTOUR="812.00" #SHAPE#="[Geometry]" #ID#="0" />
        '    <POLYLINE><PATH><COORDS>
        '      333549.001 217344.563;333550.471 217345.222;333551.940 217345.956;333553.427 217346.859;
        '      333560.998 217354.238;333562.297 217356.003;333563.537 217357.932;333564.830 217360.178;
        '      333578.750 217387.985;333579.655 217392.015;333580.219 217395.108;333580.719 217397.891;
        '  </COORDS></PATH></POLYLINE></FEATURE>
        '  <FEATURE>
        '    <ENVELOPE minx="333467.202302982" miny="217302.644286611" maxx="333653.188214699" maxy="217457.467446375"/>
        '    <FIELDS SHAPE_LEN="1361.85" CONTOUR="810.00" #SHAPE#="[Geometry]" #ID#="1" />
        '    <POLYLINE><PATH><COORDS>
        '      333535.532 217457.467;333533.359 217456.708;333531.094 217455.797;333527.970 217454.468;
        '      333637.778 217426.987;333639.850 217428.173;333641.819 217429.158;333643.682 217430.276;
        '      333645.416 217431.562;333647.010 217433.022;333648.506 217434.598;333649.866 217436.342;
        '  </COORDS></PATH></POLYLINE></FEATURE>
        '  <FEATURE>
        '    <ENVELOPE minx="333439.876983023" miny="217264.906998668" maxx="333650.344430703" maxy="217457.077302376"/>
        '    <FIELDS SHAPE_LEN="1410.17" CONTOUR="808.00" #SHAPE#="[Geometry]" #ID#="2" />
        '    <POLYLINE><PATH><COORDS>
        '      333492.906 217457.077;333490.998 217456.635;333488.913 217456.090;333486.932 217455.596;
        '      333599.815 217310.840;333601.509 217313.032;333603.628 217315.790;333605.133 217317.817;
        '      333606.526 217319.588;333607.913 217321.374;333609.312 217323.032;333610.781 217324.608;
        '  </COORDS></PATH></POLYLINE></FEATURE>
        '<FEATURECOUNT count="3" hasmore="false" />
        '</FEATURES>
        '</RESPONSE>
        '</ARCXML>

        localInfo = ""
        Dim xmlList As DataTable = GetTerraceXmlContours(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'If xmlList Is Nothing OrElse xmlList.Rows.Count < 1 Then
        '  debugInfo &= "No contours found" 'this should not happen since I'm writing the ORG record before calling fortran
        '  Exit Try 'ABORT, records not written
        'End If

        Dim xmlRecs As DataRow() = xmlList.Select("XMLTYPE='SMO'")
        Dim xmlRec As DataRow = Nothing
        If xmlRecs.Count > 0 Then xmlRec = xmlRecs(0)

        If xmlRec Is Nothing Then
          debugInfo &= "No smooth contours created" ' ----- DEBUG
          Exit Try 'ABORT, records not written
        End If
        Dim smoothXml As String = NullSafeString(xmlRec.Item("XMLCONTOUR"), "")
        If String.IsNullOrWhiteSpace(smoothXml) Then
          Throw New ArgumentNullException("XMLCONTOUR")
        End If

        Dim newFeature As ContourFull
        Dim fields As XElement
        Dim elev As Single
        Dim outputId As Integer
        Dim coords As String
        Dim geom As GGeom.IGeometry
        Dim wkb As String

        'remove possible illegals
        smoothXml = smoothXml.Replace("#", "").Replace("[", "").Replace("]", "")
        Dim doc As XDocument = XDocument.Parse(smoothXml)
        Dim root As XElement = doc.Root

        localInfo = ""
        DeleteAllByType(projectId, Guid.Empty, "SMO", localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        For Each xelem As XElement In root.Descendants("FEATURE")
          fields = xelem.Descendants("FIELDS").First
          elev = NullSafeSingle(fields.Attribute("CONTOUR").Value, Single.MinValue)
          debugInfo &= "  elev: " & elev & "  " ' ----- DEBUG
          outputId = NullSafeInteger(fields.Attribute("ID").Value, -1)
          debugInfo &= "  outputId: " & outputId & "  " ' ----- DEBUG
          coords = NullSafeString(xelem.Descendants("COORDS").First.Value, "")
          geom = MakeSmoothXmlGeom(coords, localInfo)
          wkb = ConvertGeometryToWkb(geom, localInfo)

          newFeature = New ContourFull
          With newFeature
            .Contour = CInt(elev)
            .Coords = coords
            .Length = -1
            .ObjectID = -1
            .Shape = wkb
            .Type = "SMO"
          End With

          Dim usr As Model.User = Model.UserHelper.Fetch(HttpContext.Current.User.Identity.Name, Nothing)
          Dim usrId As Guid = usr.UserId

          localInfo = ""
          Insert(projectId, usrId, newFeature, localInfo)
          If localInfo.Contains("error") Then callInfo &= localInfo
        Next

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      'SendEmail(ozzyEmail, ozzyEmail, "smooth xml", debugInfo & "  ; callInfo: " & callInfo, Nothing, "", "", Nothing, False)

    End Sub

    ''' <summary>
    ''' Convert smooth xml coords into a geometry
    ''' </summary>
    Public Function MakeSmoothXmlGeom(ByVal coords As String, ByRef callInfo As String) As GGeom.IGeometry
      Dim retVal As GGeom.IGeometry = Nothing
      Dim localInfo As String = ""
      Try
        'EXAMPLE 
        '    <POLYLINE><PATH><COORDS>
        '      333549.001 217344.563;333550.471 217345.222;333551.940 217345.956;333553.427 217346.859;
        '      333560.998 217354.238;333562.297 217356.003;333563.537 217357.932;333564.830 217360.178;
        '      333578.750 217387.985;333579.655 217392.015;333580.219 217395.108;333580.719 217397.891;
        '  </COORDS></PATH></POLYLINE></FEATURE> 

        localInfo = ""
        coords = Fortran.DeFormatFortranCoords(coords, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        'SendEmail(ozzyEmail, ozzyEmail, "smooth xml", coords, Nothing, "", "", Nothing, False) ' ----- DEBUG
        localInfo = ""
        coords = ConvertUtmCoordsToLatLon(coords, 15, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo

        localInfo = ""
        retVal = CreateLineStringFromCoordString(coords, localInfo, False)
        If localInfo.Contains("error") Then callInfo &= localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Sub Duplicate(ByVal origProjectId As Long, ByVal newProjectId As Long, Optional ByRef callInfo As String = "")
      Dim localInfo As String = ""
      Try
        Dim feat As ContourRecord
        Dim pkg As ContourPackage
        Dim featureList As ContourPackageList

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