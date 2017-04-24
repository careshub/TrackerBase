Option Explicit On
Option Strict On

#Region "Imports"
Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Data
Imports System.Collections
Imports System.Xml
Imports System.Xml.Linq
Imports System.Collections.Generic
Imports System.Data.SqlClient
Imports System.Reflection
Imports System.Reflection.MethodBase
Imports System.Threading

Imports EH = ErrorHandler
Imports CF = CommonFunctions
Imports CV = CommonVariables
Imports GTA = GIS.GISToolsAddl
Imports MDL = TerLoc.Model
Imports GeoAPI.Geometries
Imports NetTopologySuite
Imports NetTopologySuite.Features
Imports NetTopologySuite.Geometries
Imports NetTopologySuite.IO

#End Region

Public Class GISFieldUploadInfo
  Public ShapefileName As String = ""
  Public ShapeType As String = ""
  Public ColCount As Integer = -1
  Public RowCount As Integer = -1
  Public Columns As List(Of String)
End Class

Public Class GISFields
  Public fileName As String = ""
  Public shapeType As String
  Public colCount As Integer = -1
  Public rowCount As Integer = -1
  Public FIDCol As String = ""

End Class

Public Class UploadTools

#Region "Module variables"
  Private Const sqMtrsPerAcre As Double = 4046.8564224
  Private Shared dataSchema As String = CF.GetDataSchemaName
  Private Shared dataConn As String = CF.GetBaseDatabaseConnString
#End Region

  ''' <summary>
  ''' Only used with old database
  ''' </summary>
  Public Shared Sub WriteContourInfo(ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      Dim connStr As String = <a>Data Source=chulak\chulak;Initial Catalog=soilcon;Persist Security Info=True;
              User ID=soilcon;Password=noclios</a>.Value
      Dim oldConnStr As String = <a>Data Source=128.206.9.101;Network Library=DBMSSOCN;Initial Catalog=soilcon;
              User ID=soilcon;Password=noclios;</a>.Value

      Using conn As New SqlConnection(connStr)
        If conn.State = ConnectionState.Closed Then conn.Open()
        Using cmd As SqlCommand = conn.CreateCommand()
          Dim cmdText As String = <a>SELECT TOP 1 [CONTID]
                ,[PROJID]
                ,[XMLCONTOUROLD]
                ,[XMLTYPE]
                ,[XMLCONTOUR]
              FROM [soilcon].[soilcon].[TERRACE_XMLCONTOUR]
              WHERE contid = 1625
              ORDER BY [PROJID] DESC
          </a>.Value
          cmd.CommandText = cmdText

          Using readr As SqlDataReader = cmd.ExecuteReader
            While readr.Read
              Dim xml As String = CF.NullSafeString(readr("xmlcontour"), "xmlcontour not found")
              Dim projId As String = CF.NullSafeString(readr("projid"), "projid not found")
              WriteTextFile(xml, projId, localInfo)
              If Not String.IsNullOrWhiteSpace(localInfo) Then callInfo &= localInfo
            End While
          End Using
        End Using
      End Using

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Only used with old database. Project id is hardwired.
  ''' </summary>
  Public Shared Function WriteTextFile(ByVal text As String, ByVal filename As String, ByRef callInfo As String) As String
    Dim fname As String = ""
    Dim localInfo As String = ""
    Try

      Dim prjFoldr As String = CF.GetProjectFolderByProjectId(1, localInfo)
      If localInfo.Contains("error") Then callInfo &= localInfo

      fname = filename & ".txt"
      fname = System.IO.Path.Combine(prjFoldr, fname)
      Dim objStreamWriter As StreamWriter
      objStreamWriter = File.CreateText(fname)
      objStreamWriter.WriteLine(text)
      objStreamWriter.Close()
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return fname
  End Function

  Public Shared Function GetShapefileInfo(ByVal fileName As String, ByRef callInfo As String) As GISFieldUploadInfo
    Dim retVal As New GISFieldUploadInfo
    Dim retColumns As New List(Of String)
    Dim localInfo As String = ""
    Try
      Dim factory As New GeometryFactory()
      Dim shapeFileDataReader As New ShapefileDataReader(fileName, factory)
      retVal.ShapefileName = Path.GetFileName(fileName)

      'Display the shapefile type
      Dim shpHeader As ShapefileHeader = shapeFileDataReader.ShapeHeader
      Dim shapeType As String = shpHeader.ShapeType.ToString
      retVal.ShapeType = shapeType

      'Display summary information about the Dbase file
      Dim header As DbaseFileHeader = shapeFileDataReader.DbaseHeader
      retVal.ColCount = header.Fields.Length
      retVal.RowCount = header.NumRecords
      Dim fldDescriptor As DbaseFieldDescriptor
      For i As Integer = 0 To header.NumFields - 1
        fldDescriptor = header.Fields(i)
        retColumns.Add(fldDescriptor.Name)
      Next

      'Close and free up any resources
      shapeFileDataReader.Close()
      shapeFileDataReader.Dispose()

      retVal.Columns = retColumns
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  Public Shared Sub ImportGisContours(ByVal projectId As Long, ByVal fileName As String, _
                           ByVal contourCol As String, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      Dim fullFileName As String = Path.Combine(CF.GetProjectFolderByProjectId(projectId, Nothing), "UserDocuments\GISUpload", fileName)

      Dim isUTM As Boolean = True
      Dim prjInfo As String = ""
      Using sr As New StreamReader(fullFileName.Replace(".shp", ".prj"))
        prjInfo = sr.ReadToEnd()
        sr.Close()
        'TODO: not sure how to use this yet. There is a service that will try to parse it. 
        'or I could search for zone 14 vs. 15 for MO zones
      End Using

      Dim pm As New PrecisionModel()
      Dim factory As New GeometryFactory(pm, 26915)
      Dim shapeFileDataReader As New ShapefileDataReader(fullFileName, factory)

      'Display the shapefile type
      Dim shpHeader As ShapefileHeader = shapeFileDataReader.ShapeHeader
      Dim shapeType As String = shpHeader.ShapeType.ToString
      If Not shapeType.ToLower.Contains("linestring") And Not shapeType.ToLower.Contains("line") Then
        callInfo &= " error: Line shapefile not found"
      End If

      Dim header As DbaseFileHeader = shapeFileDataReader.DbaseHeader

      'Reset the pointer to the start of the shapefile, just in case
      shapeFileDataReader.Reset()

      'Read through all records of the shapefile (geometry and attributes) into a feature collection 
      Dim features As New ArrayList
      Dim fidColName As String = ""
      Dim contourColName As String = ""
      While shapeFileDataReader.Read()
        Dim feature As New Feature()
        Dim attributesTable As New AttributesTable()
        Dim keys As String() = New String(header.NumFields - 1) {}
        Dim geometry As Geometry = DirectCast(shapeFileDataReader.Geometry, Geometry)
        For i As Integer = 0 To header.NumFields - 1
          Dim fldDescriptor As DbaseFieldDescriptor = header.Fields(i)
          keys(i) = fldDescriptor.Name
          attributesTable.AddAttribute(fldDescriptor.Name, shapeFileDataReader.GetValue(i))
        Next
        feature.Geometry = geometry
        feature.Attributes = attributesTable
        features.Add(feature)
      End While

      localInfo = ""
      ProcessUploadedContours(projectId, features, fidColName, contourCol, localInfo)
      If localInfo.Contains("error") Then callInfo &= " process: " & localInfo & "  "

      localInfo = ""
      ProcessUploadedContoursXml(projectId, features, contourCol, localInfo)
      If localInfo.Contains("error") Then callInfo &= " xml: " & localInfo & "  "

      'Close and free up any resources
      shapeFileDataReader.Close()
      shapeFileDataReader.Dispose()

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Prepare features for db insertion
  ''' </summary>
  ''' <param name="zone">15 is default for most of MO</param> 
  Public Shared Sub ProcessUploadedContours(ByVal projectId As Long, ByVal features As ArrayList, _
                      ByVal idCol As String, ByVal contourCol As String, ByRef callInfo As String, _
                      Optional ByRef zone As Integer = 15, Optional isSouth As Boolean = False)
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = MDL.UserHelper.GetCurrentUser(Nothing).UserId
      Dim newFeature As MDL.ContourRawFull
      Dim helpr As New MDL.ContourRawHelper()
      Dim asGeom As IGeometry
      Dim countr As Integer = 0
      Dim shape As String = ""
      Dim length As Double = 0
      Dim coords As String = ""
      'loop thru new features
      For Each feat As Feature In features
        countr += 1
        asGeom = feat.Geometry

        If asGeom.GeometryType <> GetType(LineString).Name Then
          callInfo &= "error: got " & asGeom.GeometryType
          Continue For
        End If
        localInfo = ""
        Dim asGeomGeo = GTA.TransformGeomFromUtmToGeo(asGeom, zone, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
        coords = CalculateMetricsForGeometry(asGeomGeo, shape, length, projectId, True, localInfo)

        newFeature = New MDL.ContourRawFull
        With newFeature
          .ObjectID = -1
          .Contour = CF.NullSafeInteger(feat.Attributes(contourCol), -1)
          .Shape = shape
          .Length = length
          .Coords = coords
        End With

        localInfo = ""
        helpr.Insert(projectId, usrId, newFeature, localInfo)
        If localInfo.Contains("error") Then callInfo &= localInfo
      Next

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  Public Shared Sub ProcessUploadedContoursXml(ByVal projectId As Long, ByVal features As ArrayList, _
                       ByVal contourCol As String, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = MDL.UserHelper.GetCurrentUser(Nothing).UserId
      Dim newFeature As MDL.ContourXmlFull
      Dim helpr As New MDL.ContourXmlHelper()
      Dim asGeom As IGeometry
      Dim coords As Coordinate()

      Dim docX As New XDeclaration("1.0", "UTF-8", Nothing)
      Dim attrX As New XAttribute("version", "1.1")
      Dim arcX As New XElement("ARCXML", attrX)
      Dim respX As New XElement("RESPONSE")
      Dim featsX As New XElement("FEATURES")
      Dim featX As XElement
      Dim envX As XElement
      Dim fieldsX As XElement
      Dim polyX As XElement
      Dim pathX As XElement
      Dim coordsX As XElement
      Dim coordsStr As String
      Dim count As Integer = 0
      Dim boundry As Envelope
      'loop thru new features
      For Each feat As Feature In features
        count += 1
        'If count > 1 Then Continue For ' ----- DEBUG
        asGeom = feat.Geometry

        If asGeom.GeometryType <> GetType(LineString).Name Then
          callInfo &= "error: got " & asGeom.GeometryType
          Continue For
        End If

        boundry = asGeom.EnvelopeInternal

        coords = asGeom.Coordinates
        coordsStr = GTA.GetCoordsStringFromGeom(asGeom, localInfo)

        coordsStr = Fortran.FormatFortranCoords(coordsStr, Nothing)
        coordsX = New XElement("COORDS", coordsStr)
        pathX = New XElement("PATH", coordsX)
        polyX = New XElement("POLYLINE", pathX)
        fieldsX = New XElement("FIELDS")
        attrX = New XAttribute("SHAPE_LEN", asGeom.Length)
        fieldsX.Add(attrX)
        attrX = New XAttribute("CONTOUR", feat.Attributes.Item("CONTOUR"))
        fieldsX.Add(attrX)
        attrX = New XAttribute("SHAPE", "[Geometry]")
        fieldsX.Add(attrX)
        attrX = New XAttribute("ID", count)
        fieldsX.Add(attrX)
        envX = New XElement("ENVELOPE")
        attrX = New XAttribute("minx", boundry.MinX)
        envX.Add(attrX)
        attrX = New XAttribute("miny", boundry.MinY)
        envX.Add(attrX)
        attrX = New XAttribute("maxx", boundry.MaxX)
        envX.Add(attrX)
        attrX = New XAttribute("maxy", boundry.MaxY)
        envX.Add(attrX)
        featX = New XElement("FEATURE")
        featX.Add(envX)
        featX.Add(fieldsX)
        featX.Add(polyX)
        featsX.Add(featX)
      Next
      featsX.Add(New XElement("FEATURECOUNT", New XAttribute("count", count)))
      respX.Add(featsX)
      arcX.Add(respX)
      Dim Xdoc As New XDocument(docX, arcX)

      Dim XString As String = CF.XDocToStringWithDeclaration(Xdoc)

      localInfo = ""
      Dim path As String = CommonFunctions.GetProjectFolderByProjectId(projectId, localInfo)
      Xdoc.Save(System.IO.Path.Combine(path, "newxml.xml"))

      Dim doc As XmlDocument = New XmlDocument()
      Using readr As System.Xml.XmlReader = Xdoc.CreateReader
        doc.Load(readr)
      End Using

      newFeature = New MDL.ContourXmlFull
      With newFeature
        .ObjectID = -1
        .XmlContour = XString
        .XmlDoc = Xdoc
        .XmlType = "ORG"
      End With

      localInfo = ""
      helpr.InsertContourXml(projectId, usrId, newFeature, localInfo)
      If localInfo.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Calculate metric and wkb from a geometry
  ''' </summary>
  ''' <returns>Trimmed or adjusted coords string</returns>
  ''' <remarks></remarks>
  Public Shared Function CalculateMetricsForGeometry(ByVal geom As IGeometry, ByRef wkb As String,
         ByRef size As Double, ByVal projectId As Long, ByVal convertToUtm As Boolean, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim geomType As String = geom.GeometryType
      Select Case geomType
        Case GetType(Point).Name
          'nothing to do 
        Case GetType(LineString).Name
          localInfo = ""
          If convertToUtm = True Then
            size = GTA.GetLengthFromLatLngLinestring(CType(geom, ILineString), localInfo)
          Else
            size = GTA.GetLengthFromUtmLinestring(CType(geom, ILineString), localInfo)
          End If
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Case GetType(Polygon).Name
          localInfo = ""
          size = GTA.GetAreaFromLatLngPolygon(CType(geom, IPolygon), localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Case GetType(MultiPolygon).Name
          localInfo = ""
          size = GTA.GetAreaFromLatLngMultiPolygon(CType(geom, IMultiPolygon), localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Case Else
          Throw New ArgumentException("Geometry type " & geomType & " not calculable.")
      End Select
      localInfo = ""
      retVal = GTA.GetCoordsStringFromGeom(geom, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      localInfo = ""
      Dim coordsPrecision As Integer = CV.CoordinatePrecision
      retVal = GTA.TrimCoords(retVal, coordsPrecision, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      'Convert shape to wkb
      localInfo = ""
      wkb = GTA.ConvertGeometryToWkb(geom, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ' ''' <summary>
  ' ''' Calculate metric and wkb from a geometry
  ' ''' </summary>
  ' ''' <returns>Trimmed or adjusted coords string</returns>
  ' ''' <remarks></remarks>
  'Public Shared Function CalculateNewContourFromPoly(ByVal newPoly As ILineString, ByRef wkb As String,
  '                  ByRef length As Double, ByVal projectId As Long, ByRef callInfo As String) As String
  '  Dim retVal As String = ""
  '  Dim localInfo As String = ""
  '  Try
  '    localInfo = ""
  '    length = GTA.GetLengthFromLatLngLinestring(newPoly, localInfo)
  '    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
  '    localInfo = ""
  '    retVal = GTA.GetCoordsStringFromGeom(newPoly, localInfo)
  '    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
  '    localInfo = ""
  '    Dim coordsPrecision As Integer = CV.CoordinatePrecision
  '    retVal = GTA.TrimCoords(retVal, coordsPrecision, localInfo)
  '    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

  '    'Convert shape to wkb
  '    localInfo = ""
  '    wkb = GTA.ConvertGeometryToWkb(CType(newPoly, IGeometry), localInfo)
  '    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

  '  Catch ex As Exception
  '    callInfo &= MethodIdentifier() & " error: " & ex.Message
  '  End Try
  '  Return retVal
  'End Function

  Private Shared Function ImportNewGIS(ByVal projectFolderName As String, ByVal xmlfile As String, ByVal projectId As Long, _
                                       ByVal usrId As Guid, ByRef callInfo As String) As String
    Dim localInfo As String = ""

    Try
      Dim rowsAffected As Integer
      Dim insertFields As String = ""
      Dim insertValues As String = ""

      'Fields handling - simplify shapes for now
      rowsAffected = SimplifyLandMgmtUnitShapes(projectId, localInfo)
      'If localInfo.Contains("error") Then callInfo &= localInfo & Environment.NewLine & "(" & rowsAffected & " rows affected)"
      If rowsAffected = 0 Then callInfo &= "No LMU shapes updated." & Environment.NewLine

      localInfo = ""
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return localInfo
  End Function

  Public Shared Function ProcessUploadedFile(ByVal projectId As Long, ByVal projectFolderName As String, _
                                             ByVal uploadedFile As String, ByVal usrId As Guid) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Dim xmlFileName As String = ""
    Try
      Dim datumCount As Integer = mdl.projectdatumhelper.DeleteAllProjectDatumRecordsByProjectId(projectId, localInfo)
      If localInfo.Contains("error") Then retVal &= Environment.NewLine & localInfo & Environment.NewLine & "(datumCount: " & datumCount & ")" : Exit Try

      CF.DeleteProjectFiles(projectFolderName, localInfo)
      If localInfo.Contains("error") Then retVal &= Environment.NewLine & localInfo : Exit Try

      CF.CreateBaseProjectFolders(projectFolderName, localInfo)
      If localInfo.Contains("error") Then retVal &= Environment.NewLine & localInfo : Exit Try

      If uploadedFile.EndsWith(".zip") Then
        If Not CF.Unzip(uploadedFile, projectFolderName, localInfo) Then
          retVal &= Environment.NewLine & localInfo : Exit Try
        End If
      End If

      xmlFileName = CF.MoveUploadFilesIntoProjectFolders(projectFolderName, uploadedFile, localInfo)
      If localInfo.Contains("error") Then retVal &= Environment.NewLine & localInfo : Exit Try

      retVal &= Environment.NewLine & String.Format("xmlfile: {0}", xmlFileName)
      If xmlFileName <> "" Then

        'GIS shape handling
        Dim gisXmlFileName As String = Path.Combine(projectFolderName, CV.ProjectSupportFolder)
        gisXmlFileName = Path.Combine(gisXmlFileName, xmlFileName.Replace(".mmp.", ".gis."))
        Dim gisFileExists As Boolean = File.Exists(gisXmlFileName)
        retVal &= Environment.NewLine & gisXmlFileName & Environment.NewLine & gisFileExists.ToString & Environment.NewLine
        If gisFileExists Then
          ImportNewGIS(projectFolderName, gisXmlFileName, projectId, usrId, localInfo)
          If localInfo.Contains("error") Then retVal &= Environment.NewLine & localInfo
        End If
      End If

    Catch ex As Exception
      retVal &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

#Region "ProjectDatum-related Delete, Create or Update"

  Public Shared Function DeleteRowsThatMatchDatumId(ByVal datumID As Integer) As String
    Dim retVal As String = ""
    Try
      Dim tablist() As String = CV.DeleteProjectDatumTables
      Dim recsAffected As Integer = 0

      For w As Integer = 0 To UBound(tablist)
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = "Delete from " & dataSchema & "." & tablist(w) & " where ObjectID = " & datumID & ""
            If conn.State = ConnectionState.Closed Then conn.Open()
            recsAffected += cmd.ExecuteNonQuery()
          End Using
        End Using
      Next
    Catch ex As Exception
      retVal &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  Public Shared Function DeleteProjectDatumRecords(ByVal projectId As Long) As String
    Dim retVal As String = ""
    Try
      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = "Delete from " & dataSchema & ".ProjectDatum where ProjectId = " & projectId & ""
          If conn.State = ConnectionState.Closed Then conn.Open()
          cmd.ExecuteNonQuery()
        End Using
      End Using
    Catch ex As Exception
      retVal &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

#End Region

#Region "Record Creation from XML (GIS)"

  Public Shared Function SimplifyLandMgmtUnitShapes(ByVal projectId As Long, ByRef callInfo As String) As Integer
    'For now (8/5/11) want to pull out all LMU shapes and get the largest outer shell for each shape, then rewrite the wkb. Then pull them out again and clip them, again rewrite wkb.

    Dim localInfo As String = ""
    Dim retVal As Integer
    Try
      Dim lmuOids(0) As Integer
      Dim lmuShapes(0) As String
      Dim lmuAreas(0) As Double
      Dim indx As Integer

      Dim cmdText As String = "SELECT LMU.* FROM " & dataSchema & ".LandManagementUnit AS LMU INNER JOIN " & dataSchema & ".ProjectDatum AS PD " & _
              " ON LMU.ObjectId=PD.ObjectId WHERE PD.ProjectId=" & projectId
      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = cmdText

          If conn.State = ConnectionState.Closed Then conn.Open()
          Using readr As SqlDataReader = cmd.ExecuteReader
            While readr.Read
              indx = UBound(lmuOids)
              If lmuOids(indx) > 1 Then
                indx += 1
                ReDim Preserve lmuOids(indx)
                ReDim Preserve lmuShapes(indx)
                ReDim Preserve lmuAreas(indx)
              End If
              lmuOids(indx) = CInt(readr("ObjectId"))
              lmuShapes(indx) = readr("Shape").ToString
            End While
          End Using

          For indx = 0 To UBound(lmuShapes)
            lmuShapes(indx) = GetShellWkb(lmuShapes(indx), localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
            If lmuShapes(indx) <> "" Then lmuAreas(indx) = GTA.GetAreaForWkb(lmuShapes(indx), localInfo) / sqMtrsPerAcre
          Next

          'Put the new shapes back in db
          'Dim cmdText As String = "" 'full command text
          Dim cmdTextWhenShape As String = "" 'command text for conditionals part of update
          Dim cmdTextWhenArea As String = "" 'command text for conditionals part of update
          Dim cmdTextIn As String = "" 'list of objectids to update
          For indx = 0 To UBound(lmuShapes)
            cmdTextWhenShape &= " WHEN " & lmuOids(indx) & " THEN '" & lmuShapes(indx) & "' "
            cmdTextWhenArea &= " WHEN " & lmuOids(indx) & " THEN '" & lmuAreas(indx) & "' "
            cmdTextIn &= lmuOids(indx) & "," 'trim last comma below
          Next

          cmdText = "UPDATE " & dataSchema & ".LandManagementUnit "
          cmdText &= "   SET Shape = CASE ObjectId "
          cmdText &= cmdTextWhenShape
          cmdText &= "   END "
          cmdText &= "   , TotalArea = CASE ObjectId "
          cmdText &= cmdTextWhenArea
          cmdText &= "   END "
          cmdText &= "WHERE ObjectId IN (" & cmdTextIn.TrimEnd(","c) & ")"
          cmd.CommandText = cmdText

          'callInfo &= "     cmd: " & cmdText.Replace("WHEN", "\nWHEN") & "      "
          Dim affectedRecCnt As Integer = cmd.ExecuteNonQuery
          callInfo &= "    updated: " & affectedRecCnt & " |||  "
        End Using
      End Using

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    Finally
    End Try
    Return retVal
  End Function

  Private Shared Function GetShellWkb(origWkb As String, ByRef callInfo As String) As String
    'Get wkb for largest shell of original wkb 
    Dim retVal As String = ""
    Try
      retVal = GTA.GetShellWkb(origWkb, callInfo)

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

#End Region

End Class