Option Strict On

#Region "Imports"

Imports CF = CommonFunctions
Imports CV = CommonVariables
Imports MDL = TerLoc.Model
Imports EH = ErrorHandler
Imports APP = Application
Imports GTA = GIS.GISToolsAddl

#End Region
'Imports <xmlns="http://www.landxml.org/schema/LandXML-1.2">
'Imports <xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

Namespace LandXML

  Partial Public Class LandXML

#Region "Module declarations"
    Private fileName As String = "" ' full path of file to be saved
    Private projectId As Long = Long.MinValue

    Private Shared debugInfo As String = ""
    Private Shared errorInfo As String = ""

    Private Shared ns As XNamespace = "http://www.landxml.org/schema/LandXML-1.2"
    Private Shared xsi As XNamespace = "http://www.w3.org/2001/XMLSchema-instance"
#End Region

    Public Sub New(ByVal projectId As Long, ByVal fullFileName As String)
      Me.fileName = fullFileName
      Me.projectId = projectId
    End Sub

    ''' <summary>
    ''' Create LandXML file.
    ''' </summary>
    Public Sub MakeXml()
      Dim localInfo As String = ""
      Try

        'TODO: make sure UNITS are correct !!!

        Dim units As XElement = MakeUnitsXml()
        Dim surfaces As XElement = MakeSurfacesXml()
        Dim terraceArea As XElement = MakeSurfaceXml()
        'Dim alignments As XElement = MakeAlignmentsXml()
        Dim planFeatures As List(Of XElement) = MakePlanFeaturesXmlList()
        Dim project As XElement = MakeProjectXml()
        Dim application As XElement = MakeApplicationXml()

        Dim landXml As New XElement(ns + "LandXML" _
          , New XAttribute("date", Now.ToString("yyyy-MM-dd")) _
          , New XAttribute("time", Now.ToString("hh:mm:ss")) _
          , New XAttribute("version", "1.2") _
          , New XAttribute("language", "English") _
          , New XAttribute("readOnly", "false") _
          , New XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName) _
          , New XAttribute(xsi + "schemaLocation", ns.NamespaceName + " http://www.landxml.org/schema/LandXML-1.2/LandXML-1.2.xsd") _
          , units _
          , surfaces _
          , project _
          , application _
          )
        For Each planFeature As XElement In planFeatures
          landXml.Add(planFeature)
        Next

        landXml.Save(fileName)

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString, Nothing)
      End Try
    End Sub

    ''' <summary>
    ''' Create XML element for each set of plan features (divides, waterways, ridgelines).
    ''' </summary>
    Private Function MakePlanFeaturesXmlList() As List(Of XElement)
      Dim retVal As New List(Of XElement)
      Dim localInfo As String = ""
      Try
        retVal = MakePlanFeaturesXml()
      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for plan features (divides, waterways, ridgelines).
    ''' </summary>
    Private Function MakePlanFeaturesXml() As List(Of XElement)
      Dim retVal As New List(Of XElement)
      Dim xelemName As String = "PlanFeatures"
      Dim localInfo As String = ""
      Try
        Dim highpointHelper As New MDL.HighPointHelper
        Dim highpointFeatures As MDL.HighPointPackageList = highpointHelper.Fetch(projectId, localInfo)

        For featIx As Integer = 0 To highpointFeatures.highPoints.Count - 1
          Dim feature As MDL.HighPointPackage = highpointFeatures.highPoints(featIx)
          Dim featureCoords As String = feature.highPointRecord.Latitude & " " & feature.highPointRecord.Longitude
          Dim highpointElement As New XElement(ns + "Location")
          highpointElement.Add( _
             featureCoords _
            )

          'Dim highpointInfo As New XElement(ns + "Feature")
          'highpointInfo.Add(New XAttribute("code", "Highpoint"))

          Dim planfeature As New XElement(ns + "PlanFeature")
          planfeature.Add( _
             New XAttribute("name", "Highpoint") _
            , New XAttribute("desc", "Maximum elevation for terrace area") _
            , highpointElement _
            )

          Dim highpointPlanFeatures As New XElement(ns + xelemName)
          highpointPlanFeatures.Add(planfeature)
          retVal.Add(highpointPlanFeatures)
        Next

        Dim ridgelines As New List(Of XElement)
        Dim ridgelineHelper As New MDL.RidgelineHelper
        Dim ridgelineFeatures As MDL.RidgelinePackageList = ridgelineHelper.Fetch(projectId, localInfo)

        For featIx As Integer = 0 To ridgelineFeatures.ridgelines.Count - 1
          Dim feature As MDL.RidgelinePackage = ridgelineFeatures.ridgelines(featIx)
          Dim featureCoords As String = feature.ridgelineRecord.Coords
          Dim points As String() = featureCoords.Split(CChar(CV.PointSplitter))
          Dim pointsLen As Integer = points.Count
          Dim first As New Point()
          With first
            .northing = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(0))
            .easting = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(1))
          End With
          Dim last As New Point()
          With last
            .northing = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(0))
            .easting = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(1))
          End With
          Dim line As New IrregularLine
          With line
            .StartPoint = first
            .EndPoint = last
            .PntList = featureCoords.Replace(CV.CoordinateSplitter, " ").Replace(CV.PointSplitter, " ").Trim
          End With
          'Dim length As Double = GTA.GetLengthFromLatLngLinestring(GTA.CreateLineStringFromCoordString(featureCoords, Nothing), Nothing)
          Dim featureXml As XElement = MakePlanFeatureXml(line, "Ridgeline ", "Ridgeline feature")
          If featureXml IsNot Nothing Then ridgelines.Add(featureXml)
        Next
        If ridgelines.Count > 0 Then
          Dim ridgelinesPlanFeatures As New XElement(ns + xelemName)
          For Each ridgelinePlanFeature As XElement In ridgelines
            ridgelinesPlanFeatures.Add(ridgelinePlanFeature)
          Next
          retVal.Add(ridgelinesPlanFeatures)
        End If

        Dim divides As New List(Of XElement)
        Dim divideHelper As New MDL.DivideHelper
        Dim divideFeatures As MDL.DividePackageList = divideHelper.Fetch(projectId, localInfo)

        For featIx As Integer = 0 To divideFeatures.divides.Count - 1
          Dim feature As MDL.DividePackage = divideFeatures.divides(featIx)
          Dim featureCoords As String = feature.divideRecord.Coords
          Dim points As String() = featureCoords.Split(CChar(CV.PointSplitter))
          Dim pointsLen As Integer = points.Count
          Dim first As New Point()
          With first
            .northing = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(0))
            .easting = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(1))
          End With
          Dim last As New Point()
          With last
            .northing = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(0))
            .easting = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(1))
          End With
          Dim line As New IrregularLine
          With line
            .StartPoint = first
            .EndPoint = last
            .PntList = featureCoords.Replace(CV.CoordinateSplitter, " ").Replace(CV.PointSplitter, " ").Trim
          End With
          'Dim length As Double = GTA.GetLengthFromLatLngLinestring(GTA.CreateLineStringFromCoordString(featureCoords, Nothing), Nothing)
          Dim featureXml As XElement = MakePlanFeatureXml(line, "Divide " & feature.divideRecord.Ordinal, "Divide feature")
          If featureXml IsNot Nothing Then divides.Add(featureXml)
        Next
        If divides.Count > 0 Then
          Dim dividesPlanFeatures As New XElement(ns + xelemName)
          For Each dividePlanFeature As XElement In divides
            dividesPlanFeatures.Add(dividePlanFeature)
          Next
          retVal.Add(dividesPlanFeatures)
        End If

        Dim waterways As New List(Of XElement)
        Dim waterwayHelper As New MDL.WaterwayHelper
        Dim waterwayFeatures As MDL.WaterwayPackageList = waterwayHelper.Fetch(projectId, localInfo)

        For featIx As Integer = 0 To waterwayFeatures.waterways.Count - 1
          Dim feature As MDL.WaterwayPackage = waterwayFeatures.waterways(featIx)
          Dim featureCoords As String = feature.waterwayRecord.Coords
          Dim points As String() = featureCoords.Split(CChar(CV.PointSplitter))
          Dim pointsLen As Integer = points.Count
          Dim first As New Point()
          With first
            .northing = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(0))
            .easting = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(1))
          End With
          Dim last As New Point()
          With last
            .northing = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(0))
            .easting = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(1))
          End With
          Dim line As New IrregularLine
          With line
            .StartPoint = first
            .EndPoint = last
            .PntList = featureCoords.Replace(CV.CoordinateSplitter, " ").Replace(CV.PointSplitter, " ").Trim
          End With
          'Dim length As Double = GTA.GetLengthFromLatLngLinestring(GTA.CreateLineStringFromCoordString(featureCoords, Nothing), Nothing)
          Dim featureXml As XElement = MakePlanFeatureXml(line, "Waterway " & feature.waterwayRecord.Ordinal, "Waterway feature")
          If featureXml IsNot Nothing Then waterways.Add(featureXml)
        Next
        If waterways.Count > 0 Then
          Dim waterwaysPlanFeatures As New XElement(ns + xelemName)
          For Each waterwayPlanFeature As XElement In waterways
            waterwaysPlanFeatures.Add(waterwayPlanFeature)
          Next
          retVal.Add(waterwaysPlanFeatures)
        End If

        Dim terraces As New List(Of XElement)
        Dim terraceHelper As New MDL.TerraceHelper
        Dim terraceFeatures As MDL.TerracePackageList = terraceHelper.Fetch(projectId, localInfo)

        Dim terraceTypesRegular() As String = {"Original", "Smooth", "Filled"}
        Dim terraceTypeKey As String = "Key Terrace"
        Dim typeTerraces As List(Of MDL.TerracePackage)

        For Each terraceType As String In terraceTypesRegular
          typeTerraces = terraceFeatures.terraces.Where(Function(t) t.terraceRecord.Type = terraceType).ToList
          terraces = New List(Of XElement)
          For Each terracePkg As MDL.TerracePackage In typeTerraces
            Dim featureCoords As String = terracePkg.terraceRecord.Coords
            Dim points As String() = featureCoords.Split(CChar(CV.PointSplitter))
            Dim pointsLen As Integer = points.Count
            Dim first As New Point()
            With first
              .northing = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(0))
              .easting = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(1))
            End With
            Dim last As New Point()
            With last
              .northing = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(0))
              .easting = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(1))
            End With
            Dim line As New IrregularLine
            With line
              .StartPoint = first
              .EndPoint = last
              .PntList = featureCoords.Replace(CV.CoordinateSplitter, " ").Replace(CV.PointSplitter, " ").Trim
            End With
            'Dim length As Double = GTA.GetLengthFromLatLngLinestring(GTA.CreateLineStringFromCoordString(featureCoords, Nothing), Nothing)
            Dim featureXml As XElement = MakePlanFeatureXml(line, "Terrace " & terraceType & " " & terracePkg.terraceRecord.Ordinal, "Terrace feature")
            If featureXml IsNot Nothing Then
              If terracePkg.terraceRecord.Custom = True Then
                featureXml.Add(New Feature("Custom", "true").ToXml)
              End If
              terraces.Add(featureXml)
            End If
          Next
          If terraces.Count > 0 Then
            Dim terracesPlanFeatures As New XElement(ns + xelemName)
            For Each terracePlanFeature As XElement In terraces
              terracesPlanFeatures.Add(terracePlanFeature)
            Next
            retVal.Add(terracesPlanFeatures)
          End If
        Next
        For keyTerraceIx As Integer = 1 To 20 'assume max number of key terraces created
          Dim keyIx As Integer = keyTerraceIx
          typeTerraces = terraceFeatures.terraces.Where(Function(t) t.terraceRecord.Type = terraceTypeKey & " " & keyIx.ToString).ToList
          terraces = New List(Of XElement)
          For Each terracePkg As MDL.TerracePackage In typeTerraces
            Dim featureCoords As String = terracePkg.terraceRecord.Coords
            Dim points As String() = featureCoords.Split(CChar(CV.PointSplitter))
            Dim pointsLen As Integer = points.Count
            Dim first As New Point()
            With first
              .northing = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(0))
              .easting = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(1))
            End With
            Dim last As New Point()
            With last
              .northing = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(0))
              .easting = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(1))
            End With
            Dim line As New IrregularLine
            With line
              .StartPoint = first
              .EndPoint = last
              .PntList = featureCoords.Replace(CV.CoordinateSplitter, " ").Replace(CV.PointSplitter, " ").Trim
            End With
            'Dim length As Double = GTA.GetLengthFromLatLngLinestring(GTA.CreateLineStringFromCoordString(featureCoords, Nothing), Nothing)
            Dim featureXml As XElement = MakePlanFeatureXml(line, terraceTypeKey & " " & keyTerraceIx & " " & terracePkg.terraceRecord.Ordinal, "Terrace feature")
            If featureXml IsNot Nothing Then
              If terracePkg.terraceRecord.Custom = True Then
                featureXml.Add(New Feature("Custom", "true").ToXml)
              End If
              terraces.Add(featureXml)
            End If
          Next
          If terraces.Count > 0 Then
            Dim terracesPlanFeatures As New XElement(ns + xelemName)
            For Each terracePlanFeature As XElement In terraces
              terracesPlanFeatures.Add(terracePlanFeature)
            Next
            retVal.Add(terracesPlanFeatures)
          End If
        Next
        'If terraces.Count > 0 Then
        '  Dim terracesPlanFeatures As New XElement(ns + xelemName)
        '  For Each terracePlanFeature As XElement In terraces
        '    terracesPlanFeatures.Add(terracePlanFeature)
        '  Next
        '  retVal.Add(terracesPlanFeatures)
        'End If

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for a plan feature.
    ''' </summary>
    Private Function MakePlanFeatureXml(ByVal line As IrregularLine, ByVal name As String, ByVal description As String) As XElement
      Dim xelemName As String = "PlanFeature"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim coordGeom As XElement = MakeCoordGeomXml(line)

        retVal.Add( _
           New XAttribute("name", name) _
          , New XAttribute("desc", description) _
          , coordGeom _
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for alignments (divides, waterways, ridgelines).
    ''' </summary>
    Private Function MakeAlignmentsXml() As XElement
      Dim xelemName As String = "Alignments"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim helperDivide As New MDL.DivideHelper
        Dim features As MDL.DividePackageList = helperDivide.Fetch(projectId, localInfo)

        For featIx As Integer = 0 To features.divides.Count - 1
          Dim feature As MDL.DividePackage = features.divides(featIx)
          Dim featureCoords As String = feature.divideRecord.Coords
          Dim points As String() = featureCoords.Split(CChar(CV.PointSplitter))
          Dim pointsLen As Integer = points.Count
          Dim first As New Point()
          With first
            .northing = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(0))
            .easting = CDbl(points(0).Split(CChar(CV.CoordinateSplitter))(1))
          End With
          Dim last As New Point()
          With last
            .northing = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(0))
            .easting = CDbl(points(pointsLen - 1).Split(CChar(CV.CoordinateSplitter))(1))
          End With
          Dim line As New IrregularLine
          With line
            .StartPoint = first
            .EndPoint = last
            .PntList = featureCoords.Replace(CV.CoordinateSplitter, " ").Replace(CV.PointSplitter, " ").Trim
          End With
          Dim length As Double = GTA.GetLengthFromLatLngLinestring(GTA.CreateLineStringFromCoordString(featureCoords, Nothing), Nothing)
          Dim featureXml As XElement = MakeAlignmentXml(line, "Divide " & featIx, length)
          If featureXml IsNot Nothing Then retVal.Add(featureXml)
        Next

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for an alignment.
    ''' </summary>
    Private Function MakeAlignmentXml(ByVal line As IrregularLine, ByVal name As String, ByVal length As Double) As XElement
      Dim xelemName As String = "Alignment"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim coordGeom As XElement = MakeCoordGeomXml(line)

        '<xs:element name="Alignment">
        '  <xs:annotation>
        '    <xs:documentation>geometric horizontal alignment, PGL or chain typically representing a road design center line</xs:documentation>
        '  </xs:annotation>
        '  <xs:complexType>
        '    <xs:choice maxOccurs="unbounded">
        '      <xs:choice>
        '        <xs:element ref="Start" minOccurs="0"/>
        '        <xs:element ref="CoordGeom"/>
        '        <xs:element ref="AlignPIs" minOccurs="0"/>
        '        <xs:element ref="Cant" minOccurs="0"/>
        '      </xs:choice>
        '      <xs:element ref="StaEquation" minOccurs="0" maxOccurs="unbounded"/>
        '      <xs:element ref="Profile" minOccurs="0" maxOccurs="unbounded"/>
        '      <xs:element ref="CrossSects" minOccurs="0"/>
        '      <xs:element ref="Superelevation" minOccurs="0" maxOccurs="unbounded"/>
        '      <xs:element ref="Feature" minOccurs="0" maxOccurs="unbounded"/>
        '    </xs:choice>
        '    <xs:attribute name="name" type="xs:string" use="required"/>
        '    <xs:attribute name="length" type="xs:double" use="required"/>
        '    <xs:attribute name="staStart" type="xs:double" use="required"/>
        '    <xs:attribute name="desc" type="xs:string"/>
        '    <xs:attribute name="oID" type="xs:string"/>
        '    <xs:attribute name="state" type="stateType"/>
        '  </xs:complexType>
        '</xs:element>

        retVal.Add( _
           New XAttribute("name", name) _
          , New XAttribute("length", length) _
          , New XAttribute("staStart", "0.0") _
          , coordGeom _
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for a coordinate geometry.
    ''' </summary>
    Private Function MakeCoordGeomXml(ByVal line As IrregularLine) As XElement
      Dim xelemName As String = "CoordGeom"
      Dim retVal As New XElement(ns + xelemName)
      Try
        Dim lineElem As XElement = line.ToXml

        retVal.Add( _
           lineElem _
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for surfaces.
    ''' </summary>
    Private Function MakeSurfacesXml() As XElement
      Dim xelemName As String = "Surfaces"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim surface As XElement = MakeSurfaceXml()
        Dim contours As XElement = MakeContoursSurfaceXml()

        retVal.Add( _
           surface _
           , contours _
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for contours surface.
    ''' </summary>
    Private Function MakeContoursSurfaceXml() As XElement
      Dim xelemName As String = "Surface"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim sourceData As XElement = MakeContoursSourceDataXml()

        retVal.Add( _
          New XAttribute("name", "Contours") _
          , New XAttribute("desc", "Smoothed contours") _
          , New XAttribute("OID", 0.ToString) _
          , sourceData
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for the contours source data.
    ''' </summary>
    Private Function MakeContoursSourceDataXml() As XElement
      Dim xelemName As String = "SourceData"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim contours As XElement = MakeContoursXml()

        retVal.Add( _
          contours
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for contours.
    ''' </summary>
    Private Function MakeContoursXml() As XElement
      Dim xelemName As String = "Contours"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim helper As New MDL.ContourHelper
        Dim features As MDL.ContourPackageList = helper.Fetch(projectId, localInfo)
        'Dim featuresXmls As New List(Of XElement)
        For Each feature As MDL.ContourPackage In features.contours
          Dim featureXml As XElement = MakeContourXml(feature)
          'If featureXml IsNot Nothing Then featuresXmls.Add(featureXml)
          If featureXml IsNot Nothing Then retVal.Add(featureXml)
        Next

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for a contour.
    ''' </summary>
    Private Function MakeContourXml(ByVal contour As MDL.ContourPackage) As XElement
      Dim xelemName As String = "Contour"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim points As New XElement(ns + "PntList2D" _
          , MakeLandXmlPoints(contour.contourRecord.Coords) _
          )

        '<xs:element ref="PntList2D"/>
        '<xs:attribute name="elev" type="xs:double" use="required"/>
        retVal.Add( _
           New XAttribute("elev", contour.contourRecord.Contour) _
          , points _
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for a surface.
    ''' </summary>
    Private Function MakeSurfaceXml() As XElement
      Dim xelemName As String = "Surface"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim fieldHelp As New MDL.FieldHelper
        Dim allFields As MDL.FieldPackageList = fieldHelp.GetFields(projectId, localInfo)
        Dim field As MDL.FieldRecord = allFields.fields(0).fieldRecord

        Dim sourceData As XElement = MakeSourceDataXml()

        '<xs:attribute name="name" type="xs:string" use="required"/>
        '<xs:attribute name="desc" type="xs:string"/>
        '<xs:attribute name="OID" type="xs:string"/>
        '<xs:attribute name="state" type="stateType"/>
        retVal.Add( _
          New XAttribute("name", "Terrace Area") _
          , New XAttribute("desc", "Area of terrace location analysis") _
          , New XAttribute("OID", field.ObjectID.ToString) _
          , sourceData
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for the source data.
    ''' </summary>
    Private Function MakeSourceDataXml() As XElement
      Dim xelemName As String = "SourceData"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim boundaries As XElement = MakeBoundariesXml()

        retVal.Add( _
          boundaries
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for boundaries.
    ''' </summary>
    Private Function MakeBoundariesXml() As XElement
      Dim xelemName As String = "Boundaries"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim boundary As XElement = MakeBoundaryXml()

        retVal.Add( _
           boundary _
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for a boundary.
    ''' </summary>
    Private Function MakeBoundaryXml() As XElement
      Dim xelemName As String = "Boundary"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim fieldHelp As New MDL.FieldHelper
        Dim allFields As MDL.FieldPackageList = fieldHelp.GetFields(projectId, localInfo)
        Dim field As MDL.FieldRecord = allFields.fields(0).fieldRecord
        Dim coords As String = MakeLandXmlPoints(field.Coords)

        Dim points As New XElement(ns + "PntList2D" _
          , coords _
          )

        '<xs:attribute name="bndType" type="surfBndType" use="required"/>
        '<xs:attribute name="edgeTrim" type="xs:boolean" use="required"/>
        '<xs:attribute name="area" type="xs:double"/>
        '<xs:attribute name="desc" type="xs:string"/>
        '<xs:attribute name="name" type="xs:string"/>
        '<xs:attribute name="state" type="stateType"/>
        retVal.Add( _
            New XAttribute("bndType", "outer") _
          , New XAttribute("edgeTrim", "true") _
          , New XAttribute("name", "Terrace Area") _
          , points _
          )

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for the units.
    ''' </summary> 
    Private Function MakeUnitsXml() As XElement
      Dim xelemName As String = "Units"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        '<xs:attribute name="areaUnit" type="metArea" use="required"/>
        '<xs:attribute name="linearUnit" type="metLinear" use="required"/>
        '<xs:attribute name="volumeUnit" type="metVolume" use="required"/>
        '<xs:attribute name="temperatureUnit" type="metTemperature" use="required"/>
        '<xs:attribute name="pressureUnit" type="metPressure" use="required"/>
        '<xs:attribute name="diameterUnit" type="metDiameter"/>
        '<xs:attribute name="widthUnit" type="metWidth"/>
        '<xs:attribute name="heightUnit" type="metHeight"/>
        '<xs:attribute name="velocityUnit" type="metVelocity"/>
        '<xs:attribute name="flowUnit" type="metFlow"/>
        '<xs:attribute name="angularUnit" type="angularType" default="radians"/>
        '<xs:attribute name="directionUnit" type="angularType" default="radians"/>
        '<xs:attribute name="latLongAngularUnit" type="latLongAngularType" default="decimal degrees"/>
        '<xs:attribute name="elevationUnit" type="elevationType" default="meter"/>
        Dim imperial As New XElement(ns + "Imperial" _
          , New XAttribute("areaUnit", "squareFoot") _
          , New XAttribute("linearUnit", "USSurveyFoot") _
          , New XAttribute("volumeUnit", "cubicFeet") _
          , New XAttribute("temperatureUnit", "fahrenheit") _
          , New XAttribute("pressureUnit", "inHG") _
          , New XAttribute("angularUnit", "decimal degrees") _
          , New XAttribute("directionUnit", "decimal degrees") _
          )
        ', New XAttribute("diameterUnit", "USSurveyFoot") _
        ', New XAttribute("widthUnit", "USSurveyFoot") _
        ', New XAttribute("heightUnit", "USSurveyFoot") _
        ', New XAttribute("velocityUnit", "feetPerSecond") _
        ', New XAttribute("flowUnit", "cubicFeetSecond") _
        retVal.Add( _
          imperial _
          )
        ', New XAttribute("desc", "") _

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for the project.
    ''' </summary> 
    Private Function MakeProjectXml() As XElement
      Dim xelemName As String = "Project"
      Dim retVal As New XElement(ns + xelemName)
      Dim localInfo As String = ""
      Try
        Dim theProject As MDL.Project = MDL.ProjectHelper.Fetch(projectId, localInfo)
        'If localInfo.Contains("error") Then callInfo &= localInfo

        '<xs:attribute name="name" type="xs:string" use="required"/>
        '<xs:attribute name="desc" type="xs:string"/>
        '<xs:attribute name="state" type="stateType"/>
        retVal.Add( _
            New XAttribute("name", theProject.Name) _
          )
        ', New XAttribute("state","") _
        ', New XAttribute("desc", "") _

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString & Environment.NewLine & localInfo, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for the application.
    ''' </summary> 
    Private Function MakeApplicationXml() As XElement
      Dim xelemName As String = "Application"
      Dim retVal As New XElement(ns + xelemName)
      Try
        Dim author As XElement = MakeAuthorXml()

        '<xs:attribute name="name" type="xs:string" use="required"/>
        '<xs:attribute name="desc" type="xs:string"/>
        '<xs:attribute name="manufacturer" type="xs:string"/>
        '<xs:attribute name="version" type="xs:string"/>
        '<xs:attribute name="manufacturerURL" type="xs:string"/>
        '<xs:attribute name="timeStamp" type="xs:dateTime" use="optional"/>
        retVal.Add( _
            New XAttribute("name", "Terrace Location Tool") _
          , New XAttribute("manufacturer", "University of Missouri") _
          , New XAttribute("manufacturerURL", "terrace.missouri.edu") _
          , author _
          )
        ', New XAttribute("desc", "") _
        ', New XAttribute("version", "") _
        ', New XAttribute("timeStamp", Now.ToShortDateString()) _

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for the author.
    ''' </summary>
    Private Function MakeAuthorXml() As XElement
      Dim xelemName As String = "Author"
      Dim retVal As New XElement(ns + xelemName)
      Try
        Dim ownerId As Guid = CF.GetProjectOwner(projectId, Nothing)
        If ownerId = Guid.Empty Then
          retVal.Add(New XAttribute("createdBy", "Not found"))
          Exit Try
        End If
        'Dim cmdText As String = <a>
        '  SELECT CONCAT([FirstName], ' ',[LastName]) 
        '  FROM [mmptrackerdev].[dbo].[Terrace_Users] T1 
        '    INNER JOIN [mmptrackerdev].[dbo].[aspnet_Users] T2 ON T1.UserID=T2.UserID 
        '  WHERE T1.UserID = @userId
        '                      </a>.ToString

        'Dim parm As New System.Data.SqlClient.SqlParameter("@userId", Data.SqlDbType.UniqueIdentifier, 16)
        'parm.Value = New System.Guid(MDL.UserHelper.GetCurrentUser(Nothing).UserId.ToString)
        'CommonFunctions.SendOzzy(EH.GetCallerMethod(), parm.Value.ToString, Nothing)
        Dim uname As String = MDL.UserHelper.GetUserFullName(MDL.UserHelper.GetCurrentUser(Nothing), Nothing) ' APP.GetUserData(cmdText, parm)

        '<xs:attribute name="createdBy" type="xs:string"/>
        '<xs:attribute name="createdByEmail" type="xs:string"/>
        '<xs:attribute name="company" type="xs:string"/>
        '<xs:attribute name="companyURL" type="xs:string"/>
        '<xs:attribute name="timeStamp" type="xs:dateTime" use="optional"/>
        retVal.Add( _
          New XAttribute("createdBy", uname) _
          )
        ', New XAttribute("createdByEmail", "") _
        ', New XAttribute("company", "") _
        ', New XAttribute("companyURL", "") _
        ', New XAttribute("timeStamp", Now.ToShortDateString()) _

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString, Nothing)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Create XML element for the author.
    ''' </summary>
    Private Function MakeLandXmlPoints(ByVal coords As String) As String
      Dim retVal As String = ""
      Try
        'Dim northingeastings As New List(Of String)
        Dim allPoints As String() = coords.Split(CV.PointSplitter.ToCharArray)
        For ptIx As Integer = 0 To allPoints.Count - 1
          Dim pt As String = allPoints(ptIx)
          Dim ords As String() = pt.Split(CV.CoordinateSplitter.ToCharArray)
          retVal &= ords(1) & " " & ords(0) & " "
        Next

      Catch ex As Exception
        CF.SendOzzy(EH.GetCallerMethod & " error: ", ex.ToString, Nothing)
      End Try
      Return retVal.Trim
    End Function

    ''' <summary>
    ''' Retrieve debugging information.
    ''' </summary>
    Public Function GetDebug() As String
      Return debugInfo
    End Function

    ''' <summary>
    ''' Retrieve error information.
    ''' </summary>
    Public Function GetError() As String
      Return errorInfo
    End Function

  End Class

End Namespace
