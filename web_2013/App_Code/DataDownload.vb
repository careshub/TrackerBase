'Option Strict On 

#Region "Imports"
Imports Microsoft.VisualBasic
Imports System.Data
Imports System.Data.SqlClient
Imports System.IO
Imports System.Net
Imports CF = CommonFunctions
Imports CV = CommonVariables
Imports GT = GISTools
Imports GTA = GIS.GISToolsAddl
Imports TerLoc.Model
Imports GeoAPI.Geometries
Imports NetTopologySuite
Imports NetTopologySuite.Features
Imports NetTopologySuite.Geometries
Imports NetTopologySuite.IO
#End Region

Public Class DataDownload

#Region "Module declarations"
  Private Shared isOzzy As Boolean = False
  Private Shared dataSchema As String = CF.GetDataSchemaName
  Private Shared dataConn As String = CF.GetBaseDatabaseConnString

  'parsing vars
  Private Shared tmpSingle As Single
  Private Shared tmpLong As Int64
  Private Shared tmpShort As Int16
  Private Shared tmpInt As Int32
  Private Shared tmpBool As Boolean
#End Region

#Region "Data dump"

  ''' <summary>
  ''' GIS XML for download.
  ''' </summary>
  ''' <param name="fileName">Name of project.</param>
  ''' <returns>Full path of saved file.</returns>
  Public Shared Function MakeGisXml(ByVal projectId As Long, ByVal fileName As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim newXml As New LandXML.LandXML(projectId, fileName)
      newXml.MakeXml()

      Dim xmlDebug As String = newXml.GetDebug
      If Not String.IsNullOrWhiteSpace(xmlDebug) Then CF.SendOzzy("LandXML debug " & fileName, xmlDebug, Nothing)
      Dim xmlError As String = newXml.GetError
      If Not String.IsNullOrWhiteSpace(xmlError) Then CF.SendOzzy("LandXML error " & fileName, xmlError, Nothing)

    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' GIS XML for download.
  ''' </summary>
  ''' <param name="fileName">Name of project.</param>
  ''' <returns>Full path of saved file.</returns>
  Public Shared Function MakeGisXml_ORIG_MovedTo_LandXML(ByVal projectId As Long, ByVal fileName As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      'Dim fieldId As ArrayList = CommonFunctions.GetAllObjectIdsByProjectIdAndTableName(projectId, "LandManagementUnit", localInfo)
      Dim fieldHelp As New FieldHelper
      Dim allFields As FieldPackageList = fieldHelp.GetFields(projectId, localInfo)
      If localInfo.Contains("error") Then callInfo &= localInfo

      Dim field As FieldRecord = allFields.fields(0).fieldRecord
      Dim boundary As New XElement("TerraceArea" _
        , New XAttribute("Acres", field.TotalArea) _
        , New XAttribute("ObjectID", field.ObjectID) _
        , New XElement("PntList2D", field.Coords) _
        )
      'Dim boundary As New XElement("TerraceArea", _
      '  allFields.fields.[Select](Function(field) _
      '    New XElement("Order" _
      '      , New XAttribute("Acres", field.fieldRecord.TotalArea) _
      '      , New XAttribute("ObjectID", field.fieldRecord.ObjectID) _
      '      , New XElement("PntList2D", field.fieldRecord.Coords) _
      '      ) _
      '    ) _
      '  )

      localInfo = ""
      Dim theProject As Project = ProjectHelper.Fetch(projectId, localInfo)
      If localInfo.Contains("error") Then callInfo &= localInfo
      Dim project As New XElement("Project" _
        , New XAttribute("name", theProject.Name) _
        , New XAttribute("desc", "") _
        )

      '<LandXML xsi:schemaLocation="http://www.landxml.org/schema/LandXML-1.0  http://www.landxml.org/schema/LandXML-1.0/LandXML-1.0.xsd" 
      'version="1.0" date="2007-08-22" time="15:59:24">
      Dim landXml As New XElement("LandXML" _
        , New XAttribute("schemaLocation", "http://www.landxml.org/schema/LandXML-1.0  http://www.landxml.org/schema/LandXML-1.0/LandXML-1.0.xsd") _
        , New XAttribute("version", "1.0") _
        , New XAttribute("date", Now.ToString("yyyy-MM-dd")) _
        , New XAttribute("time", Now.ToString("hh:mm:ss")) _
        , New XElement(project) _
        , New XElement(boundary) _
        )

      landXml.Save(fileName)

    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

#End Region

#Region "Clipper"

  Public Shared Function GetClipperParam(ByRef bbox As String, ByVal fileName As String, ByRef callInfo As String) As String
    'var bbox = minLon + "," + minLat + " " + maxLon + "," + maxLat;
    'param = "w=" + w + "&h=" + h + "&lat=" + midLat + "&lon=" + midLon + "&x=" + minXY[0] + "&y=" + maxXY[1] + "&z=" + midXY[2] + "&bbox=" + bbox;
    'param += "&xmax=" + maxXY[0] + "&ymin=" + minXY[1];
    'param += "&fname=" + nfname;
    'parent.document.location = "datadownload_CB.asp?" + param;
    'plus callfrom = tracker
    '"bbox":"-92.411757,38.983348 -92.393739,38.993439"

    Dim retVal As String = ""
    Dim localInfo As String = ""
    Dim minLon As Double, minLat As Double, midLon As Double, midLat As Double, maxLon As Double, maxLat As Double
    Dim w As Double, h As Double
    Try
      Dim bbox2 As String = bbox.Replace(CV.PointSplitter, CV.CoordinateSplitter)
      Dim coords() As String = bbox2.Split(CV.CoordinateSplitter.ToCharArray, StringSplitOptions.RemoveEmptyEntries)
      'callInfo &= Environment.NewLine & "bbox: " & bbox
      'callInfo &= Environment.NewLine & "bbox2: " & bbox2
      'callInfo &= Environment.NewLine & "coords(0): " & coords(0)
      'callInfo &= Environment.NewLine & "coords(1): " & coords(1)
      'callInfo &= Environment.NewLine & "coords(2): " & coords(2)
      'callInfo &= Environment.NewLine & "coords(3): " & coords(3)
      If Not Double.TryParse(coords(0), minLon) OrElse Not Double.TryParse(coords(1), minLat) _
        OrElse Not Double.TryParse(coords(2), maxLon) OrElse Not Double.TryParse(coords(3), maxLat) Then
        Throw New ArgumentOutOfRangeException("bbox2", bbox2, "Coordinates are not valid.")
      End If
      'callInfo &= Environment.NewLine & "maxLat: " & maxLat & " / NEWMAXLAT "
      'callInfo &= Environment.NewLine & "minLat: " & minLat & " / NEWMINLAT "
      'callInfo &= Environment.NewLine & "maxLon: " & maxLon & " / NEWMAXLON "
      'callInfo &= Environment.NewLine & "minLon: " & minLon & " / NEWMINLON "

      'Add 10% buffer to the area
      Dim latBuf As Double = Math.Abs(maxLat - minLat) * 0.1
      Dim lonBuf As Double = Math.Abs(maxLon - minLon) * 0.1
      'callInfo &= Environment.NewLine & "latBuf: " & latBuf
      'callInfo &= Environment.NewLine & "lonBuf: " & lonBuf
      minLat = minLat - latBuf
      minLon = minLon - lonBuf
      maxLat = maxLat + latBuf
      maxLon = maxLon + lonBuf
      callInfo.Replace("NEWMAXLAT", maxLat.ToString())
      callInfo.Replace("NEWMINLAT", minLat.ToString())
      callInfo.Replace("NEWMAXLON", maxLon.ToString())
      callInfo.Replace("NEWMINLON", minLon.ToString())
      bbox = minLon & "," & minLat & " " & maxLon & "," & maxLat 'reset to larger box

      midLat = (minLat + maxLat) / 2 '38.988394
      midLon = (minLon + maxLon) / 2 '-92.402748
      'callInfo &= Environment.NewLine & "midLat: " & midLat
      'callInfo &= Environment.NewLine & "midLon: " & midLon

      'var midXY = GeoToUTM_Main(midLat, midLon);
      Dim midXY() As Double = GTA.GeoToUTM_Main(midLat, midLon)
      'var minXY = new Array(2);
      'var maxXY = new Array(2);
      Dim minXY() As Double = {0, 0}
      Dim maxXY() As Double = {0, 0}
      'LatLonToUTMXY(DegToRad (minLat), DegToRad (minLon), midXY[2], minXY);     calls <script language=javascript src='http://ims.missouri.edu/website/maproom/_carescommon/jscripts/coordConverter.js'></script>
      GTA.LatLonToUTMXY(GTA.DegToRad(minLat), GTA.DegToRad(minLon), midXY(2), minXY)
      'LatLonToUTMXY(DegToRad (maxLat), DegToRad (maxLon), midXY[2], maxXY);
      GTA.LatLonToUTMXY(GTA.DegToRad(maxLat), GTA.DegToRad(maxLon), midXY(2), maxXY)

      w = Math.Round(maxXY(0) - minXY(0))
      h = Math.Round(maxXY(1) - minXY(1))
      'callInfo &= Environment.NewLine & "h: " & h
      'callInfo &= Environment.NewLine & "w: " & w
      'callInfo &= Environment.NewLine & "x: " & minXY(0)
      'callInfo &= Environment.NewLine & "y: " & maxXY(1)
      'callInfo &= Environment.NewLine & "z: " & midXY(2)
      'callInfo &= Environment.NewLine & "xmax: " & maxXY(0)
      'callInfo &= Environment.NewLine & "ymin: " & minXY(1)

      Dim area As Double = w * h * 0.000247105381
      If (area > 10506.25) Then
        Throw New ArgumentOutOfRangeException("Area", "Your area is larger than 10,000 acres (" & area.ToString("f2") & "). Please define a smaller area.")
      End If

      retVal = "w=" & w & "&h=" & h & "&lat=" & midLat & "&lon=" & midLon & _
        "&x=" & minXY(0) & "&y=" & maxXY(1) & "&z=" & midXY(2) & _
        "&bbox=" & bbox & _
        "&xmax=" & maxXY(0) & "&ymin=" & minXY(1) & _
        "&fname=" & fileName & "&callfrom=" & "tracker"

    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Get clipper files for download.
  ''' </summary>
  ''' <param name="bbox">for Clipper: bbox = minLon + "," + minLat + " " + maxLon + "," + maxLat</param>
  Public Shared Function GetClipper(ByVal projectId As Long, ByVal fileName As String, ByVal bbox As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim param As String = GetClipperParam(bbox, fileName, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      If String.IsNullOrWhiteSpace(param) Then Return retVal

      Dim requestURL As String = "http://clipper.missouri.edu/nrcsdata/datadownload_CB.asp" & "?" & param
      Dim webReq As HttpWebRequest = WebRequest.Create(requestURL)
      webReq.Method = "GET"
      webReq.Timeout = 10 * 60 * 1000 '10 min * 60 sec * 1000 millisec
      Dim webResp As HttpWebResponse = webReq.GetResponse
      Dim answer As Stream = webResp.GetResponseStream

      Dim _answer As StreamReader = New StreamReader(answer)
      Dim zipAndFlag As String = ""
      Dim line As String
      Do
        line = _answer.ReadLine()
        If line IsNot Nothing AndAlso line.IndexOf("zip=") > -1 Then zipAndFlag = line
        If line IsNot Nothing Then callInfo &= line
      Loop Until line Is Nothing
      _answer.Close()

      If String.IsNullOrWhiteSpace(zipAndFlag) Then Throw New ArgumentException("Request info was not found. Status: " & webResp.StatusCode)
      retVal = zipAndFlag

      'callInfo &= Environment.NewLine & "error: " & requestURL
    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Obsolete. Get clipper files for download. Has all code made while developing function.
  ''' </summary>
  ''' <param name="bbox">for Clipper: bbox = minLon + "," + minLat + " " + maxLon + "," + maxLat</param>
  Public Shared Function GetClipper2(ByVal projectId As Long, ByVal fileName As String, ByVal bbox As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim param As String = GetClipperParam(bbox, fileName, localInfo)
      If localInfo.Contains("error") Then callInfo &= Space(2) & localInfo & Space(2)
      'Throw New ArgumentException(param)

      'http://www.codeproject.com/Articles/18034/HttpWebRequest-Response-in-a-Nutshell-Part-1

      Dim requestURL As String = "http://clipper.missouri.edu/nrcsdatadev/datadownload_CB.asp" ' & "?" & param
      requestURL = requestURL & "?" & param

      Dim webReq As HttpWebRequest = WebRequest.Create(requestURL)
      webReq.Method = "GET"
      Dim webResp As HttpWebResponse = webReq.GetResponse
      Dim answer As Stream = webResp.GetResponseStream
      Dim _answer As StreamReader = New StreamReader(answer)
      Dim zipAndFlag As String = ""
      Dim line As String
      Do
        line = _answer.ReadLine()
        If line.IndexOf("zip=") > -1 Then zipAndFlag = line
      Loop Until line Is Nothing
      _answer.Close()

      If String.IsNullOrWhiteSpace(zipAndFlag) Then Throw New ArgumentException("Request info was not found. Status: " & webResp.StatusCode)
      retVal = zipAndFlag

    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Clean file name for clipper call.
  ''' </summary>
  Public Shared Function CleanFileNameForClipper(ByVal fileName As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim curr As String
      Dim validchar As String = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890_"
      For i As Integer = 0 To fileName.Length - 1
        curr = fileName.Chars(i).ToString
        retVal &= If(validchar.IndexOf(curr) > -1, curr, "_")
      Next

    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

#End Region

#Region "Shapefiles"

  ''' <summary>
  ''' Write the projection file for a shapefile
  ''' </summary>
  Public Shared Sub WriteProjectionFile(ByVal fileName As String)
    Try
      Dim proj As String = CV.ProjectionDef
      Using sw As New StreamWriter(fileName & ".prj")
        sw.Write(proj)
      End Using
    Catch ex As Exception

    End Try
  End Sub

  'from: http://dominoc925.blogspot.com/2013/04/using-nettopologysuite-to-read-and.html
  Public Shared Sub ReadShapefiles(ByVal projectId As Long, ByVal fileName As String, ByRef callInfo As String)

    Dim factory As New GeometryFactory()
    Dim shapeFileDataReader As New ShapefileDataReader(fileName, factory)

    'Display the shapefile type
    Dim shpHeader As ShapefileHeader = shapeFileDataReader.ShapeHeader
    Console.WriteLine(String.Format("Shape type: {0}", shpHeader.ShapeType))

    'Display the min and max bounds of the shapefile
    Dim bounds As Envelope = shpHeader.Bounds
    Console.WriteLine(String.Format("Min bounds: ({0},{1})", bounds.MinX, bounds.MinY))
    Console.WriteLine(String.Format("Max bounds: ({0},{1})", bounds.MaxX, bounds.MaxY))

    'Display summary information about the Dbase file
    Dim header As DbaseFileHeader = shapeFileDataReader.DbaseHeader
    Console.WriteLine("Dbase info")
    Console.WriteLine(String.Format("{0} Columns, {1} Records", header.Fields.Length, header.NumRecords))
    For i As Integer = 0 To header.NumFields - 1
      Dim fldDescriptor As DbaseFieldDescriptor = header.Fields(i)
      Console.WriteLine(String.Format("   {0} {1}", fldDescriptor.Name, fldDescriptor.DbaseType))
    Next

    'Reset the pointer to the start of the shapefile, just in case
    shapeFileDataReader.Reset()

    'Read through all records of the shapefile (geometry and attributes) into a feature collection 
    Dim features As New ArrayList()
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

    'Close and free up any resources
    shapeFileDataReader.Close()
    shapeFileDataReader.Dispose()

  End Sub

  ''' <summary>
  ''' GIS Shapefiles for download.
  ''' </summary>
  ''' <param name="folderName">shapefile container</param>
  Public Shared Function CreateGisShapefiles(ByVal projectId As Long, ByVal projectName As String, _
                      ByVal folderName As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim di As New DirectoryInfo(folderName)
      If Not di.Exists Then di.Create()

      Dim fileNameBase As String = String.Format("{0}{1}", folderName, projectName)
      Dim zipFileName As String = String.Format("{0}_{1}.zip", fileNameBase, "GIS") 'e.g. Farm17_GIS.zip
      Dim fileNamesForZipping As List(Of String) = New List(Of String)

      'Make Field shapefiles
      Dim currGis As String = "_Field"
      Dim currFileName As String = fileNameBase & currGis
      Dim shpFields(,) As Object = CV.TerraceAreaShpFields
      Dim shpLookupTable As String(,) = CV.LMUToSHP
      Dim cmdText As String = String.Format("Select FEATS.*, PD.Notes from {0}.LandManagementUnit as FEATS " & _
          "inner join {0}.ProjectDatum as PD on PD.objectid = FEATS.objectid " & _
          "where PD.projectid = " & projectId, dataSchema)
      localInfo = ""
      CreateShapefile(projectId, currFileName, cmdText, shpFields, shpLookupTable, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & "Terrace Area: " & localInfo
      For Each fi As FileInfo In di.GetFiles(projectName & currGis & ".*")
        fileNamesForZipping.Add(fi.FullName)
      Next

      'Make Contour shapefiles
      currGis = "_Contour"
      currFileName = fileNameBase & currGis
      shpFields = CV.ContourShpFields
      shpLookupTable = CV.ContourToSHP
      cmdText = String.Format("Select FEATS.*, PD.Notes from {0}.Contour as FEATS " & _
          "inner join {0}.ProjectDatum as PD on PD.objectid = FEATS.objectid " & _
          "where PD.projectid = " & projectId, dataSchema)
      localInfo = ""
      CreateShapefile(projectId, currFileName, cmdText, shpFields, shpLookupTable, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & "Contour: " & localInfo
      For Each fi As FileInfo In di.GetFiles(projectName & currGis & ".*")
        fileNamesForZipping.Add(fi.FullName)
      Next

      'Make Divide shapefiles
      currGis = "_Divide"
      currFileName = fileNameBase & currGis
      shpFields = CV.DivideShpFields
      shpLookupTable = CV.DivideToSHP
      cmdText = String.Format("Select FEATS.*, PD.Notes from {0}.Divide as FEATS " & _
          "inner join {0}.ProjectDatum as PD on PD.objectid = FEATS.objectid " & _
          "where PD.projectid = " & projectId, dataSchema)
      localInfo = ""
      CreateShapefile(projectId, currFileName, cmdText, shpFields, shpLookupTable, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & "Divide: " & localInfo
      For Each fi As FileInfo In di.GetFiles(projectName & currGis & ".*")
        fileNamesForZipping.Add(fi.FullName)
      Next

      'Make HighPoint shapefiles. 
      'Need to add Shape field to db for HighPoint. Until then, use extra parameter in call.
      currGis = "_HighPoint"
      currFileName = fileNameBase & currGis
      shpFields = CV.HighPointShpFields
      shpLookupTable = CV.HighPointToSHP
      cmdText = String.Format("Select FEATS.*, PD.Notes from {0}.HighPoint as FEATS " & _
          "inner join {0}.ProjectDatum as PD on PD.objectid = FEATS.objectid " & _
          "where PD.projectid = " & projectId, dataSchema)
      localInfo = ""
      CreateShapefile(projectId, currFileName, cmdText, shpFields, shpLookupTable, localInfo, "HighPoint")
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & "HighPoint: " & localInfo
      For Each fi As FileInfo In di.GetFiles(projectName & currGis & ".*")
        fileNamesForZipping.Add(fi.FullName)
      Next

      'Make Ridgeline shapefiles
      currGis = "_Ridgeline"
      currFileName = fileNameBase & currGis
      shpFields = CV.RidgelineShpFields
      shpLookupTable = CV.RidgelineToSHP
      cmdText = String.Format("Select FEATS.*, PD.Notes from {0}.Ridgeline as FEATS " & _
          "inner join {0}.ProjectDatum as PD on PD.objectid = FEATS.objectid " & _
          "where PD.projectid = " & projectId, dataSchema)
      localInfo = ""
      CreateShapefile(projectId, currFileName, cmdText, shpFields, shpLookupTable, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & "Ridgeline: " & localInfo
      For Each fi As FileInfo In di.GetFiles(projectName & currGis & ".*")
        fileNamesForZipping.Add(fi.FullName)
      Next

      'Make Waterway shapefiles
      currGis = "_Waterway"
      currFileName = fileNameBase & currGis
      shpFields = CV.WaterwayShpFields
      shpLookupTable = CV.WaterwayToSHP
      cmdText = String.Format("Select FEATS.*, PD.Notes from {0}.Waterway as FEATS " & _
          "inner join {0}.ProjectDatum as PD on PD.objectid = FEATS.objectid " & _
          "where PD.projectid = " & projectId, dataSchema)
      localInfo = ""
      CreateShapefile(projectId, currFileName, cmdText, shpFields, shpLookupTable, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & "Waterway: " & localInfo
      For Each fi As FileInfo In di.GetFiles(projectName & currGis & ".*")
        fileNamesForZipping.Add(fi.FullName)
      Next

      'Make Terrace shapefiles
      currGis = "_Terrace"
      currFileName = fileNameBase & currGis
      shpFields = CV.TerraceShpFields
      shpLookupTable = CV.TerraceToSHP
      cmdText = String.Format("Select FEATS.*, PD.Notes from {0}.Terrace as FEATS " & _
          "inner join {0}.ProjectDatum as PD on PD.objectid = FEATS.objectid " & _
          "where PD.projectid = " & projectId, dataSchema)
      localInfo = ""
      CreateShapefile(projectId, currFileName, cmdText, shpFields, shpLookupTable, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & "Terrace: " & localInfo
      For Each fi As FileInfo In di.GetFiles(projectName & currGis & ".*")
        fileNamesForZipping.Add(fi.FullName)
      Next



      'Zip it all up
      localInfo = ""
      CF.Zip(fileNamesForZipping, zipFileName, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

  Public Shared Function CreateShapefile(ByVal projectId As Long, ByVal fileName As String, _
                              ByVal cmdText As String, ByVal shpFields(,) As Object, _
                              ByVal shpLookupTable As String(,), ByRef callInfo As String, _
                              Optional featureType As String = "") As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim dbfeats As DataTable
      dbfeats = CF.GetDataTable(dataConn, cmdText, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      Dim shpCols As List(Of DbaseFieldDescriptor) = CreateShapefileColumns(shpFields, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Dim featOid As Long
      Dim feats As New List(Of Feature)
      Dim feat As Feature
      Dim featAttrs As AttributesTable
      Dim featGeom As IGeometry
      Dim attrType As Char
      Dim attrName As String
      Dim attr As Object 
      For Each dbfeat As DataRow In dbfeats.Rows
        featOid = CF.NullSafeLong(dbfeat.Item("ObjectID"), -1)
        '      callInfo &= Environment.NewLine & "featOid: " & featOid
        Dim shape As String = ""
        If featureType = "HighPoint" Then
          Dim hpHelper As New TerLoc.Model.HighPointHelper
          localInfo = ""
          shape = hpHelper.MakeWkb(dbfeat, localInfo)
          If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Else
          Try : shape = CF.NullSafeString(dbfeat.Item("Shape"), "") : Catch : End Try
        End If
        localInfo = ""
        featGeom = GTA.ConvertWkbToGeometry(shape, localInfo)
        If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        'if (type == typeof(Char)) Return 'C';
        'if (type == typeof(string)) Return 'C';
        'if (type == typeof(Double)) Return 'N';
        'if (type == typeof(Single)) Return 'N';
        'if (type == typeof(Int16)) Return 'N';
        'if (type == typeof(Int32)) Return 'N';
        'if (type == typeof(Int64)) Return 'N';
        'if (type == typeof(UInt16)) Return 'N';
        'if (type == typeof(UInt32)) Return 'N';
        'if (type == typeof(UInt64)) Return 'N';
        'if (type == typeof(Decimal)) Return 'N';
        'if (type == typeof(Boolean)) Return 'L';
        'if (type == typeof(DateTime)) Return 'D';
        If featGeom IsNot Nothing Then
          featAttrs = New AttributesTable
          For Each col As DataColumn In dbfeat.Table.Columns
            attrName = LookupShpForSqlField(shpLookupTable, col.ColumnName)
            'callInfo &= Environment.NewLine & col.ColumnName & ": " & attrName & ": "
            If Not String.IsNullOrWhiteSpace(attrName) Then
              attrType = (From dfd In shpCols Where dfd.Name = attrName Select dfd.DbaseType)(0) '.First did not work
              'callInfo &= attrType & ": "
              attr = dbfeat.Item(col)
              If col.DataType = GetType(System.Boolean) Then 
                attr = If(attr = True, 1, 0)
              End If
              If attrType = "N" Then attr = attr.ToString 
              If attr Is Nothing OrElse IsDBNull(attr) Then
                featAttrs.AddAttribute(attrName, Nothing)
                'callInfo &= "  Null" & ":" & attrName
              Else
                featAttrs.AddAttribute(attrName, attr)
                'callInfo &= If(attr.ToString.Length > 20, attr.ToString.Substring(0, 20), attr.ToString)
              End If
            End If
          Next

          feat = New Feature(featGeom, featAttrs)
          feats.Add(feat)
        End If 
      Next

      'callInfo &= Environment.NewLine & "feats.Count: " & feats.Count
      If feats.Count = 0 Then Return retVal
      Try
        Dim writer As New ShapefileDataWriter(fileName, New GeometryFactory) With { _
           .Header = ShapefileDataWriter.GetHeader(shpCols.ToArray, feats.Count) _
        }
        'callInfo &= Environment.NewLine & "feats.Count 1: " & feats.Count
        Dim featList As IList = DirectCast(feats, System.Collections.IList)
        'callInfo &= Environment.NewLine & "feats.Count 2: " & feats.Count
        writer.Write(featList)
      Catch ex As Exception
        callInfo &= Environment.NewLine & "2: " & ShowError(MethodIdentifier(), ex)
      End Try

      'make projection file
      WriteProjectionFile(fileName)

    Catch ex As Exception
      callInfo &= Environment.NewLine & ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

  Public Shared Function CreateShapefileColumns(ByVal shpFlds(,) As Object, ByRef callInfo As String) As List(Of DbaseFieldDescriptor)
    Dim retVal As New List(Of DbaseFieldDescriptor)
    Try
      Dim desc As New DbaseFieldDescriptor
      For i As Integer = 0 To UBound(shpFlds, 1)
        desc = New DbaseFieldDescriptor With {.Name = shpFlds(i, 0).ToString, .DbaseType = GetDbaseTypeForRuntimeType(shpFlds(i, 1), Nothing)}
        If shpFlds(i, 2) > 0 Then desc.Length = shpFlds(i, 2)
        retVal.Add(desc)
        'callInfo &= shpFlds(i, 0).ToString & " ... "
      Next

    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Get the char for DbaseFieldDescriptor
  ''' </summary>
  Public Shared Function GetDbaseTypeForRuntimeType(ByVal _type As Type, ByRef callInfo As String) As Char
    Dim retVal As Char
    Try
      Select Case _type
        Case GetType(Boolean)
          retVal = "L"c ' logical data type, one character (T,t,F,f,Y,y,N,n)
          Exit Select
        Case GetType(String)
          retVal = "C"c ' char or string
          Exit Select
        Case GetType(DateTime)
          retVal = "D"c ' date
          Exit Select
        Case GetType(Short), GetType(Integer), GetType(Long), GetType(Double)
          retVal = "N"c ' numeric
          Exit Select
        Case GetType(Single)
          retVal = "F"c ' double
          Exit Select
        Case GetType(Byte())
          retVal = "B"c ' BLOB - not a dbase but this will hold the WKB for a geometry object.
          Exit Select
        Case Else
          Throw New NotSupportedException("Do not know how to parse Field type " & _type.ToString)
      End Select

    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

#Region "Shapefile Lookups"

  Public Shared Function LookupShpForSqlField(ByVal lookupTable As String(,), ByVal lookup As String) As String
    Dim retVal As String = Nothing
    Try
      retVal = MapProjectAttrToShpAttr(lookupTable, lookup, True)
    Catch ex As Exception
      retVal = ex.Message
    End Try
    Return retVal
  End Function

  Public Shared Function MapProjectAttrToShpAttr(ByVal lookupTable As String(,), ByVal lookupValue As String, ByVal getShp As Boolean) As String
    Dim retVal As String = Nothing
    Try
      Dim findCol As Integer = If(getShp, 0, 1)
      Dim getCol As Integer = If(getShp, 1, 0)
      For iRow As Integer = 0 To UBound(lookupTable, 1)
        If lookupTable(iRow, findCol) = lookupValue Then Return lookupTable(iRow, getCol)
      Next
    Catch ex As Exception
      retVal = ex.Message
    End Try
    Return retVal
  End Function

#End Region

#End Region

  Private Shared Function ShowError(ByVal callingMethod As String, ByVal inEx As Exception) As String
    Try
      Return (callingMethod + " error: " & FormatErrorMessage(inEx))
    Catch ex As Exception
      Return "ShowError didn't work"
    End Try
  End Function

  Private Shared Function FormatErrorMessage(ByVal inEx As Exception) As String
    'Used for error message attributes
    Try
      Dim CurrentStack As New System.Diagnostics.StackTrace(inEx, True)
      Dim fln As Integer = CurrentStack.GetFrame(CurrentStack.GetFrames().Length - 1).GetFileLineNumber()
      Dim lnNum As String = If(fln <> 0, " (line " & fln.ToString() & ") ", "")
      ' (string)(fln != 0 ? " (line " + fln.ToString() + ")" : "");
      Return lnNum & Convert.ToString(inEx.Message)
    Catch ex As Exception
      Return "FormatErrorMessage didn't work (" + ex.Message + ")"
    End Try
  End Function

  Private Shared Function MethodIdentifier() As String
    'Used for error message attributes (title)
    Try
      Return CF.FormatMethodIdentifier(System.Reflection.MethodBase.GetCurrentMethod.DeclaringType.Name, New System.Diagnostics.StackFrame(1).GetMethod().Name)
    Catch ex As Exception
      Return "SiteAddl MethodIdentifier didn't work"
    End Try
  End Function

End Class
