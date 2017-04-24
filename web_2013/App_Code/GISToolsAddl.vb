Option Explicit On
Option Strict On

Imports GGeom = GeoAPI.Geometries
Imports Microsoft.VisualBasic
Imports NTS = NetTopologySuite
Imports NGeom = NetTopologySuite.Geometries
Imports NIo = NetTopologySuite.IO
Imports NOp = NetTopologySuite.Operation
Imports NSimp = NetTopologySuite.Simplify
Imports SNAP = NetTopologySuite.Operation.Overlay.Snap

Imports CF = CommonFunctions
Imports CV = CommonVariables
Imports System
Imports System.Data.SqlClient
Imports System.Text
Imports System.Data
Imports System.Diagnostics

Namespace GIS

  Public Class GISToolsAddl
    Private Const okayMsg As String = "Okay"
    Private Shared splitter As String = " // "
    Private Shared ReadOnly coordsPrecision As Integer = CV.CoordinatePrecision
    Private Shared ReadOnly xySplitter As String = CV.CoordinateSplitter
    Private Shared ReadOnly ptSplitter As String = CV.PointSplitter
    Private Shared ReadOnly geomPartSplitter As String = CV.GeometryPartSplitter   'use for e.g. holes to divide sets of coords
    Private Shared ReadOnly geomSplitter As String = CV.GeometrySplitter   'use for e.g. multiple polygons to divide sets of coords
     
    Private Shared ReadOnly dataSchema As String = CF.GetDataSchemaName
    Private Shared ReadOnly dataConn As String = CF.GetBaseDatabaseConnString
    Private Shared ReadOnly mtrsPerFoot As Double = CV.FeetToMetersMultiplier
    Private Shared ReadOnly sqMtrsPerAcre As Double = CV.AcresToSquareMetersMultiplier
    Private Shared ReadOnly lmuSizePrecision As Integer = CV.LandManagementUnitSizePrecision

#Region "WKB - Well-Known Binary"

    'SOURCES
    'http://dev.mysql.com/doc/refman/5.0/en/gis-wkb-format.html
    'http://ariasprado.name/2010/11/25/getting-the-well-known-binary-representation-of-geometries-using-the-jts-topology-suite.html

    ''' <summary>
    ''' Create the WKB string for a geometry
    ''' </summary>
    Public Shared Function ConvertGeometryToWkb(ByVal inputShape As GGeom.IGeometry, ByRef callInfo As String) As String
      Dim retVal As String = ""
      Try
        If inputShape Is Nothing Then Return ""
        Dim wkbw As New NIo.WKBWriter
        retVal = System.Convert.ToBase64String(wkbw.Write(inputShape))
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create a geometry from its WKB string
    ''' </summary>
    Public Shared Function ConvertWkbToGeometry(ByVal inputWkb As String, ByRef callInfo As String) As GGeom.IGeometry
      Dim retVal As GGeom.IGeometry = Nothing
      If String.IsNullOrWhiteSpace(inputWkb) Then Return retVal
      Try
        Dim wkbr As New NIo.WKBReader
        retVal = wkbr.Read(System.Convert.FromBase64String(inputWkb))
      Catch ex As Exception
        callInfo &= MethodIdentifier() & " error: " & ex.ToString
      End Try
      Return retVal
    End Function

    'Used as test
    Public Shared Function WkbPolygonToShape(ByVal WkbStr As String, ByVal DestProjection As String, _
             ByRef coords As String, ByRef DestShape As GGeom.IGeometry, ByRef callInfo As String) As Boolean
      'Convert a polygon or multi-polygon represented as a Well Known Binary 
      ' string to a MapWindow shape. 
      'http://edndoc.esri.com/arcsde/9.1/general_topics/wkb_representation.htm
      'http://www.mapwindow.org/wiki/index.php/MapWinGIS:Shape_Serialize_Format
      'http://www.opengeospatial.org/standards

      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As Boolean = False
      Dim CvtBytes As Byte()

      Try  'Catch any exceptions if invalid WKB string format, etc.
        If WkbStr = "" Then Exit Try
        CvtBytes = System.Convert.FromBase64String(WkbStr)

        Dim wkbr As New NIo.WKBReader
        Dim thisGeom As GeoAPI.Geometries.IGeometry
        Dim thisPoly As GeoAPI.Geometries.IPolygon
        Dim thisMultiPoly As GeoAPI.Geometries.IMultiPolygon
        Try
          thisGeom = wkbr.Read(CvtBytes)
          Select Case thisGeom.GeometryType
            Case GetType(NGeom.Polygon).Name
              thisPoly = CType(thisGeom, GeoAPI.Geometries.IPolygon)
              callInfo &= " Holes: " & thisPoly.Holes.Length
              callInfo &= " Rings: " & thisPoly.InteriorRings.Length & "/" & thisPoly.NumInteriorRings
              'callInfo &= " Shell: " & thisPoly.Shell.CoordinateSequence.ToString
              If thisGeom.NumGeometries > 1 Then Exit Try
            Case GetType(NGeom.MultiPolygon).Name
              thisMultiPoly = CType(thisGeom, GeoAPI.Geometries.IMultiPolygon)
              callInfo &= " error: Shape is of type " & thisGeom.GeometryType
              callInfo &= " Geoms: " & thisMultiPoly.NumGeometries
              'callInfo &= " Length: " & thisMultiPoly.Geometries.
              If thisGeom.NumGeometries > 1 Then Exit Try
              Exit Try
            Case Else
              callInfo &= " error: Shape is of type " & thisGeom.GeometryType
              Exit Try
          End Select
          coords = GetCoordsStringFromGeom(thisGeom, localInfo)
          callInfo &= localInfo
        Catch ex As Exception
          callInfo &= " Set geom error: " & ex.ToString
        End Try


        retVal = True
        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function  'WkbPolygonToShape

#End Region

    ''' <summary>
    ''' Converts WKB of a geometry into the WKB of a single polygon
    ''' </summary>
    ''' <param name="origWkb"></param>
    ''' <param name="callInfo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function GetShellWkb(ByVal origWkb As String, ByRef callInfo As String) As String
      'Get wkb for largest shell of original wkb
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As String = ""
      Try
        Dim origGeom As GGeom.IGeometry = ConvertWkbToGeometry(origWkb, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        Dim newPoly As GGeom.IPolygon = GetShellOfPolygonOrMultiPolygon(origGeom, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        retVal = ConvertGeometryToWkb(CType(newPoly, GGeom.IGeometry), localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function GetShellOfPolygonOrMultiPolygon(ByVal inGeom As GGeom.IGeometry, _
                               ByRef callInfo As String) As GGeom.IPolygon
      callInfo = MethodIdentifier()
      Dim retval As GGeom.IPolygon = Nothing
      Try
        Dim geomFact As NGeom.GeometryFactory = New NGeom.GeometryFactory
        Select Case inGeom.GeometryType
          Case GetType(NGeom.Polygon).Name
            retval = geomFact.CreatePolygon(CType(inGeom, GGeom.IPolygon).Shell, Nothing)
          Case GetType(NGeom.MultiPolygon).Name
            Dim amult As GGeom.IMultiPolygon = CType(inGeom, GGeom.IMultiPolygon)
            For Each geom As GGeom.IGeometry In amult.Geometries
              If geom.GeometryType = GetType(NGeom.Polygon).Name Then
                If retval Is Nothing Then
                  retval = geomFact.CreatePolygon(CType(geom, GGeom.IPolygon).Shell, Nothing)
                Else
                  If retval.Area < geomFact.CreatePolygon(CType(geom, GGeom.IPolygon).Shell, Nothing).Area Then
                    retval = geomFact.CreatePolygon(CType(geom, GGeom.IPolygon).Shell, Nothing)
                  End If
                End If
              End If
            Next

            retval = geomFact.CreatePolygon(CType(CType(inGeom, GGeom.IMultiPolygon).GetGeometryN(0), GGeom.IPolygon).Shell, Nothing)
          Case Else
            callInfo &= " error: Shape is of type " & inGeom.GeometryType
            Exit Try
        End Select

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retval
    End Function

    Public Shared Function GetCoordsForWkb(ByVal featWkb As String, ByRef callInfo As String) As String
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As String = ""
      Try
        Dim thisGeom As GGeom.IGeometry = ConvertWkbToGeometry(featWkb, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo : localInfo = ""

        retVal = GetCoordsStringFromGeom(thisGeom, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo : localInfo = ""
        callInfo &= localInfo
        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

#Region "GIS Methods"
    '======================================================================================================
    '++++        NOTE: polygon coords are lon/lat, i.e. x/y                               ++++++++++++
    '++++        NOTE: string format is "x1,y1 x2,y2 etc."                                ++++++++++++
    '======================================================================================================

    Public Shared Function GetBoundingBox(ByVal geoms As List(Of GGeom.IGeometry), ByRef callInfo As String) As String
      ' <param name="bbox">for Clipper: bbox = minLon + "," + minLat + " " + maxLon + "," + maxLat</param>
      Dim retVal As String = ""
      Dim totalGeom As GGeom.IGeometry = Nothing
      Try
        For Each geom In geoms
          If geom IsNot Nothing Then totalGeom = If(totalGeom Is Nothing, geom, totalGeom.Union(geom))
        Next

        Dim env As GGeom.Envelope = Nothing
        If totalGeom IsNot Nothing Then env = totalGeom.EnvelopeInternal

        If env IsNot Nothing Then retVal = env.MinX & "," & env.MinY & " " & env.MaxX & "," & env.MaxY
      Catch ex As Exception
        callInfo &= MethodIdentifier() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function GetFeaturesEnvelopeBBox(ByVal projectId As Long, ByRef callInfo As String) As String
      ' <param name="bbox">for Clipper: bbox = minLon + "," + minLat + " " + maxLon + "," + maxLat</param>
      Dim retVal As String = ""
      Dim localInfo As String = ""
      Try
        Dim env As GGeom.Envelope = GetAllFeaturesEnvelope(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        If env IsNot Nothing Then retVal = env.MinX & "," & env.MinY & " " & env.MaxX & "," & env.MaxY
      Catch ex As Exception
        callInfo &= MethodIdentifier() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function GetAllFeaturesEnvelope(ByVal projectId As Long, ByRef callInfo As String) As GGeom.Envelope
      Dim retVal As GGeom.Envelope = Nothing
      Dim featsEnv As GGeom.Envelope
      Dim localInfo As String = ""
      Try
        'Get extent of fields
        featsEnv = GetFieldsEnvelope(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        If retVal Is Nothing Then retVal = featsEnv
        If featsEnv IsNot Nothing Then retVal.ExpandToInclude(featsEnv)

      Catch ex As Exception
        callInfo &= MethodIdentifier() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function GetFieldsEnvelope(ByVal projectId As Long, ByRef callInfo As String) As GGeom.Envelope
      Dim retVal As GGeom.Envelope = Nothing
      Dim featGeom As GGeom.IGeometry = Nothing
      Dim localInfo As String = ""
      Try
        featGeom = GetFieldsGeometry(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        If featGeom IsNot Nothing Then retVal = featGeom.EnvelopeInternal
      Catch ex As Exception
        callInfo &= MethodIdentifier() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Get shape for a field
    ''' </summary>
    ''' <param name="projectId"></param>
    ''' <param name="callInfo"></param>
    Public Shared Function GetFieldShape(ByVal projectId As Long, ByVal featureId As Integer, ByRef callInfo As String) As String
      Dim retVal As String = ""
      Dim localInfo As String = ""
      Dim cmdText As String
      Dim featTable As DataTable
      Try
        'Get table of fields wkb
        cmdText = "Select shape  " & _
        " FROM " & dataSchema & ".LandManagementUnit as FEATS  " & _
        " INNER JOIN " & dataSchema & ".ProjectDatum as PD ON FEATS.ObjectID = PD.ObjectID   " & _
        " WHERE PD.ProjectId = @projId AND FEATS.ObjectId = @featId"

        Dim params As New List(Of SqlParameter)
        Dim param As New SqlParameter("@projId", SqlDbType.BigInt)
        param.Value = projectId
        params.Add(param)
        param = New SqlParameter("@featId", SqlDbType.BigInt)
        param.Value = featureId
        params.Add(param)

        featTable = CF.GetDataTable(dataConn, cmdText, params.ToArray, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        retVal = CF.NullSafeString(featTable.Rows(0).Item("shape"), "")

      Catch ex As Exception
        callInfo &= MethodIdentifier() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Get combined geometry of all fields (will be polygon or multipolygon)
    ''' </summary>
    ''' <param name="projectId"></param>
    ''' <param name="callInfo"></param>
    Public Shared Function GetFieldsGeometry(ByVal projectId As Long, ByRef callInfo As String) As GGeom.IGeometry
      Dim retVal As GGeom.IGeometry = Nothing
      Dim featGeom As GGeom.IGeometry = Nothing
      Dim localInfo As String = ""
      Dim cmdText As String
      Dim featTable As DataTable
      Try
        'Get table of fields wkb
        cmdText = "Select shape  " & _
        " FROM " & dataSchema & ".LandManagementUnit as FEATS  " & _
        " INNER JOIN " & dataSchema & ".ProjectDatum as PD ON FEATS.ObjectID = PD.ObjectID   " & _
        " WHERE PD.ProjectId = " & projectId & ""

        localInfo = ""
        featTable = CF.GetDataTable(dataConn, cmdText, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

        'callInfo &= "error: " & featTable.Rows.Count & Space(3) & cmdText 'debug
        'loop
        For Each dr As DataRow In featTable.Rows
          'callInfo &= CF.NullSafeString(dr.Item("shape"), "") & CV.HtmlLineBreak 'debug
          featGeom = ConvertWkbToGeometry(CF.NullSafeString(dr.Item("shape"), ""), Nothing)
          If featGeom IsNot Nothing Then retVal = If(retVal Is Nothing, featGeom, retVal.Union(featGeom))
        Next

      Catch ex As Exception
        callInfo &= MethodIdentifier() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Converts a string of coords in latlng to coords in utm
    ''' </summary>
    ''' <param name="latLonCoords"></param>
    ''' <param name="zone"></param>
    ''' <param name="callInfo"></param>
    Public Shared Function ConvertLatLonCoordsToUtm(ByRef latLonCoords As String, ByRef zone As Integer, _
                                                    ByRef callInfo As String) As String

      callInfo = MethodIdentifier()
      Dim retVal As String = ""
      Dim geomParts As String() 'Stores coord strings for the geometrys within the string (e.g. multipolygon, polygon, multipoint, etc.)
      Dim geomPart As String 'Stores coord string for an individual geometry (polygon, linestring, etc.)
      Dim subGeomParts As String() 'Stores coord string for all subdivisions of a geometry (points, linestrings, rings, etc.)
      Dim subGeomPart As String 'Stores coord string for a single geometry part (ring, point, etc.)
      Dim xyPairs As String() 'Stores all xy-pairs for a geometry part
      Dim xyPair As String() 'Stores a single xy-pair

      Dim origXStr As String, origYStr As String
      Dim origXNum As Double, origYNum As Double
      Dim utmXY() As Double = Nothing

      Try
        If latLonCoords <> "" Then
          geomParts = latLonCoords.Trim.Split(New String() {geomSplitter}, StringSplitOptions.RemoveEmptyEntries)
          For partIndx As Integer = 0 To UBound(geomParts)
            geomPart = geomParts(partIndx)
            subGeomParts = geomPart.Trim.Split(New String() {geomPartSplitter}, StringSplitOptions.RemoveEmptyEntries)
            For subpartIndx As Integer = 0 To UBound(subGeomParts)
              subGeomPart = subGeomParts(subpartIndx)
              xyPairs = subGeomPart.Trim.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)
              For pairIndx As Integer = 0 To UBound(xyPairs)
                xyPair = xyPairs(pairIndx).Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries)
                origXStr = xyPair(0)
                origYStr = xyPair(1)
                If Double.TryParse(origXStr, origXNum) AndAlso Double.TryParse(origYStr, origYNum) Then
                  utmXY = GeoToUTM_Main(origYNum, origXNum)
                  xyPair(0) = utmXY(0).ToString
                  xyPair(1) = utmXY(1).ToString
                  zone = CInt(utmXY(2))
                  xyPairs(pairIndx) = Join(xyPair, xySplitter)
                End If
              Next
              subGeomParts(subpartIndx) = Join(xyPairs, ptSplitter)
            Next
            geomParts(partIndx) = Join(subGeomParts, geomPartSplitter)
          Next
          retVal = Join(geomParts, geomSplitter)
        End If

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal.Trim
    End Function

    Public Shared Function ConvertUtmCoordsToLatLon(ByRef utmCoords As String, ByVal zone As Integer, _
                                                    ByRef callInfo As String) As String

      callInfo = MethodIdentifier()
      Dim retVal As String = ""
      Dim geomParts As String() 'Stores coord strings for the geometrys within the string (e.g. multipolygon, polygon, multipoint, etc.)
      Dim geomPart As String 'Stores coord string for an individual geometry (polygon, linestring, etc.)
      Dim subGeomParts As String() 'Stores coord string for all subdivisions of a geometry (points, linestrings, rings, etc.)
      Dim subGeomPart As String 'Stores coord string for a single geometry part (ring, point, etc.)
      Dim xyPairs As String() 'Stores all xy-pairs for a geometry part
      Dim xyPair As String() 'Stores a single xy-pair

      Dim origXStr As String, origYStr As String
      Dim origXNum As Double, origYNum As Double
      Dim geoLatLng() As Double = Nothing

      Try
        If utmCoords <> "" Then
          geomParts = utmCoords.Trim.Split(New String() {geomSplitter}, StringSplitOptions.RemoveEmptyEntries)
          For partIndx As Integer = 0 To UBound(geomParts)
            geomPart = geomParts(partIndx)
            subGeomParts = geomPart.Trim.Split(New String() {geomPartSplitter}, StringSplitOptions.RemoveEmptyEntries)
            For subpartIndx As Integer = 0 To UBound(subGeomParts)
              subGeomPart = subGeomParts(subpartIndx)
              xyPairs = subGeomPart.Trim.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)
              For pairIndx As Integer = 0 To UBound(xyPairs)
                xyPair = xyPairs(pairIndx).Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries)
                origXStr = xyPair(0)
                origYStr = xyPair(1)
                If Double.TryParse(origXStr, origXNum) AndAlso Double.TryParse(origYStr, origYNum) Then
                  geoLatLng = UTMToGeo_Main(origXNum, origYNum, zone, False) 'returns lat/lng in array
                  xyPair(0) = geoLatLng(1).ToString
                  xyPair(1) = geoLatLng(0).ToString
                  xyPairs(pairIndx) = Join(xyPair, xySplitter)
                End If
              Next
              subGeomParts(subpartIndx) = Join(xyPairs, ptSplitter)
            Next
            geomParts(partIndx) = Join(subGeomParts, geomPartSplitter)
          Next
          retVal = Join(geomParts, geomSplitter)
        End If

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function ClipPolygon(ByVal polyToPreserve As GGeom.IMultiPolygon, _
                   ByVal polyToClip As GGeom.IMultiPolygon, ByRef callInfo As String) As GGeom.IMultiPolygon
      'Clip a polygon based on another polygon, return the clipped polygon
      Dim retVal As GGeom.IMultiPolygon = Nothing
      Try
        If polyToPreserve IsNot Nothing Then
          Dim clippedGeom As GGeom.IGeometry
          clippedGeom = polyToClip.Difference(CType(polyToPreserve, GGeom.IGeometry)) 'clip

          Select Case clippedGeom.GeometryType
            Case GetType(NGeom.Point).Name
              callInfo &= "error: got point"
            Case GetType(NGeom.LineString).Name
              callInfo &= "error: got line"
            Case GetType(NGeom.Polygon).Name
              Dim apoly As GGeom.IPolygon = CType(clippedGeom, GGeom.IPolygon)
              retVal = ConvertPolygonIntoMultipolygon(apoly, Nothing)
            Case GetType(NGeom.MultiPolygon).Name
              retVal = CType(clippedGeom, GGeom.IMultiPolygon)
          End Select
        Else
          retVal = polyToClip
        End If

      Catch topex As NetTopologySuite.Geometries.TopologyException
        'TODO: what to do?
        callInfo &= "error: bad topology"
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      If retVal Is Nothing Then retVal = polyToClip
      Return retVal
    End Function

    ''' <summary>
    ''' Returns length in meters for a linestring in utm coords
    ''' </summary> 
    Public Shared Function GetLengthFromUtmLinestring(ByVal lineInLatLng As GGeom.ILineString, _
                            ByRef callInfo As String) As Double
      Dim localInfo As String = ""
      Dim retVal As Double = 0
      Try
        localInfo = ""
        Dim tmpCoords As String = GetCoordsStringFromGeom(lineInLatLng, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        localInfo = ""
        Dim utmLine As GGeom.ILineString = CreateLineStringFromCoordString(tmpCoords, localInfo, False)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        If utmLine IsNot Nothing Then retVal = utmLine.Length

      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns length in meters for a linestring in lat/lng coords
    ''' </summary> 
    Public Shared Function GetLengthFromLatLngLinestring(ByVal lineInLatLng As GGeom.ILineString, _
                            ByRef callInfo As String) As Double
      Dim localInfo As String = ""
      Dim retVal As Double = 0
      Try
        localInfo = ""
        Dim tmpCoords As String = GetCoordsStringFromGeom(lineInLatLng, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        localInfo = ""
        Dim utmLine As GGeom.ILineString = CreateLineStringFromCoordString(tmpCoords, localInfo, True)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        If utmLine IsNot Nothing Then retVal = utmLine.Length

      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns area in sq. meters for a polygon or multipolygon in wkb format
    ''' </summary> 
    Public Shared Function GetAreaForWkb(ByVal wkb As String, _
                                              ByRef callInfo As String) As Double
      'Convert poly to UTM and get area 
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As Double = 0
      Try
        Dim inGeom As GGeom.IGeometry = ConvertWkbToGeometry(wkb, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo : localInfo = ""
        Select Case inGeom.GetType.Name
          Case GetType(NGeom.Point).Name, GetType(NGeom.LineString).Name
            retVal = 0 : callInfo &= "Wrong kind of geometry (" & GetType(NGeom.Point).Name & ")"
          Case GetType(NGeom.Polygon).Name
            retVal += GetAreaFromLatLngPolygon(CType(inGeom, GGeom.IPolygon), localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= localInfo : localInfo = ""
          Case GetType(NGeom.MultiPolygon).Name
            retVal += GetAreaFromLatLngMultiPolygon(CType(inGeom, GGeom.IMultiPolygon), localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= localInfo : localInfo = ""
        End Select

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns area in sq. meters for a multipolygon in lat/lng coords
    ''' </summary> 
    Public Shared Function GetAreaFromLatLngMultiPolygon(ByVal multipolyInLatLng As GGeom.IMultiPolygon, _
                                              ByRef callInfo As String) As Double
      'Convert poly to UTM and get area
      Dim localInfo As String = ""
      Dim retVal As Double = 0
      Try
        If multipolyInLatLng IsNot Nothing Then
          For Each geom As GGeom.IGeometry In multipolyInLatLng.Geometries
            localInfo = ""
            retVal += GetAreaFromLatLngPolygon(CType(geom, GGeom.IPolygon), localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
          Next
        End If
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns area in sq. meters for a polygon in lat/lng coords
    ''' </summary> 
    Public Shared Function GetAreaFromLatLngPolygon(ByVal polyInLatLng As GGeom.IPolygon, _
                                              ByRef callInfo As String) As Double
      'Convert poly to UTM and get area 
      Dim localInfo As String = ""
      Dim retVal As Double = 0
      Try
        'TODO: set up for rings
        localInfo = ""
        Dim tmpCoords As String = GetCoordsStringFromGeom(polyInLatLng, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        localInfo = ""
        Dim utmPoly As GGeom.IMultiPolygon = CreateMultipolyFromCoordString(tmpCoords, localInfo, True)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
        If utmPoly IsNot Nothing Then retVal = utmPoly.Area

      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function CreateMultipolyFromCoordString_ORIG(ByVal coords As String, ByRef callInfo As String, _
                                                          Optional ByRef convertToUtm As Boolean = False) As GGeom.IMultiPolygon
      'Use geometry factory to create a polygon from a string of coordinates
      callInfo = MethodIdentifier()
      Dim errPart As Integer = 0
      Dim localInfo As String = ""
      Dim retVal As GGeom.IMultiPolygon = Nothing
      Dim uboundCoords As Integer
      Try
        Dim newCoord As GGeom.Coordinate
        Dim newX As String, newY As String
        Dim newXCoord As Double, newYCoord As Double

        Dim geomFact As NGeom.GeometryFactory = New NGeom.GeometryFactory
        Dim coordSeq As GGeom.ICoordinateSequence
        Dim shell As GGeom.ILinearRing = Nothing
        Dim newPoly As GGeom.IPolygon
        Dim polyList As New List(Of GGeom.IPolygon)
        'Dim ntsCoordsList As New List(Of GGeom.Coordinate)
        Dim ntsCoordsList As New NGeom.CoordinateList
        Dim ntsCoords As GGeom.Coordinate()
        Dim innerRingsList As New List(Of GGeom.ILinearRing)
        Dim innerRing As GGeom.ILinearRing

        Dim geomParts As String() 'Stores coord strings for the geometrys within the string (e.g. multipolygon, polygon, multipoint, etc.)
        Dim geomPart As String 'Stores coord string for an individual geometry (polygon, linestring, etc.)
        Dim subGeomParts As String() 'Stores coord string for all subdivisions of a geometry (points, linestrings, rings, etc.)
        Dim subGeomPart As String 'Stores coord string for a single geometry part (ring, point, etc.)
        Dim xyPairs As String() 'Stores all xy-pairs for a geometry part
        Dim xy As String() 'Stores a single xy-pair
        Dim coordsInUTM() As Double

        If String.IsNullOrWhiteSpace(coords) Then Throw New ArgumentException("Coords string is null or empty")
        errPart = 1
        geomParts = coords.Trim.Split(New String() {geomSplitter}, StringSplitOptions.RemoveEmptyEntries)
        'callInfo &= String.Format(" geomParts: {0} ", geomParts.Length)
        For partIndx As Integer = 0 To UBound(geomParts)
          geomPart = geomParts(partIndx)
          subGeomParts = geomPart.Trim.Split(New String() {geomPartSplitter}, StringSplitOptions.RemoveEmptyEntries)
          If 0 = subGeomParts.Length Then Continue For
          'callInfo &= String.Format(" subGeomParts: {0} ", subGeomParts.Length)

          'Assume first part is outer ring, others are inner rings
          subGeomPart = subGeomParts(0) 'get outer ring (shell)
          xyPairs = subGeomPart.Trim.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)
          errPart = 100 + partIndx
          ntsCoordsList = New NGeom.CoordinateList
          uboundCoords = UBound(xyPairs)
          For pairIndx As Integer = 0 To uboundCoords
            xy = xyPairs(pairIndx).Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries)
            newX = xy(0) 'string
            newY = xy(1) 'string
            'If values are doubles, process them
            If Double.TryParse(newY, newYCoord) AndAlso Double.TryParse(newX, newXCoord) Then
              'callInfo += String.Format(" ({0},{1}) ", newX, newY)
              If True = convertToUtm Then
                coordsInUTM = GeoToUTM_Main(newYCoord, newXCoord) 'Convert to UTM
                newXCoord = coordsInUTM(0)
                newYCoord = coordsInUTM(1)
              End If
              newCoord = New GGeom.Coordinate(newXCoord, newYCoord)
              ntsCoordsList.Add(newCoord)
            End If
          Next
          ntsCoordsList.CloseRing() 'Verify first and last are same
          ntsCoords = ntsCoordsList.ToArray

          errPart = 200 + partIndx
          coordSeq = NGeom.Implementation.CoordinateArraySequenceFactory.Instance.Create(ntsCoords)
          errPart = 300 + partIndx
          shell = New NGeom.LinearRing(coordSeq, geomFact)

          errPart = 400 + partIndx
          If Not shell.IsSimple Then
            retVal = Nothing
            Exit Try 'BOMB out if non-simple

          End If
          errPart = 700 + partIndx
          If shell Is Nothing Then Continue For

          errPart = 800 + partIndx
          If shell.IsValid AndAlso shell.IsSimple Then
            'Create interior rings
            innerRingsList = New List(Of GGeom.ILinearRing)
            For subpartIndx As Integer = 1 To UBound(subGeomParts)
              subGeomPart = subGeomParts(subpartIndx)
              xyPairs = subGeomPart.Trim.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)

              ntsCoordsList = New NGeom.CoordinateList
              uboundCoords = UBound(xyPairs)
              For pairIndx As Integer = 0 To uboundCoords
                xy = xyPairs(pairIndx).Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries)
                newX = xy(0) 'string
                newY = xy(1) 'string
                'If values are doubles, process them
                If Double.TryParse(newY, newYCoord) AndAlso Double.TryParse(newX, newXCoord) Then
                  callInfo += String.Format(" ({0},{1}) ", newX, newY)
                  If True = convertToUtm Then
                    coordsInUTM = GeoToUTM_Main(newYCoord, newXCoord) 'Convert to UTM
                    newXCoord = coordsInUTM(0)
                    newYCoord = coordsInUTM(1)
                  End If
                  newCoord = New GGeom.Coordinate(newXCoord, newYCoord)
                  ntsCoordsList.Add(newCoord)
                End If
              Next
              ntsCoordsList.CloseRing() 'Verify first and last are same
              ntsCoords = ntsCoordsList.ToArray

              coordSeq = NGeom.Implementation.CoordinateArraySequenceFactory.Instance.Create(ntsCoords)
              innerRing = New NGeom.LinearRing(coordSeq, geomFact)

              If Not innerRing.IsSimple Then
                Continue For
              End If
              If innerRing.IsValid Then
                innerRingsList.Add(innerRing)
              Else
                callInfo &= String.Format("{0} error in-ring {1} is nothing: {2}", Environment.NewLine, subpartIndx, (innerRing Is Nothing).ToString)
              End If

            Next

            errPart = 830 + partIndx
            newPoly = geomFact.CreatePolygon(shell, innerRingsList.ToArray)
            If Not newPoly.IsValid Then newPoly = CType(newPoly.Buffer(0), GGeom.IPolygon) 'try to clean it
            If newPoly.IsValid Then
              polyList.Add(newPoly)
            Else
              callInfo &= Environment.NewLine & " error poly is nothing: " & (newPoly Is Nothing).ToString
            End If

            errPart = 860 + partIndx
            Dim polys() As GGeom.IPolygon = polyList.ToArray
            retVal = geomFact.CreateMultiPolygon(polys)

          Else
            callInfo &= String.Format("{0} error shell is nothing: {1}", Environment.NewLine, (shell Is Nothing).ToString)
          End If

        Next

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= String.Format(" error: {0}", ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function CreateMultipolyFromCoordString(ByVal coords As String, ByRef callInfo As String, _
                                                          Optional ByRef convertToUtm As Boolean = False) As GGeom.IMultiPolygon
      'Use geometry factory to create a polygon from a string of coordinates 
      Dim localInfo As String = ""
      Dim retVal As GGeom.IMultiPolygon = Nothing
      Try
        Dim geomFact As NGeom.GeometryFactory = New NGeom.GeometryFactory
        Dim shell As GGeom.ILinearRing = Nothing
        Dim newPoly As GGeom.IPolygon
        Dim totalGeom As GGeom.IGeometry = Nothing
        Dim innerRingsList As List(Of GGeom.ILinearRing)
        Dim innerRing As GGeom.ILinearRing

        Dim geomParts As String() 'Stores coord strings for the geometrys within the string (e.g. multipolygon, polygon, multipoint, etc.)
        Dim geomPart As String 'Stores coord string for an individual geometry (polygon, linestring, etc.)
        Dim subGeomParts As String() 'Stores coord string for all subdivisions of a geometry (points, linestrings, rings, etc.)
        Dim subGeomPart As String 'Stores coord string for a single geometry part (ring, point, etc.)

        If String.IsNullOrWhiteSpace(coords) Then Exit Try 'BOMB out if non-simple Throw New ArgumentException("Coords string is null or empty")

        geomParts = coords.Trim.Split(New String() {geomSplitter}, StringSplitOptions.RemoveEmptyEntries)
        For partIndx As Integer = 0 To UBound(geomParts)
          geomPart = geomParts(partIndx)
          subGeomParts = geomPart.Trim.Split(New String() {geomPartSplitter}, StringSplitOptions.RemoveEmptyEntries)
          If 0 = subGeomParts.Length Then Continue For

          'Assume first part is outer ring, others are inner rings
          subGeomPart = subGeomParts(0) 'get outer ring (shell)

          localInfo = ""
          shell = CreateLinearRingFromCoordString(subGeomPart, localInfo, convertToUtm)
          If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

          If Not shell.IsSimple Then
            retVal = Nothing
            Exit Try 'BOMB out if non-simple
          End If
          If shell Is Nothing Then Continue For

          If shell.IsValid AndAlso shell.IsSimple Then
            'Create interior rings
            innerRingsList = New List(Of GGeom.ILinearRing)
            For subpartIndx As Integer = 1 To UBound(subGeomParts)
              subGeomPart = subGeomParts(subpartIndx)

              localInfo = ""
              innerRing = CreateLinearRingFromCoordString(subGeomPart, localInfo, convertToUtm)
              If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

              If Not innerRing.IsSimple Then Continue For
              If innerRing.IsValid Then
                innerRingsList.Add(innerRing)
              Else
                callInfo &= String.Format("{0} error in-ring {1} is nothing: {2}", Environment.NewLine, subpartIndx, (innerRing Is Nothing).ToString)
              End If

            Next

            newPoly = geomFact.CreatePolygon(shell, innerRingsList.ToArray)
            If Not newPoly.IsValid Then newPoly = CType(newPoly.Buffer(0), GGeom.IPolygon) 'try to clean it

            If newPoly.IsValid Then
              If totalGeom IsNot Nothing Then
                totalGeom = totalGeom.Union(CType(newPoly, GGeom.IGeometry))
              Else
                totalGeom = CType(newPoly, GGeom.IGeometry)
              End If
              totalGeom = SNAP.GeometrySnapper.SnapToSelf(totalGeom, 0.000001, True) 'deal with slivers
            Else
              callInfo &= Environment.NewLine & " error poly is nothing: " & (newPoly Is Nothing).ToString
            End If

          Else
            callInfo &= String.Format("{0} error shell is nothing: {1}", Environment.NewLine, (shell Is Nothing).ToString)
          End If

        Next

        Select Case totalGeom.GetType.Name
          Case GetType(NGeom.Point).Name, GetType(NGeom.LineString).Name
            callInfo &= Environment.NewLine & "error snapping: wrong geometry type: " & totalGeom.GetType.Name
          Case GetType(NGeom.Polygon).Name
            localInfo = ""
            retVal = ConvertPolygonIntoMultipolygon(CType(totalGeom, GGeom.IPolygon), localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
          Case GetType(NGeom.MultiPolygon).Name
            retVal = CType(totalGeom, GGeom.IMultiPolygon)
          Case Else
            callInfo &= Environment.NewLine & "error snapping: wrong geometry type: " & totalGeom.GetType.Name
        End Select

      Catch ex As Exception
        callInfo &= String.Format(" error: {0}", ex.Message)
      End Try

      If Not String.IsNullOrWhiteSpace(callInfo) Then callInfo = MethodIdentifier() & " " & callInfo
      Return retVal
    End Function

    Public Shared Function CreateMultipolyFromCoordString_NEW(ByVal coords As String, ByRef callInfo As String, _
                                                          Optional ByRef convertToUtm As Boolean = False) As GGeom.IMultiPolygon
      'Use geometry factory to create a polygon from a string of coordinates
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As GGeom.IMultiPolygon = Nothing
      Dim isBadMulti As Boolean = False
      Try
        Dim geomParts As String() 'Stores coord strings for the geometrys within the string (e.g. multipolygon, polygon, multipoint, etc.)
        Dim geomPart As String 'Stores coord string for an individual geometry (polygon, linestring, etc.)
        Dim geomFact As NGeom.GeometryFactory = New NGeom.GeometryFactory
        Dim newPoly As GGeom.IPolygon
        Dim polyList As New List(Of GGeom.IPolygon)

        geomParts = coords.Trim.Split(New String() {geomSplitter}, StringSplitOptions.RemoveEmptyEntries)
        If 0 = geomParts.Length Then Throw New ArgumentException("Coords string is null or empty")

        'Create polys
        For geomIdx As Integer = 0 To UBound(geomParts)
          geomPart = geomParts(geomIdx)
          'callInfo &= String.Format(Environment.NewLine & " error: {0}", geomPart) 'DEBUG
          newPoly = CreatePolyFromCoordString(geomPart, localInfo, convertToUtm)
          If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
          If newPoly IsNot Nothing Then polyList.Add(newPoly)
        Next

        If polyList.Count = 0 Then isBadMulti = True : Throw New ArgumentException("No polygons are valid for this multipolygon") ' no polys are good

        Dim polys() As GGeom.IPolygon = polyList.ToArray
        retVal = geomFact.CreateMultiPolygon(polys)

        If Not retVal.IsSimple Then isBadMulti = True : Throw New ArgumentException("Multipolygon is not simple") 'self-intersections
        'If Not retVal.IsValid Then isBadMulti = True : Throw New ArgumentException("Multipolygon is not valid")

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= String.Format(" error: {0}", ex.Message)
      End Try
      If isBadMulti = True Then retVal = Nothing

      'Try
      '  ' The tolerance is measured in the unit of the geometry. 
      '  ' If decimal degrees then 1 decimal degree, if feet then one foot etc. 
      '  Dim tolerance As Double = 1
      '  If retVal IsNot Nothing Then retVal = CType(Simplify(CType(retVal, GGeom.IGeometry), tolerance), GGeom.IMultiPolygon)
      'Catch ex As Exception
      '  callInfo &= String.Format(" error (simplify): {0}", ex.Message)
      'End Try
      Return retVal
    End Function

    Public Shared Function CreatePolyFromCoordString(ByVal coords As String, ByRef callInfo As String, _
                                                          Optional ByRef convertToUtm As Boolean = False) As GGeom.IPolygon
      'Use geometry factory to create a polygon from a string of coordinates
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As GGeom.IPolygon = Nothing
      Dim isBadPoly = False
      Try
        Dim geomParts As String() 'Stores coord strings for the geometrys within the string (e.g. multipolygon, polygon, multipoint, etc.)
        geomParts = coords.Trim.Split(New String() {geomSplitter}, StringSplitOptions.RemoveEmptyEntries)
        coords = geomParts(0) 'only want the first geometry
        If String.IsNullOrWhiteSpace(coords) Then Throw New ArgumentException("Coords string is null or empty")

        Dim geomFact As NGeom.GeometryFactory = New NGeom.GeometryFactory
        Dim shell As GGeom.ILinearRing = Nothing
        Dim innerRingsList As New List(Of GGeom.ILinearRing)
        Dim innerRing As GGeom.ILinearRing

        Dim subGeomParts As String() 'Stores coord string for all subdivisions of a geometry (points, linestrings, rings, etc.)
        Dim subGeomPart As String 'Stores coord string for a single geometry part (ring, point, etc.)

        subGeomParts = coords.Trim.Split(New String() {geomPartSplitter}, StringSplitOptions.RemoveEmptyEntries)
        If 0 = subGeomParts.Length Then Throw New ArgumentException("Coords string is null or empty")

        'find shell (assumption is that it's first, but use first good one)
        Dim shellIdx = -1
        For subpartIndx As Integer = 0 To UBound(subGeomParts)
          subGeomPart = subGeomParts(subpartIndx)
          shell = CreateLinearRingFromCoordString(subGeomPart, localInfo, convertToUtm)
          If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
          'callInfo &= String.Format(Environment.NewLine & " shell error: {0}", shell.Area) 'DEBUG
          If shell IsNot Nothing AndAlso shellIdx < 0 Then shellIdx = subpartIndx
        Next

        If shellIdx = -1 Then isBadPoly = True : Throw New ArgumentException("No rings are valid for the polygon") ' no rings are good

        'Create interior rings
        innerRingsList = New List(Of GGeom.ILinearRing)
        For subpartIndx As Integer = shellIdx + 1 To UBound(subGeomParts)
          subGeomPart = subGeomParts(subpartIndx)
          'callInfo &= String.Format(Environment.NewLine & " error: {0}", subGeomPart) 'DEBUG
          innerRing = CreateLinearRingFromCoordString(subGeomPart, localInfo, convertToUtm)
          If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
          If innerRing IsNot Nothing Then innerRingsList.Add(innerRing)
          'callInfo &= String.Format(Environment.NewLine & " ring error: {0}/{1}", (innerRing Is Nothing).ToString, innerRing.Area) 'DEBUG
        Next

        'callInfo &= String.Format(Environment.NewLine & " poly error: {0}/{1}", (shell Is Nothing).ToString, innerRingsList.Count) 'DEBUG
        retVal = geomFact.CreatePolygon(shell, innerRingsList.ToArray)
        'retVal = CType(retVal.Buffer(0), GGeom.IPolygon) 'try to clean it

        If Not retVal.IsSimple Then isBadPoly = True : Throw New ArgumentException("Polygon is not simple") 'self-intersections
        'If Not retVal.IsValid Then isBadPoly = True : Throw New ArgumentException("Polygon is not valid")

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= String.Format(" error: {0}", ex.Message)
      End Try
      If isBadPoly = True Then retVal = Nothing

      'Try
      '  ' The tolerance is measured in the unit of the geometry. 
      '  ' If decimal degrees then 1 decimal degree, if feet then one foot etc. 
      '  Dim tolerance As Double = 1
      '  If retVal IsNot Nothing Then retVal = CType(Simplify(CType(retVal, GGeom.IGeometry), tolerance), GGeom.IPolygon)
      'Catch ex As Exception
      '  callInfo &= String.Format(" error (simplify): {0}", ex.Message)
      'End Try
      Return retVal
    End Function

    Public Shared Function CreatePolyFromCoordString_OLD(ByVal coords As String, ByRef callInfo As String) As GGeom.IPolygon
      'Use geometry factory to create a polygon from a string of coordinates
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As GGeom.IPolygon = Nothing
      Dim ordCount As Integer
      Dim uboundCoords As Integer
      Dim errPart As Integer = 0
      Try
        Dim someCoords() As String = coords.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)
        uboundCoords = UBound(someCoords)
        Dim ntsCoords(uboundCoords) As GGeom.Coordinate
        Dim newCoord As GGeom.Coordinate
        Dim newX As String, newY As String
        Dim newXCoord As Double, newYCoord As Double

        errPart = 1
        'Create coord Array
        Dim tempCoords() As String
        Dim XYpair As String
        For ordCount = 0 To uboundCoords
          XYpair = someCoords(ordCount)
          tempCoords = XYpair.Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries) 'split it
          newX = tempCoords(0) 'string
          newY = tempCoords(1) 'string
          'If values are doubles, process them
          If Double.TryParse(newY, newYCoord) AndAlso Double.TryParse(newX, newXCoord) Then
            newCoord = New GGeom.Coordinate(newXCoord, newYCoord)
            ntsCoords(ordCount) = newCoord
          End If
        Next

        errPart = 2
        'Verify first and last are same
        If Not ntsCoords(0).Equals(ntsCoords(ntsCoords.Length - 1)) Then
          ReDim Preserve ntsCoords(ntsCoords.Length) 'add a closing coordinate slot
          ntsCoords(ntsCoords.Length - 1) = ntsCoords(0) 'close the poly 
        End If

        errPart = 3
        Dim geomFact As NGeom.GeometryFactory = New NGeom.GeometryFactory
        Dim coordSeq As GGeom.ICoordinateSequence = NGeom.Implementation.CoordinateArraySequenceFactory.Instance.Create(ntsCoords)
        errPart = 4
        Dim shell As GGeom.ILinearRing = New NGeom.LinearRing(coordSeq, geomFact)
        errPart = 5
        callInfo &= Environment.NewLine & " shell: " & (shell Is Nothing).ToString
        If shell IsNot Nothing AndAlso shell.IsValid Then
          errPart = 6
          Dim newPoly As GGeom.IPolygon = geomFact.CreatePolygon(shell, Nothing)
          newPoly = CType(newPoly.Buffer(0), GGeom.IPolygon) 'should simplify the shape
          errPart = 7
          If Not newPoly.IsValid Then newPoly = CType(newPoly.Buffer(0), GGeom.IPolygon)
          If newPoly.IsValid Then
            retVal = newPoly
          Else
            callInfo &= Environment.NewLine & " error poly: " & (newPoly Is Nothing).ToString
          End If
        Else
          callInfo &= Environment.NewLine & " error shell: " & (shell Is Nothing).ToString
        End If

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= String.Format(" error part {1}: {0}", ex.Message, errPart)
      End Try
      Return retVal
    End Function

    Public Shared Function CreateLinearRingFromCoordString(ByVal coords As String, ByRef callInfo As String, _
                                                          Optional ByRef convertToUtm As Boolean = False) As GGeom.ILinearRing
      'Use geometry factory to create a linear ring from a string of coordinates
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As GGeom.ILinearRing = Nothing
      Dim uboundCoords As Integer
      Dim isBadRing = False
      Dim errPart = 0
      Try
        Dim geomParts As String() 'Stores coord strings for the geometrys within the string (e.g. multipolygon, polygon, multipoint, etc.)
        geomParts = coords.Trim.Split(New String() {geomSplitter}, StringSplitOptions.RemoveEmptyEntries)
        coords = geomParts(0) 'only want first geometry
        If String.IsNullOrWhiteSpace(coords) Then Throw New ArgumentException("Coords string is null or empty")

        Dim subGeomParts As String() 'Stores coord string for all subdivisions of a geometry (points, linestrings, rings, etc.)
        subGeomParts = coords.Trim.Split(New String() {geomPartSplitter}, StringSplitOptions.RemoveEmptyEntries)
        coords = subGeomParts(0) 'only want a ring
        If 0 = subGeomParts.Length Then Throw New ArgumentException("Coords string is null or empty")

        Dim newCoord As GGeom.Coordinate
        Dim newX As String, newY As String
        Dim newXCoord As Double, newYCoord As Double

        Dim geomFact As NGeom.GeometryFactory = New NGeom.GeometryFactory
        Dim coordSeq As GGeom.ICoordinateSequence
        Dim ntsCoordsList As New NGeom.CoordinateList
        Dim ntsCoords As GGeom.Coordinate()

        Dim xyPairs As String() 'Stores all xy-pairs for a geometry part
        Dim xy As String() 'Stores a single xy-pair
        Dim coordsInUTM() As Double
        errPart = 100
        xyPairs = coords.Trim.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)
        ntsCoordsList = New NGeom.CoordinateList
        uboundCoords = UBound(xyPairs)
        For pairIndx As Integer = 0 To uboundCoords
          xy = xyPairs(pairIndx).Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries)
          newX = xy(0) 'string
          newY = xy(1) 'string
          'If values are doubles, process them
          errPart = 200 + pairIndx
          If Double.TryParse(newY, newYCoord) AndAlso Double.TryParse(newX, newXCoord) Then
            If True = convertToUtm Then
              coordsInUTM = GeoToUTM_Main(newYCoord, newXCoord) 'Convert to UTM
              newXCoord = coordsInUTM(0)
              newYCoord = coordsInUTM(1)
            End If
            errPart = 300 + pairIndx
            newCoord = New GGeom.Coordinate(newXCoord, newYCoord)
            If ntsCoordsList.Count = 0 OrElse Not newCoord.Equals(ntsCoordsList.Last) Then ntsCoordsList.Add(newCoord)
          End If
        Next
        errPart = 400
        If ntsCoordsList.Count < 3 Then isBadRing = True : Throw New ArgumentException("Coords string is null or empty") 'not a ring
        If ntsCoordsList.Count = 3 AndAlso ntsCoordsList.First.Equals(ntsCoordsList.Last) Then isBadRing = True : Throw New ArgumentException("Not enough points for a ring") 'only 2 distinct points
        errPart = 500
        ntsCoordsList.CloseRing() 'Verify first and last are same
        ntsCoords = ntsCoordsList.ToArray
        errPart = 600
        coordSeq = NGeom.Implementation.CoordinateArraySequenceFactory.Instance.Create(ntsCoords)
        retVal = New NGeom.LinearRing(coordSeq, geomFact)
        errPart = 700
        'innerRing = CType(innerRing.Buffer(0), GGeom.ILinearRing) 'clean it for testing '===== this bombs cuz it returns a polygon geometry

        errPart = 800
        If Not retVal.IsSimple Then isBadRing = True : Throw New ArgumentException("Ring is not simple") 'self-intersections
        If Not retVal.IsValid Then isBadRing = True : Throw New ArgumentException("Ring is not valid")

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= String.Format(" error (part {1}): {0}", ex.Message, errPart)
      End Try
      If isBadRing = True Then retVal = Nothing

      'Try
      '  ' The tolerance is measured in the unit of the geometry. 
      '  ' If decimal degrees then 1 decimal degree, if feet then one foot etc. 
      '  Dim tolerance As Double = 1
      '  If retVal IsNot Nothing Then retVal = CType(Simplify(CType(retVal, GGeom.IGeometry), tolerance), GGeom.ILinearRing)
      'Catch ex As Exception
      '  callInfo &= String.Format(" error (simplify): {0}", ex.Message)
      'End Try
      Return retVal
    End Function

    Public Shared Function CreateLineStringFromCoordString(ByVal someCoordNumbers As String, _
                           ByRef callInfo As String, Optional ByRef convertToUtm As Boolean = False) As GGeom.ILineString
      'Use geometry factory to create a linestring from a string of coordinate numbers, "y1,x1 y2,x2 etc."
      callInfo = MethodIdentifier()
      Dim retVal As GGeom.ILineString = Nothing
      Try
        Dim someCoords() As String = someCoordNumbers.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)
        Dim ntsCoords(someCoords.Length - 1) As GGeom.Coordinate
        Dim newCoord As GGeom.Coordinate
        Dim newX As String, newY As String
        Dim newXCoord As Double, newYCoord As Double
        Dim coordsInUTM() As Double

        'Create coord Array
        Dim tempCoords() As String
        Dim XYpair As String
        For ordCount As Integer = 0 To someCoords.Length - 1
          XYpair = someCoords(ordCount)
          tempCoords = XYpair.Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries) 'split it
          newX = tempCoords(0)
          newY = tempCoords(1)
          If Double.TryParse(newX, newXCoord) AndAlso Double.TryParse(newY, newYCoord) Then
            If True = convertToUtm Then
              coordsInUTM = GeoToUTM_Main(newYCoord, newXCoord) 'Convert to UTM
              newXCoord = coordsInUTM(0)
              newYCoord = coordsInUTM(1)
            End If
            newCoord = New GGeom.Coordinate(newXCoord, newYCoord)
            ntsCoords(ordCount) = newCoord
          End If
        Next

        Dim geomFact As NGeom.GeometryFactory = New NGeom.GeometryFactory
        Dim coordSeq As GeoAPI.Geometries.ICoordinateSequence = _
                       NGeom.Implementation.CoordinateArraySequenceFactory.Instance.Create(ntsCoords)
        retVal = geomFact.CreateLineString(coordSeq)

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function CreatePointFromCoordString(ByVal someCoordNumbers As String, _
                                              ByRef callInfo As String) As GGeom.IPoint
      'Use geometry factory to create a point from a string of coordinate numbers, just x and y
      callInfo = MethodIdentifier()
      Dim retVal As GGeom.IPoint = Nothing
      Try
        Dim someCoords() As String = someCoordNumbers.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)
        callInfo &= Environment.NewLine & MethodIdentifier() & ": " & someCoords.Count & Environment.NewLine
        Dim ntsCoords(someCoords.Length - 1) As GGeom.Coordinate
        Dim newCoord As GGeom.Coordinate
        Dim newX As String, newY As String
        Dim newXCoord As Double, newYCoord As Double

        'Create coord Array
        Dim tempCoords() As String
        Dim XYpair As String
        For ordCount As Integer = 0 To someCoords.Length - 1
          XYpair = someCoords(ordCount)
          tempCoords = XYpair.Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries) 'split it
          newX = tempCoords(0)
          newY = tempCoords(1)
          If Double.TryParse(newX, newXCoord) AndAlso Double.TryParse(newY, newYCoord) Then
            newCoord = New GGeom.Coordinate(newXCoord, newYCoord)
            ntsCoords(ordCount) = newCoord
          End If
        Next

        Dim geomFact As New NGeom.GeometryFactory
        Dim coordSeq As GeoAPI.Geometries.ICoordinateSequence = _
                       NGeom.Implementation.CoordinateArraySequenceFactory.Instance.Create(ntsCoords)
        retVal = geomFact.CreatePoint(coordSeq)

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function BufferPolygon(ByVal polyToBuffer As GGeom.IMultiPolygon, ByVal distance As Double, _
                   ByRef callInfo As String, Optional ByVal bufferDirection As String = "INCLUDE") As GGeom.IGeometry
      'Buffer a polygon based on a distance and optional inside inclusion
      callInfo = MethodIdentifier()

      Dim retVal As GGeom.IGeometry = Nothing
      Try
        Dim buffedPoly As GGeom.IGeometry = polyToBuffer.Buffer(distance, 16, GeoAPI.Operations.Buffer.EndCapStyle.Round)
        If buffedPoly.IsValid Then
          retVal = buffedPoly
        Else
          callInfo &= ": " & "buffered poly is not valid"
        End If

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function BufferLine(ByVal lineToBuffer As GGeom.ILineString, ByVal distance As Double, _
                   ByRef callInfo As String, Optional ByVal bufferDirection As String = "BOTH") As GGeom.IGeometry
      'Buffer a linestring based on a distance and an optional direction
      callInfo = MethodIdentifier()

      Dim retVal As GGeom.IGeometry = Nothing
      Try
        Dim buffedPoly As GGeom.IGeometry = lineToBuffer.Buffer(distance, 16, GeoAPI.Operations.Buffer.EndCapStyle.Round)
        If buffedPoly.IsValid Then
          retVal = buffedPoly
        Else
        End If

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function BufferPoint(ByVal pointToBuffer As GGeom.IPoint, ByVal distance As Double, _
                   ByRef callInfo As String) As GGeom.IGeometry
      'Buffer a point based on a distance
      callInfo = MethodIdentifier()

      Dim retVal As GGeom.IGeometry = Nothing
      Try
        Dim buffedPoly As GGeom.IGeometry = pointToBuffer.Buffer(distance, 16, GeoAPI.Operations.Buffer.EndCapStyle.Round)
        If buffedPoly.IsValid Then
          retVal = buffedPoly
        Else
        End If

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Shared Function ConvertPolygonIntoMultipolygon(ByVal inGeom As GGeom.IPolygon, _
                               ByRef callInfo As String) As GGeom.IMultiPolygon
      callInfo = MethodIdentifier()
      Dim retval As GGeom.IMultiPolygon = Nothing
      Try
        Dim geomFact As NGeom.GeometryFactory = New NGeom.GeometryFactory
        Dim polys() As GGeom.IPolygon = New GGeom.IPolygon() {inGeom}
        retval = geomFact.CreateMultiPolygon(polys)

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retval
    End Function

    Public Shared Function ConvertNtsCoordsToString(ByVal ntsCoords() As GGeom.Coordinate, ByRef callInfo As String, _
                                Optional ByVal precision As Integer = 13) As String
      callInfo = MethodIdentifier()
      Dim retVal As New StringBuilder
      Try
        For Each xypair As GGeom.Coordinate In ntsCoords
          retVal.Append(FormatNumber(xypair.X, precision, , TriState.False, TriState.False) & xySplitter & _
                        FormatNumber(xypair.Y, precision, , TriState.False, TriState.False) & ptSplitter) 'This is correct order
        Next

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal.ToString.Trim
    End Function

    Private Shared Function Simplify(ByVal inGeom As GGeom.IGeometry, ByVal tolerance As Double) As GGeom.IGeometry
      ' There is also a faster DouglasPeuckerSimplifier but it may not keep the original shape 
      Dim simplifiedGeometry As GGeom.IGeometry = DirectCast(NSimp.TopologyPreservingSimplifier.Simplify(inGeom, tolerance), GGeom.IGeometry)
      Return simplifiedGeometry
    End Function

#End Region

#Region "Helper Methods"

    ''' <summary>
    ''' Determines if a shape already exists for a feature.
    ''' </summary>
    Public Shared Function IsShapeExistForFeature(ByVal feattype As String, ByVal oid As Integer, ByRef callInfo As String) As Boolean
      callInfo = MethodIdentifier()
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Dim tempVal As New StringBuilder
      Try
        Dim featTableName As String = ""
        Select Case feattype.ToUpper
          Case "F"
            featTableName = "LandManagementUnit"
          Case Else
            Throw New ArgumentException(String.Format("Feature type is invalid: {0}", feattype), feattype)
        End Select
        Dim cmdText As String = "SELECT Shape FROM " & dataSchema & "." & featTableName & " AS MU " & _
                " WHERE MU.ObjectID=" & oid & " "

        Dim isShapeExists As Boolean = False
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = cmdText

            If conn.State = ConnectionState.Closed Then conn.Open()
            Using readr As SqlDataReader = cmd.ExecuteReader
              While readr.Read
                If Not IsDBNull(readr("Shape")) Then
                  Dim shp As String = readr("Shape").ToString.Trim
                  If shp.Length > 0 Then isShapeExists = True
                End If
              End While
            End Using
          End Using
        End Using
        retVal = isShapeExists

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Creates a delimited string of coords for a geometry.
    ''' </summary>
    Public Shared Function GetCoordsStringFromGeom(ByVal inGeom As GGeom.IGeometry, ByRef callInfo As String) As String
      Dim retVal As String = ""
      If inGeom Is Nothing Then Return retVal
      Dim localInfo As String = ""
      Dim tempVal As New StringBuilder
      Try
        Select Case inGeom.GetType.Name
          Case GetType(NGeom.Point).Name, GetType(NGeom.LineString).Name
            localInfo = ""
            tempVal.Append(ConvertNtsCoordsToString(inGeom.Coordinates, localInfo))
            If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
          Case GetType(NGeom.Polygon).Name
            Dim inPoly As GGeom.IPolygon = CType(inGeom, GGeom.IPolygon)
            localInfo = ""
            tempVal.Append(ConvertNtsCoordsToString(CType(inPoly.Shell, GGeom.IGeometry).Coordinates, localInfo))
            If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
            For intRingIndx As Integer = 0 To inPoly.NumInteriorRings - 1
              localInfo = ""
              tempVal.Append(geomPartSplitter + (ConvertNtsCoordsToString(CType(inPoly.GetInteriorRingN(intRingIndx), GGeom.IGeometry).Coordinates, localInfo)))
              If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
            Next
          Case GetType(NGeom.MultiPolygon).Name
            Dim amult As GGeom.IMultiPolygon = CType(inGeom, GGeom.IMultiPolygon)

            For Each geom As GGeom.IGeometry In amult.Geometries
              Dim inPoly As GGeom.IPolygon = CType(geom, GGeom.IPolygon)
              callInfo &= String.Format(" shell: {0}. ", CType(inPoly.Shell, GGeom.IGeometry).Coordinates.Length)
              localInfo = ""
              tempVal.Append(ConvertNtsCoordsToString(CType(inPoly.Shell, GGeom.IGeometry).Coordinates, localInfo))
              If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
              callInfo &= String.Format(" int: {0}. ", inPoly.InteriorRings.Length)
              For intRingIndx As Integer = 0 To inPoly.NumInteriorRings - 1
                localInfo = ""
                tempVal.Append(geomPartSplitter + ConvertNtsCoordsToString(CType(inPoly.GetInteriorRingN(intRingIndx), GGeom.IGeometry).Coordinates, localInfo))
                If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
              Next
              tempVal.Append(geomSplitter)
            Next
            'trim last geomSplitter
            tempVal = tempVal.Replace(geomSplitter, "", tempVal.Length - geomSplitter.Length, geomSplitter.Length)
          Case Else
            callInfo &= Environment.NewLine & "error: Wrong geometry type: " & inGeom.GetType.Name
        End Select
        retVal = tempVal.ToString.Trim
        localInfo = ""
        retVal = TrimCoords(retVal, coordsPrecision, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Creates a delimited string of utm coords for a geometry
    ''' </summary>
    ''' <param name="latlngGeom"></param>
    ''' <param name="zone">Pass in uninitialized. Set upon conversion.</param>
    ''' <param name="callInfo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function GetUtmCoordsStringFromLatLngGeom(ByVal latlngGeom As GGeom.IGeometry, ByRef zone As Integer, ByRef callInfo As String) As String
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As String = ""
      Try
        Dim geoCoords As String = GetCoordsStringFromGeom(latlngGeom, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""
        retVal = ConvertLatLonCoordsToUtm(geoCoords, zone, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Creates a delimited string of lng/lat coords for a geometry
    ''' </summary>
    ''' <param name="latlngGeom"></param>
    ''' <param name="zone"></param>
    ''' <param name="callInfo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function GetLatLngCoordsStringFromUtmGeom(ByVal latlngGeom As GGeom.IGeometry, ByVal zone As Integer, ByRef callInfo As String) As String
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As String = ""
      Try
        Dim utmCoords As String = GetCoordsStringFromGeom(latlngGeom, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""
        retVal = ConvertLatLonCoordsToUtm(utmCoords, zone, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Converts a geometry from latlng-based to utm-based.
    ''' </summary>
    ''' <param name="latlngGeom"></param>
    ''' <param name="callInfo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function TransformGeomFromGeoToUtm(ByVal latlngGeom As GGeom.IGeometry, ByRef callInfo As String) As GGeom.IGeometry
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As GGeom.IGeometry = Nothing
      Dim zone As Integer
      Try
        Dim geoCoords As String = GetCoordsStringFromGeom(latlngGeom, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""
        Dim utmCoords As String = ConvertLatLonCoordsToUtm(geoCoords, zone, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""

        Select Case latlngGeom.GetType.Name
          Case GetType(NGeom.Point).Name
            retVal = CreatePointFromCoordString(utmCoords, localInfo)
          Case GetType(NGeom.LineString).Name
            retVal = CreateLineStringFromCoordString(utmCoords, localInfo)
          Case GetType(NGeom.Polygon).Name
            retVal = CreatePolyFromCoordString(utmCoords, localInfo)
          Case GetType(NGeom.MultiPolygon).Name
            retVal = CreateMultipolyFromCoordString(utmCoords, localInfo)
        End Select
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Converts a geometry from utm-based to latlng-based.
    ''' </summary>
    ''' <param name="utmGeom"></param>
    ''' <param name="zone"></param>
    ''' <param name="callInfo"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function TransformGeomFromUtmToGeo(ByVal utmGeom As GGeom.IGeometry,
                      ByVal zone As Integer, ByRef callInfo As String) As GGeom.IGeometry
      callInfo = MethodIdentifier()
      Dim localInfo As String = ""
      Dim retVal As GGeom.IGeometry = Nothing
      Try
        Dim geoCoords As String = GetCoordsStringFromGeom(utmGeom, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""
        Dim utmCoords As String = ConvertUtmCoordsToLatLon(geoCoords, zone, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""

        Select Case utmGeom.GetType.Name
          Case GetType(NGeom.Point).Name
            retVal = CreatePointFromCoordString(utmCoords, localInfo)
          Case GetType(NGeom.LineString).Name
            retVal = CreateLineStringFromCoordString(utmCoords, localInfo)
          Case GetType(NGeom.Polygon).Name
            retVal = CreatePolyFromCoordString(utmCoords, localInfo)
          Case GetType(NGeom.MultiPolygon).Name
            retVal = CreateMultipolyFromCoordString(utmCoords, localInfo)
        End Select
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo : localInfo = ""

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Reverses coordinate order from x,y to y,x or vice versa
    ''' </summary>
    ''' <param name="coords">String containing delimited coordinates</param>
    ''' <param name="callInfo">Function call information</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function ReverseCoords(ByVal coords As String, ByRef callInfo As String) As String
      callInfo = MethodIdentifier()
      Dim retVal As String = ""
      Dim geomParts As String() 'Stores coord strings for the geometrys within the string (e.g. multipolygon, polygon, multipoint, etc.)
      Dim geomPart As String 'Stores coord string for an individual geometry (polygon, linestring, etc.)
      Dim subGeomParts As String() 'Stores coord string for all subdivisions of a geometry (points, linestrings, rings, etc.)
      Dim subGeomPart As String 'Stores coord string for a single geometry part (ring, point, etc.)
      Dim xyPairs As String() 'Stores all xy-pairs for a geometry part
      Dim xy As String() 'Stores a single xy-pair
      Dim tmpCoord As String 'Use when swapping coordinates

      Try
        If coords <> "" Then
          geomParts = coords.Trim.Split(New String() {geomSplitter}, StringSplitOptions.RemoveEmptyEntries)
          For partIndx As Integer = 0 To UBound(geomParts)
            geomPart = geomParts(partIndx)
            subGeomParts = geomPart.Trim.Split(New String() {geomPartSplitter}, StringSplitOptions.RemoveEmptyEntries)
            For subpartIndx As Integer = 0 To UBound(subGeomParts)
              subGeomPart = subGeomParts(subpartIndx)
              xyPairs = subGeomPart.Trim.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)
              For pairIndx As Integer = 0 To UBound(xyPairs)
                xy = xyPairs(pairIndx).Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries)
                tmpCoord = xy(0)
                xy(0) = xy(1)
                xy(1) = tmpCoord
                xyPairs(pairIndx) = Join(xy, xySplitter)
              Next
              subGeomParts(subpartIndx) = Join(xyPairs, ptSplitter)
            Next
            geomParts(partIndx) = Join(subGeomParts, geomPartSplitter)
          Next
          retVal = Join(geomParts, geomSplitter)
        End If

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Trims coordinate values to indicated precision
    ''' </summary>
    ''' <param name="coords">String containing delimited coordinates</param>
    ''' <param name="precision">Desired number of significant digits to the right of the decimal</param>
    ''' <param name="callInfo">Function call information</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function TrimCoords(ByVal coords As String, ByVal precision As Integer, _
                                ByRef callInfo As String) As String
      callInfo = MethodIdentifier()
      Dim retVal As String = ""
      Dim geomParts As String() 'Stores coord strings for the geometrys within the string (e.g. multipolygon, polygon, multipoint, etc.)
      Dim geomPart As String 'Stores coord string for an individual geometry (polygon, linestring, etc.)
      Dim subGeomParts As String() 'Stores coord string for all subdivisions of a geometry (points, linestrings, rings, etc.)
      Dim subGeomPart As String 'Stores coord string for a single geometry part (ring, point, etc.)
      Dim xyPairs As String() 'Stores all xy-pairs for a geometry part
      Dim xy As String() 'Stores a single xy-pair

      Try
        If coords <> "" Then
          geomParts = coords.Trim.Split(New String() {geomSplitter}, StringSplitOptions.RemoveEmptyEntries)
          For partIndx As Integer = 0 To UBound(geomParts)
            geomPart = geomParts(partIndx)
            subGeomParts = geomPart.Trim.Split(New String() {geomPartSplitter}, StringSplitOptions.RemoveEmptyEntries)
            For subpartIndx As Integer = 0 To UBound(subGeomParts)
              subGeomPart = subGeomParts(subpartIndx)
              xyPairs = subGeomPart.Trim.Split(New String() {ptSplitter}, StringSplitOptions.RemoveEmptyEntries)
              For pairIndx As Integer = 0 To UBound(xyPairs)
                xy = xyPairs(pairIndx).Split(New String() {xySplitter}, StringSplitOptions.RemoveEmptyEntries)
                xy(0) = FormatNumber(xy(0).Trim, precision, , TriState.False, TriState.False)
                xy(1) = FormatNumber(xy(1).Trim, precision, , TriState.False, TriState.False)
                xyPairs(pairIndx) = Join(xy, xySplitter)
              Next
              subGeomParts(subpartIndx) = Join(xyPairs, ptSplitter)
            Next
            geomParts(partIndx) = Join(subGeomParts, geomPartSplitter)
          Next
          retVal = Join(geomParts, geomSplitter)
        End If

        callInfo &= ": " & okayMsg
      Catch ex As Exception
        callInfo &= " error: " & ex.Message
      End Try
      Return retVal
    End Function

    'Public Function ConvertArrayToString(ByVal inArray As Array, ByVal divider As String) As String
    '  'Convert array into string with each array entry except the last one followed by the divider
    '  Dim retVal As New System.Text.StringBuilder
    '  Try
    '    For Countr As Integer = 0 To inArray.Length - 2
    '      retVal.Append(inArray(Countr))
    '      retVal.Append(divider)
    '    Next
    '    retVal.Append(inArray(inArray.Length - 1)) 'Add last value 
    '  Catch ex As Exception
    '    retVal.Append(Environment.NewLine & CF.FormatMessageForJSAlert(MethodIdentifier() & " error: ", ex.Message))
    '  End Try
    '  Return retVal.ToString
    'End Function

#End Region ' "Helper Methods"

#Region "Geographic/UTM Coordinate Converter"

    ' Download from http://home.hiwaay.net/~taylorc/toolbox/geography/geoutm.html
    Const pi As Double = 3.14159265358979

    '/* Ellipsoid model constants (actual values here are for WGS84) */
    Const sm_a As Double = 6378137.0
    Const sm_b As Double = 6356752.314
    Const sm_EccSquared As Double = 0.00669437999013

    Const UTMScaleFactor As Double = 0.9996

    ''' <summary>
    ''' The main function to start conversion from Geographic to UTM
    ''' </summary>
    ''' <param name="lat">Latitude of the point, in degrees</param>
    ''' <param name="lon">Longitude of the point, in degrees</param>
    ''' <returns>Array in 3 parts: Latitude in radians, Longitude in radians, utm zone</returns>
    ''' <remarks></remarks>
    Public Shared Function GeoToUTM_Main(ByVal lat As Double, ByVal lon As Double) As Double()
      Dim retVal(3) As Double
      Try
        Dim xy(2) As Double

        ' Compute the UTM zone.
        Dim tempZone As Integer = CInt(Math.Floor((lon + 180.0) / 6)) + 1
        Dim zone As Integer = LatLonToUTMXY(DegToRad(lat), DegToRad(lon), tempZone, xy)

        retVal(0) = xy(0) 'x
        retVal(1) = xy(1) 'y
        retVal(2) = zone

      Catch ex As Exception
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' The main function to start conversion from UTM to Geographic
    ''' </summary>
    ''' <param name="x">Latitude of the point, in radians</param>
    ''' <param name="y">Longitude of the point, in radians</param>
    ''' <param name="zone">The UTM zone in which the point lies</param>
    ''' <param name="southhemi">True if the point is in the southern hemisphere; false otherwise</param>
    ''' <returns>Array in 2 parts: Latitude in degrees, Longitude in degrees</returns>
    ''' <remarks></remarks>
    Public Shared Function UTMToGeo_Main(ByVal x As Double, ByVal y As Double, _
                   ByVal zone As Integer, ByVal southhemi As Boolean) As Double()
      Dim retVal(2) As Double
      Try
        UTMXYToLatLon(x, y, zone, southhemi, retVal)
        Dim lat As Double = RadToDeg(retVal(0))
        Dim lon As Double = RadToDeg(retVal(1))
        retVal(0) = lat
        retVal(1) = lon

      Catch ex As Exception
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Converts degrees to radians
    ''' </summary>
    ''' <param name="deg">Coordinate of point in degrees</param>
    ''' <returns>Coordinate of point in radians</returns>
    ''' <remarks></remarks>
    Public Shared Function DegToRad(ByVal deg As Double) As Double
      Dim retVal As Double = 0
      Try
        retVal = (deg / 180.0 * pi)
      Catch ex As Exception
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Converts radians to degrees
    ''' </summary>
    ''' <param name="rad">Coordinate of point in radians</param>
    ''' <returns>Coordinate of point in degrees</returns>
    ''' <remarks></remarks>
    Public Shared Function RadToDeg(ByVal rad As Double) As Double
      Dim retVal As Double = 0
      Try
        retVal = (rad / pi * 180.0)
      Catch ex As Exception
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Computes the ellipsoidal distance from the equator to a point at a given latitude.
    ''' </summary>
    ''' <param name="phi">Latitude of the point, in radians</param>
    ''' <globals>sm_a - Ellipsoid model major axis; sm_b - Ellipsoid model minor axis.</globals>
    ''' <returns>The ellipsoidal distance of the point from the equator, in meters.</returns>
    ''' <remarks>Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J., GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.</remarks>
    Public Shared Function ArcLengthOfMeridian(ByVal phi As Double) As Double
      Dim retVal As Double
      Try
        Dim alpha As Double, beta As Double, gamma As Double, delta As Double, epsilon As Double
        Dim n As Double

        '/* Precalculate n */
        n = (sm_a - sm_b) / (sm_a + sm_b)

        '/* Precalculate alpha */
        alpha = ((sm_a + sm_b) / 2.0) * (1.0 + (Math.Pow(n, 2.0) / 4.0) + (Math.Pow(n, 4.0) / 64.0))

        '/* Precalculate beta */
        beta = (-3.0 * n / 2.0) + (9.0 * Math.Pow(n, 3.0) / 16.0) + (-3.0 * Math.Pow(n, 5.0) / 32.0)

        '/* Precalculate gamma */
        gamma = (15.0 * Math.Pow(n, 2.0) / 16.0) + (-15.0 * Math.Pow(n, 4.0) / 32.0)

        '/* Precalculate delta */
        delta = (-35.0 * Math.Pow(n, 3.0) / 48.0) + (105.0 * Math.Pow(n, 5.0) / 256.0)

        '/* Precalculate epsilon */
        epsilon = (315.0 * Math.Pow(n, 4.0) / 512.0)

        '/* Now calculate the sum of the series and return */
        retVal = alpha * (phi + (beta * Math.Sin(2.0 * phi)) + (gamma * Math.Sin(4.0 * phi)) + _
                       (delta * Math.Sin(6.0 * phi)) + (epsilon * Math.Sin(8.0 * phi)))

      Catch ex As Exception
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Determines the central meridian for the given UTM zone.
    ''' </summary>
    ''' <param name="zone">An integer value designating the UTM zone, range [1,60]</param>
    ''' <returns>
    '''   The central meridian for the given UTM zone, in radians, or zero
    '''   if the UTM zone parameter is outside the range [1,60].
    '''   Range of the central meridian is the radian equivalent of [-177,+177]. </returns>
    ''' <remarks></remarks>
    Public Shared Function UTMCentralMeridian(ByVal zone As Integer) As Double
      Dim retVal As Double
      Try
        retVal = DegToRad(-183.0 + (zone * 6.0))
      Catch ex As Exception
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Computes the footpoint latitude for use in converting transverse Mercator coordinates to ellipsoidal coordinates.
    ''' </summary>
    ''' <param name="y">The UTM northing coordinate, in meters</param>
    ''' <returns>The footpoint latitude, in radians</returns>
    ''' <remarks>Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J., GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994</remarks>
    Public Shared Function FootpointLatitude(ByVal y As Double) As Double
      Dim retVal As Double
      Try
        Dim alpha_ As Double, beta_ As Double, gamma_ As Double, delta_ As Double, epsilon_ As Double, n As Double
        Dim y_ As Double

        '/* Precalculate n (Eq. 10.18) */
        n = (sm_a - sm_b) / (sm_a + sm_b)

        '/* Precalculate alpha_ (Eq. 10.22) */
        '/* (Same as alpha in Eq. 10.17) */
        alpha_ = ((sm_a + sm_b) / 2.0) * (1 + (Math.Pow(n, 2.0) / 4) + (Math.Pow(n, 4.0) / 64))

        '/* Precalculate y_ (Eq. 10.23) */
        y_ = y / alpha_

        '/* Precalculate beta_ (Eq. 10.22) */
        beta_ = (3.0 * n / 2.0) + (-27.0 * Math.Pow(n, 3.0) / 32.0) + (269.0 * Math.Pow(n, 5.0) / 512.0)

        '/* Precalculate gamma_ (Eq. 10.22) */
        gamma_ = (21.0 * Math.Pow(n, 2.0) / 16.0) + (-55.0 * Math.Pow(n, 4.0) / 32.0)

        '/* Precalculate delta_ (Eq. 10.22) */
        delta_ = (151.0 * Math.Pow(n, 3.0) / 96.0) + (-417.0 * Math.Pow(n, 5.0) / 128.0)

        '/* Precalculate epsilon_ (Eq. 10.22) */
        epsilon_ = (1097.0 * Math.Pow(n, 4.0) / 512.0)

        '/* Now calculate the sum of the series (Eq. 10.21) */
        retVal = y_ + (beta_ * Math.Sin(2.0 * y_)) + (gamma_ * Math.Sin(4.0 * y_)) + _
                       (delta_ * Math.Sin(6.0 * y_)) + (epsilon_ * Math.Sin(8.0 * y_))

      Catch ex As Exception
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Converts a latitude/longitude pair to x and y coordinates in the Transverse Mercator projection.
    ''' Note that Transverse Mercator is not the same as UTM; a scale factor is required to convert between them.
    ''' </summary>
    ''' <param name="phi">Latitude of the point, in radians</param>
    ''' <param name="lambda">Longitude of the point, in radians</param>
    ''' <param name="lambda0">Longitude of the central meridian to be used, in radians</param>
    ''' <param name="xy">A 2-element array containing the x and y coordinates of the computed point</param>
    ''' <remarks>Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J., GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.</remarks>
    Public Shared Sub MapLatLonToXY(ByVal phi As Double, ByVal lambda As Double, _
                   ByVal lambda0 As Double, ByRef xy() As Double)
      Try
        Dim N As Double, nu2 As Double, ep2 As Double, t As Double, t2 As Double, l As Double
        Dim l3coef As Double, l4coef As Double, l5coef As Double, l6coef As Double, l7coef As Double, l8coef As Double
        Dim tmp As Double

        '/* Precalculate ep2 */
        ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0)) / Math.Pow(sm_b, 2.0)

        '/* Precalculate nu2 */
        nu2 = ep2 * Math.Pow(Math.Cos(phi), 2.0)

        '/* Precalculate N */
        N = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nu2))

        '/* Precalculate t */
        t = Math.Tan(phi)
        t2 = t * t
        tmp = (t2 * t2 * t2) - Math.Pow(t, 6.0)

        '/* Precalculate l */
        l = lambda - lambda0

        '/* Precalculate coefficients for l**n in the equations below
        '   so a normal human being can read the expressions for easting and northing
        '   -- l**1 and l**2 have coefficients of 1.0 */
        l3coef = 1.0 - t2 + nu2
        l4coef = 5.0 - t2 + 9 * nu2 + 4.0 * (nu2 * nu2)
        l5coef = 5.0 - 18.0 * t2 + (t2 * t2) + 14.0 * nu2 - 58.0 * t2 * nu2
        l6coef = 61.0 - 58.0 * t2 + (t2 * t2) + 270.0 * nu2 - 330.0 * t2 * nu2
        l7coef = 61.0 - 479.0 * t2 + 179.0 * (t2 * t2) - (t2 * t2 * t2)
        l8coef = 1385.0 - 3111.0 * t2 + 543.0 * (t2 * t2) - (t2 * t2 * t2)

        '/* Calculate easting (x) */
        xy(0) = N * Math.Cos(phi) * l _
           + (N / 6.0 * Math.Pow(Math.Cos(phi), 3.0) * l3coef * Math.Pow(l, 3.0)) _
           + (N / 120.0 * Math.Pow(Math.Cos(phi), 5.0) * l5coef * Math.Pow(l, 5.0)) _
           + (N / 5040.0 * Math.Pow(Math.Cos(phi), 7.0) * l7coef * Math.Pow(l, 7.0))

        '/* Calculate northing (y) */
        xy(1) = ArcLengthOfMeridian(phi) _
           + (t / 2.0 * N * Math.Pow(Math.Cos(phi), 2.0) * Math.Pow(l, 2.0)) _
           + (t / 24.0 * N * Math.Pow(Math.Cos(phi), 4.0) * l4coef * Math.Pow(l, 4.0)) _
           + (t / 720.0 * N * Math.Pow(Math.Cos(phi), 6.0) * l6coef * Math.Pow(l, 6.0)) _
           + (t / 40320.0 * N * Math.Pow(Math.Cos(phi), 8.0) * l8coef * Math.Pow(l, 8.0))

      Catch ex As Exception
      End Try
    End Sub

    ''' <summary>
    ''' Converts x and y coordinates in the Transverse Mercator projection to
    ''' a latitude/longitude pair.  Note that Transverse Mercator is not
    ''' the same as UTM; a scale factor is required to convert between them.
    ''' </summary>
    ''' <param name="x">The easting of the point, in meters.</param>
    ''' <param name="y">The northing of the point, in meters.</param>
    ''' <param name="lambda0">Longitude of the central meridian to be used, in radians.</param>
    ''' <param name="philambda">A 2-element array containing the latitude and longitude in radians</param>
    ''' <remarks>
    '''   The local variables Nf, nuf2, tf, and tf2 serve the same purpose as
    '''   N, nu2, t, and t2 in MapLatLonToXY, but they are computed with respect
    '''   to the footpoint latitude phif.
    '''   x1frac, x2frac, x2poly, x3poly, etc. are to enhance readability and
    '''   to optimize computations.
    ''' Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J., GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.</remarks>
    Public Shared Sub MapXYToLatLon(ByVal x As Double, ByVal y As Double, ByVal lambda0 As Double, _
                  ByRef philambda() As Double)
      Try
        Dim phif As Double, Nf As Double, Nfpow As Double, nuf2 As Double, ep2 As Double
        Dim tf As Double, tf2 As Double, tf4 As Double, cf As Double
        Dim x1frac As Double, x2frac As Double, x3frac As Double, x4frac As Double
        Dim x5frac As Double, x6frac As Double, x7frac As Double, x8frac As Double
        Dim x2poly As Double, x3poly As Double, x4poly As Double, x5poly As Double
        Dim x6poly As Double, x7poly As Double, x8poly As Double

        '/* Get the value of phif, the footpoint latitude. */
        phif = FootpointLatitude(y)

        '/* Precalculate ep2 */
        ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0)) / Math.Pow(sm_b, 2.0)

        '/* Precalculate cos (phif) */
        cf = Math.Cos(phif)

        '/* Precalculate nuf2 */
        nuf2 = ep2 * Math.Pow(cf, 2.0)

        '/* Precalculate Nf and initialize Nfpow */
        Nf = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nuf2))
        Nfpow = Nf

        '/* Precalculate tf */
        tf = Math.Tan(phif)
        tf2 = tf * tf
        tf4 = tf2 * tf2

        '/* Precalculate fractional coefficients for x**n in the equations
        '  below to simplify the expressions for latitude and longitude. */
        x1frac = 1.0 / (Nfpow * cf)

        Nfpow *= Nf    '/* now equals Nf**2) */
        x2frac = tf / (2.0 * Nfpow)

        Nfpow *= Nf    '/* now equals Nf**3) */
        x3frac = 1.0 / (6.0 * Nfpow * cf)

        Nfpow *= Nf    '/* now equals Nf**4) */
        x4frac = tf / (24.0 * Nfpow)

        Nfpow *= Nf    '/* now equals Nf**5) */
        x5frac = 1.0 / (120.0 * Nfpow * cf)

        Nfpow *= Nf    '/* now equals Nf**6) */
        x6frac = tf / (720.0 * Nfpow)

        Nfpow *= Nf    '/* now equals Nf**7) */
        x7frac = 1.0 / (5040.0 * Nfpow * cf)

        Nfpow *= Nf    '/* now equals Nf**8) */
        x8frac = tf / (40320.0 * Nfpow)

        '/* Precalculate polynomial coefficients for x**n.
        '  -- x**1 does not have a polynomial coefficient. */
        x2poly = -1.0 - nuf2
        x3poly = -1.0 - 2 * tf2 - nuf2
        x4poly = 5.0 + 3.0 * tf2 + 6.0 * nuf2 - 6.0 * tf2 * nuf2 - 3.0 * (nuf2 * nuf2) - 9.0 * tf2 * (nuf2 * nuf2)
        x5poly = 5.0 + 28.0 * tf2 + 24.0 * tf4 + 6.0 * nuf2 + 8.0 * tf2 * nuf2
        x6poly = -61.0 - 90.0 * tf2 - 45.0 * tf4 - 107.0 * nuf2 + 162.0 * tf2 * nuf2
        x7poly = -61.0 - 662.0 * tf2 - 1320.0 * tf4 - 720.0 * (tf4 * tf2)
        x8poly = 1385.0 + 3633.0 * tf2 + 4095.0 * tf4 + 1575 * (tf4 * tf2)

        '/* Calculate latitude */
        philambda(0) = phif + x2frac * x2poly * (x * x) + x4frac * x4poly * Math.Pow(x, 4.0) _
               + x6frac * x6poly * Math.Pow(x, 6.0) + x8frac * x8poly * Math.Pow(x, 8.0)

        '/* Calculate longitude */
        philambda(1) = lambda0 + x1frac * x + x3frac * x3poly * Math.Pow(x, 3.0) _
               + x5frac * x5poly * Math.Pow(x, 5.0) + x7frac * x7poly * Math.Pow(x, 7.0)

      Catch ex As Exception
      End Try
    End Sub

    ''' <summary>
    ''' Converts a latitude/longitude pair to x and y coordinates in the Universal Transverse Mercator projection.
    ''' </summary>
    ''' <param name="lat">Latitude of the point, in radians.</param>
    ''' <param name="lon">Longitude of the point, in radians.</param>
    ''' <param name="zone">UTM zone to be used for calculating values for x and y.
    ''' If zone is less than 1 or greater than 60, the routine will determine the appropriate zone from the value of lon.</param>
    ''' <param name="xy">A 2-element array where the UTM x and y values will be stored.</param>
    ''' <returns>The UTM zone used for calculating the values of x and y.</returns>
    ''' <remarks></remarks>
    Public Shared Function LatLonToUTMXY(ByVal lat As Double, ByVal lon As Double, ByVal zone As Integer, _
                   ByRef xy() As Double) As Integer
      Dim retVal As Integer = zone
      Try
        MapLatLonToXY(lat, lon, UTMCentralMeridian(zone), xy)

        '/* Adjust easting and northing for UTM system. */
        xy(0) = xy(0) * UTMScaleFactor + 500000.0 'x
        xy(1) = xy(1) * UTMScaleFactor 'y
        If (xy(1) < 0.0) Then xy(1) = xy(1) + 10000000.0

        retVal = zone

      Catch ex As Exception
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Converts x and y coordinates in the Universal Transverse Mercator projection to a latitude/longitude pair
    ''' </summary>
    ''' <param name="x">The easting of the point, in meters.</param>
    ''' <param name="y">The northing of the point, in meters.</param>
    ''' <param name="zone">The UTM zone in which the point lies.</param>
    ''' <param name="southhemi">True if the point is in the southern hemisphere; false otherwise.</param>
    ''' <param name="latlon">A 2-element array containing the latitude and longitude of the point, in radians.</param>
    ''' <remarks></remarks>
    Public Shared Sub UTMXYToLatLon(ByVal x As Double, ByVal y As Double, ByVal zone As Integer, _
                   ByVal southhemi As Boolean, ByRef latlon() As Double)
      Try
        Dim cmeridian As Double

        x -= 500000.0
        x /= UTMScaleFactor

        '/* If in southern hemisphere, adjust y accordingly. */
        If (southhemi) Then y -= 10000000.0
        y /= UTMScaleFactor

        cmeridian = UTMCentralMeridian(zone)
        MapXYToLatLon(x, y, cmeridian, latlon)

      Catch ex As Exception
      End Try
    End Sub

#End Region

    '#Region "Geographic/UTM Coordinate Converter"

    '    ' 8/26/11 KJA -- switched everything to x,y formatting
    '    ' Download from http://home.hiwaay.net/~taylorc/toolbox/geography/geoutm.html
    '    Const pi As Double = 3.14159265358979

    '    '/* Ellipsoid model constants (actual values here are for WGS84) */
    '    Const sm_a As Double = 6378137.0
    '    Const sm_b As Double = 6356752.314
    '    Const sm_EccSquared As Double = 0.00669437999013

    '    Const UTMScaleFactor As Double = 0.9996

    '    ' The main function to start conversion from geographic to UTM
    '    Public Shared Function GeoToUTM_Main(ByVal lon As Double, ByVal lat As Double) As Double()
    '      Dim retVal(3) As Double
    '      Try
    '        Dim xy(2) As Double

    '        ' Compute the UTM zone.
    '        Dim tempZone As Double = Math.Floor((lon + 180.0) / 6) + 1
    '        Dim zone as integer = LatLonToUTMXY(DegToRad(lon), DegToRad(lat), tempZone, xy)

    '        retVal(0) = xy(0)
    '        retVal(1) = xy(1)
    '        retVal(2) = zone

    '      Catch ex As Exception
    '      End Try
    '      Return retVal
    '    End Function

    '    ' The main function to start conversion from UTM to Geographic 
    '    Public Shared Function UTMToGeo_Main(ByVal x As Double, ByVal y As Double, _
    '                   ByVal zone as integer, ByVal southhemi As Boolean) As Double()
    '      Dim retVal(2) As Double
    '      Try
    '        UTMXYToLatLon(x, y, zone, southhemi, retVal)
    '        Dim lon As Double = RadToDeg(retVal(0))
    '        Dim lat As Double = RadToDeg(retVal(1))
    '        retVal(0) = lon
    '        retVal(1) = lat

    '      Catch ex As Exception
    '      End Try
    '      Return retVal
    '    End Function

    '    ' Converts degrees to radians.
    '    Public Shared Function DegToRad(ByVal deg As Double) As Double
    '      Dim retVal As Double = 0
    '      Try
    '        retVal = (deg / 180.0 * pi)
    '      Catch ex As Exception
    '      End Try
    '      Return retVal
    '    End Function

    '    ' Converts radians to degrees.
    '    Public Shared Function RadToDeg(ByVal rad As Double) As Double
    '      Dim retVal As Double = 0
    '      Try
    '        retVal = (rad / pi * 180.0)
    '      Catch ex As Exception
    '      End Try
    '      Return retVal
    '    End Function

    '    ''' <summary>
    '    ''' Computes the ellipsoidal distance from the equator to a point at a given latitude.
    '    ''' </summary>
    '    ''' <param name="phi">Latitude of the point, in radians</param>
    '    ''' <globals>sm_a - Ellipsoid model major axis; sm_b - Ellipsoid model minor axis.</globals>
    '    ''' <returns>The ellipsoidal distance of the point from the equator, in meters.</returns>
    '    ''' <remarks>Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J., GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.</remarks>
    '    Public Shared Function ArcLengthOfMeridian(ByVal phi As Double) As Double
    '      Dim retVal As Double
    '      Try
    '        Dim alpha As Double, beta As Double, gamma As Double, delta As Double, epsilon As Double
    '        Dim n As Double

    '        '/* Precalculate n */
    '        n = (sm_a - sm_b) / (sm_a + sm_b)

    '        '/* Precalculate alpha */
    '        alpha = ((sm_a + sm_b) / 2.0) * (1.0 + (Math.Pow(n, 2.0) / 4.0) + (Math.Pow(n, 4.0) / 64.0))

    '        '/* Precalculate beta */
    '        beta = (-3.0 * n / 2.0) + (9.0 * Math.Pow(n, 3.0) / 16.0) + (-3.0 * Math.Pow(n, 5.0) / 32.0)

    '        '/* Precalculate gamma */
    '        gamma = (15.0 * Math.Pow(n, 2.0) / 16.0) + (-15.0 * Math.Pow(n, 4.0) / 32.0)

    '        '/* Precalculate delta */
    '        delta = (-35.0 * Math.Pow(n, 3.0) / 48.0) + (105.0 * Math.Pow(n, 5.0) / 256.0)

    '        '/* Precalculate epsilon */
    '        epsilon = (315.0 * Math.Pow(n, 4.0) / 512.0)

    '        '/* Now calculate the sum of the series and return */
    '        retVal = alpha * (phi + (beta * Math.Sin(2.0 * phi)) + (gamma * Math.Sin(4.0 * phi)) + _
    '                       (delta * Math.Sin(6.0 * phi)) + (epsilon * Math.Sin(8.0 * phi)))

    '      Catch ex As Exception
    '      End Try
    '      Return retVal
    '    End Function

    '    ''' <summary>
    '    ''' Determines the central meridian for the given UTM zone.
    '    ''' </summary>
    '    ''' <param name="zone">An integer value designating the UTM zone, range [1,60]</param>
    '    ''' <returns>
    '    '''   The central meridian for the given UTM zone, in radians, or zero
    '    '''   if the UTM zone parameter is outside the range [1,60].
    '    '''   Range of the central meridian is the radian equivalent of [-177,+177]. </returns>
    '    ''' <remarks></remarks>
    '    Public Shared Function UTMCentralMeridian(ByVal zone as integer) As Double
    '      Dim retVal As Double
    '      Try
    '        retVal = DegToRad(-183.0 + (zone * 6.0))
    '      Catch ex As Exception
    '      End Try
    '      Return retVal
    '    End Function

    '    ''' <summary>
    '    ''' Computes the footpoint latitude for use in converting transverse Mercator coordinates to ellipsoidal coordinates.
    '    ''' </summary>
    '    ''' <param name="y">The UTM northing coordinate, in meters</param>
    '    ''' <returns>The footpoint latitude, in radians</returns>
    '    ''' <remarks>Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J., GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994</remarks>
    '    Public Shared Function FootpointLatitude(ByVal y As Double) As Double
    '      Dim retVal As Double
    '      Try
    '        Dim alpha_ As Double, beta_ As Double, gamma_ As Double, delta_ As Double, epsilon_ As Double, n As Double
    '        Dim y_ As Double

    '        '/* Precalculate n (Eq. 10.18) */
    '        n = (sm_a - sm_b) / (sm_a + sm_b)

    '        '/* Precalculate alpha_ (Eq. 10.22) */
    '        '/* (Same as alpha in Eq. 10.17) */
    '        alpha_ = ((sm_a + sm_b) / 2.0) * (1 + (Math.Pow(n, 2.0) / 4) + (Math.Pow(n, 4.0) / 64))

    '        '/* Precalculate y_ (Eq. 10.23) */
    '        y_ = y / alpha_

    '        '/* Precalculate beta_ (Eq. 10.22) */
    '        beta_ = (3.0 * n / 2.0) + (-27.0 * Math.Pow(n, 3.0) / 32.0) + (269.0 * Math.Pow(n, 5.0) / 512.0)

    '        '/* Precalculate gamma_ (Eq. 10.22) */
    '        gamma_ = (21.0 * Math.Pow(n, 2.0) / 16.0) + (-55.0 * Math.Pow(n, 4.0) / 32.0)

    '        '/* Precalculate delta_ (Eq. 10.22) */
    '        delta_ = (151.0 * Math.Pow(n, 3.0) / 96.0) + (-417.0 * Math.Pow(n, 5.0) / 128.0)

    '        '/* Precalculate epsilon_ (Eq. 10.22) */
    '        epsilon_ = (1097.0 * Math.Pow(n, 4.0) / 512.0)

    '        '/* Now calculate the sum of the series (Eq. 10.21) */
    '        retVal = y_ + (beta_ * Math.Sin(2.0 * y_)) + (gamma_ * Math.Sin(4.0 * y_)) + _
    '                       (delta_ * Math.Sin(6.0 * y_)) + (epsilon_ * Math.Sin(8.0 * y_))

    '      Catch ex As Exception
    '      End Try
    '      Return retVal
    '    End Function

    '    ''' <summary>
    '    ''' Converts a latitude/longitude pair to x and y coordinates in the Transverse Mercator projection.
    '    ''' Note that Transverse Mercator is not the same as UTM; a scale factor is required to convert between them.
    '    ''' </summary>
    '    ''' <param name="phi">Latitude of the point, in radians</param>
    '    ''' <param name="lambda">Longitude of the point, in radians</param>
    '    ''' <param name="lambda0">Longitude of the central meridian to be used, in radians</param>
    '    ''' <param name="xy">A 2-element array containing the x and y coordinates of the computed point</param>
    '    ''' <remarks>Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J., GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.</remarks>
    '    Public Shared Sub MapLatLonToXY(ByVal phi As Double, ByVal lambda As Double, _
    '                   ByVal lambda0 As Double, ByRef xy() As Double)
    '      Try
    '        Dim N As Double, nu2 As Double, ep2 As Double, t As Double, t2 As Double, l As Double
    '        Dim l3coef As Double, l4coef As Double, l5coef As Double, l6coef As Double, l7coef As Double, l8coef As Double
    '        Dim tmp As Double

    '        '/* Precalculate ep2 */
    '        ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0)) / Math.Pow(sm_b, 2.0)

    '        '/* Precalculate nu2 */
    '        nu2 = ep2 * Math.Pow(Math.Cos(phi), 2.0)

    '        '/* Precalculate N */
    '        N = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nu2))

    '        '/* Precalculate t */
    '        t = Math.Tan(phi)
    '        t2 = t * t
    '        tmp = (t2 * t2 * t2) - Math.Pow(t, 6.0)

    '        '/* Precalculate l */
    '        l = lambda - lambda0

    '        '/* Precalculate coefficients for l**n in the equations below
    '        '   so a normal human being can read the expressions for easting and northing
    '        '   -- l**1 and l**2 have coefficients of 1.0 */
    '        l3coef = 1.0 - t2 + nu2
    '        l4coef = 5.0 - t2 + 9 * nu2 + 4.0 * (nu2 * nu2)
    '        l5coef = 5.0 - 18.0 * t2 + (t2 * t2) + 14.0 * nu2 - 58.0 * t2 * nu2
    '        l6coef = 61.0 - 58.0 * t2 + (t2 * t2) + 270.0 * nu2 - 330.0 * t2 * nu2
    '        l7coef = 61.0 - 479.0 * t2 + 179.0 * (t2 * t2) - (t2 * t2 * t2)
    '        l8coef = 1385.0 - 3111.0 * t2 + 543.0 * (t2 * t2) - (t2 * t2 * t2)

    '        '/* Calculate easting (x) */
    '        xy(0) = N * Math.Cos(phi) * l _
    '           + (N / 6.0 * Math.Pow(Math.Cos(phi), 3.0) * l3coef * Math.Pow(l, 3.0)) _
    '           + (N / 120.0 * Math.Pow(Math.Cos(phi), 5.0) * l5coef * Math.Pow(l, 5.0)) _
    '           + (N / 5040.0 * Math.Pow(Math.Cos(phi), 7.0) * l7coef * Math.Pow(l, 7.0))

    '        '/* Calculate northing (y) */
    '        xy(1) = ArcLengthOfMeridian(phi) _
    '           + (t / 2.0 * N * Math.Pow(Math.Cos(phi), 2.0) * Math.Pow(l, 2.0)) _
    '           + (t / 24.0 * N * Math.Pow(Math.Cos(phi), 4.0) * l4coef * Math.Pow(l, 4.0)) _
    '           + (t / 720.0 * N * Math.Pow(Math.Cos(phi), 6.0) * l6coef * Math.Pow(l, 6.0)) _
    '           + (t / 40320.0 * N * Math.Pow(Math.Cos(phi), 8.0) * l8coef * Math.Pow(l, 8.0))

    '      Catch ex As Exception
    '      End Try
    '    End Sub

    '    ''' <summary>
    '    ''' Converts x and y coordinates in the Transverse Mercator projection to
    '    ''' a latitude/longitude pair.  Note that Transverse Mercator is not
    '    ''' the same as UTM; a scale factor is required to convert between them.
    '    ''' </summary>
    '    ''' <param name="x">The easting of the point, in meters.</param>
    '    ''' <param name="y">The northing of the point, in meters.</param>
    '    ''' <param name="lambda0">Longitude of the central meridian to be used, in radians.</param>
    '    ''' <param name="philambda">A 2-element containing the latitude and longitude in radians</param>
    '    ''' <remarks>
    '    '''   The local variables Nf, nuf2, tf, and tf2 serve the same purpose as
    '    '''   N, nu2, t, and t2 in MapLatLonToXY, but they are computed with respect
    '    '''   to the footpoint latitude phif.
    '    '''   x1frac, x2frac, x2poly, x3poly, etc. are to enhance readability and
    '    '''   to optimize computations.
    '    ''' Reference: Hoffmann-Wellenhof, B., Lichtenegger, H., and Collins, J., GPS: Theory and Practice, 3rd ed.  New York: Springer-Verlag Wien, 1994.</remarks>
    '    Public Shared Sub MapXYToLatLon(ByVal x As Double, ByVal y As Double, ByVal lambda0 As Double, _
    '                   ByRef philambda() As Double)
    '      Try
    '        Dim phif As Double, Nf As Double, Nfpow As Double, nuf2 As Double, ep2 As Double
    '        Dim tf As Double, tf2 As Double, tf4 As Double, cf As Double
    '        Dim x1frac As Double, x2frac As Double, x3frac As Double, x4frac As Double
    '        Dim x5frac As Double, x6frac As Double, x7frac As Double, x8frac As Double
    '        Dim x2poly As Double, x3poly As Double, x4poly As Double, x5poly As Double
    '        Dim x6poly As Double, x7poly As Double, x8poly As Double

    '        '/* Get the value of phif, the footpoint latitude. */
    '        phif = FootpointLatitude(y)

    '        '/* Precalculate ep2 */
    '        ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0)) / Math.Pow(sm_b, 2.0)

    '        '/* Precalculate cos (phif) */
    '        cf = Math.Cos(phif)

    '        '/* Precalculate nuf2 */
    '        nuf2 = ep2 * Math.Pow(cf, 2.0)

    '        '/* Precalculate Nf and initialize Nfpow */
    '        Nf = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nuf2))
    '        Nfpow = Nf

    '        '/* Precalculate tf */
    '        tf = Math.Tan(phif)
    '        tf2 = tf * tf
    '        tf4 = tf2 * tf2

    '        '/* Precalculate fractional coefficients for x**n in the equations
    '        '  below to simplify the expressions for latitude and longitude. */
    '        x1frac = 1.0 / (Nfpow * cf)

    '        Nfpow *= Nf    '/* now equals Nf**2) */
    '        x2frac = tf / (2.0 * Nfpow)

    '        Nfpow *= Nf    '/* now equals Nf**3) */
    '        x3frac = 1.0 / (6.0 * Nfpow * cf)

    '        Nfpow *= Nf    '/* now equals Nf**4) */
    '        x4frac = tf / (24.0 * Nfpow)

    '        Nfpow *= Nf    '/* now equals Nf**5) */
    '        x5frac = 1.0 / (120.0 * Nfpow * cf)

    '        Nfpow *= Nf    '/* now equals Nf**6) */
    '        x6frac = tf / (720.0 * Nfpow)

    '        Nfpow *= Nf    '/* now equals Nf**7) */
    '        x7frac = 1.0 / (5040.0 * Nfpow * cf)

    '        Nfpow *= Nf    '/* now equals Nf**8) */
    '        x8frac = tf / (40320.0 * Nfpow)

    '        '/* Precalculate polynomial coefficients for x**n.
    '        '  -- x**1 does not have a polynomial coefficient. */
    '        x2poly = -1.0 - nuf2
    '        x3poly = -1.0 - 2 * tf2 - nuf2
    '        x4poly = 5.0 + 3.0 * tf2 + 6.0 * nuf2 - 6.0 * tf2 * nuf2 - 3.0 * (nuf2 * nuf2) - 9.0 * tf2 * (nuf2 * nuf2)
    '        x5poly = 5.0 + 28.0 * tf2 + 24.0 * tf4 + 6.0 * nuf2 + 8.0 * tf2 * nuf2
    '        x6poly = -61.0 - 90.0 * tf2 - 45.0 * tf4 - 107.0 * nuf2 + 162.0 * tf2 * nuf2
    '        x7poly = -61.0 - 662.0 * tf2 - 1320.0 * tf4 - 720.0 * (tf4 * tf2)
    '        x8poly = 1385.0 + 3633.0 * tf2 + 4095.0 * tf4 + 1575 * (tf4 * tf2)

    '        '/* Calculate longitude */
    '        philambda(0) = lambda0 + x1frac * x + x3frac * x3poly * Math.Pow(x, 3.0) _
    '               + x5frac * x5poly * Math.Pow(x, 5.0) + x7frac * x7poly * Math.Pow(x, 7.0)

    '        '/* Calculate latitude */
    '        philambda(1) = phif + x2frac * x2poly * (x * x) + x4frac * x4poly * Math.Pow(x, 4.0) _
    '               + x6frac * x6poly * Math.Pow(x, 6.0) + x8frac * x8poly * Math.Pow(x, 8.0)

    '      Catch ex As Exception
    '      End Try
    '    End Sub

    '    '* LatLonToUTMXY
    '    '*
    '    '* 
    '    '*.
    '    '*
    '    '* Inputs:
    '    '*   lat - 
    '    '*   lon - 
    '    '*   zone -
    '    '*          
    '    '*         
    '    '*
    '    '* Outputs:
    '    '*   xy - 
    '    '*
    '    '* Returns:
    '    '*   
    '    ''' <summary>
    '    ''' Converts a latitude/longitude pair to x and y coordinates in the Universal Transverse Mercator projection.
    '    ''' </summary>
    '    ''' <param name="lat">Latitude of the point, in radians.</param>
    '    ''' <param name="lon">Longitude of the point, in radians.</param>
    '    ''' <param name="zone">UTM zone to be used for calculating values for x and y.
    '    ''' If zone is less than 1 or greater than 60, the routine will determine the appropriate zone from the value of lon.</param>
    '    ''' <param name="xy">A 2-element array where the UTM x and y values will be stored.</param>
    '    ''' <returns>The UTM zone used for calculating the values of x and y.</returns>
    '    ''' <remarks></remarks>
    '    Public Shared Function LatLonToUTMXY(ByVal lat As Double, ByVal lon As Double, ByVal zone as integer, _
    '                   ByRef xy() As Double) As Double
    '      Dim retVal As Double = zone
    '      Try
    '        MapLatLonToXY(lat, lon, UTMCentralMeridian(zone), xy)

    '        '/* Adjust easting and northing for UTM system. */
    '        xy(0) = xy(0) * UTMScaleFactor + 500000.0
    '        xy(1) = xy(1) * UTMScaleFactor
    '        If (xy(1) < 0.0) Then xy(1) = xy(1) + 10000000.0

    '        retVal = zone

    '      Catch ex As Exception
    '      End Try
    '      Return retVal
    '    End Function

    '    '* UTMXYToLatLon
    '    '*
    '    '* Converts x and y coordinates in the Universal Transverse Mercator
    '    '* projection to a latitude/longitude pair.
    '    '*
    '    '* Inputs:
    '    '*   x - The easting of the point, in meters.
    '    '*   y - The northing of the point, in meters.
    '    '*   zone - The UTM zone in which the point lies.
    '    '*   southhemi - True if the point is in the southern hemisphere;
    '    '*               false otherwise.
    '    '*
    '    '* Outputs:
    '    '*   latlon - A 2-element array containing the latitude and
    '    '*            longitude of the point, in radians.
    '    '*
    '    '* Returns:
    '    '*   The function does not return a value. 
    '    '//    function UTMXYToLatLon(x, y, zone, southhemi, latlon) {
    '    '//       var cmeridian;

    '    '//       x -= 500000.0;
    '    '//       x /= UTMScaleFactor;

    '    '//       /* If in southern hemisphere, adjust y accordingly. */
    '    '//       if (southhemi)
    '    '//          y -= 10000000.0;

    '    '//       y /= UTMScaleFactor;

    '    '//       cmeridian = UTMCentralMeridian(zone);
    '    '//       MapXYToLatLon(x, y, cmeridian, latlon);

    '    '//       return;
    '    '//    }
    '    Public Shared Sub UTMXYToLatLon(ByVal x As Double, ByVal y As Double, ByVal zone as integer, _
    '                   ByVal southhemi As Boolean, ByRef latlon() As Double)
    '      Try
    '        Dim cmeridian As Double

    '        x -= 500000.0
    '        x /= UTMScaleFactor

    '        '/* If in southern hemisphere, adjust y accordingly. */
    '        If (southhemi) Then y -= 10000000.0
    '        y /= UTMScaleFactor

    '        cmeridian = UTMCentralMeridian(zone)
    '        MapXYToLatLon(x, y, cmeridian, latlon)

    '      Catch ex As Exception
    '      End Try
    '    End Sub

    '#End Region

    Private Shared Function MethodIdentifier() As String
      'Used for error message attributes (title)
      Try
        Return CF.FormatMethodIdentifier(System.Reflection.MethodBase.GetCurrentMethod.DeclaringType.Name, New System.Diagnostics.StackFrame(1).GetMethod().Name)
      Catch ex As Exception
        Return "GIS Tools Addl MethodIdentifier didn't work"
      End Try
    End Function

    Private Shared Function FormatErrorMessage(inEx As Exception) As String
      'Used for error message attributes
      Try
        Dim CurrentStack As New System.Diagnostics.StackTrace(inEx, True)
        Dim fln As Integer = CurrentStack.GetFrame(CurrentStack.GetFrames().Length - 1).GetFileLineNumber()
        Return String.Format("(line {0}) {1}", fln.ToString(), inEx.Message)
      Catch ex As Exception
        Return "FormatErrorMessage didn't work"
      End Try
    End Function

  End Class

End Namespace