#Region "Imports"

Imports Microsoft.VisualBasic
Imports System.ComponentModel
Imports System.Reflection.MethodBase
Imports System.Web
Imports System.Web.Script.Services
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.Xml
Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient
Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions

Imports NTS = NetTopologySuite
Imports NGeom = NetTopologySuite.Geometries
Imports NOp = NetTopologySuite.Operation
Imports NIo = NetTopologySuite.IO
Imports GGeom = GeoAPI.Geometries

Imports EH = ErrorHandler
Imports CF = CommonFunctions
Imports CV = CommonVariables
Imports DD = DataDownload
Imports GTA = GIS.GISToolsAddl
Imports MAP = Mapping
Imports UP = UploadTools
Imports PRJ = TerLoc.Model.ProjectHelper
Imports TerLoc.Model

#End Region

' To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line.
<System.Web.Script.Services.ScriptService()> _
<WebService(Namespace:="http://terrace.missouri.edu/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class GISTools
  Inherits System.Web.Services.WebService

#Region "Module declarations"

  Private ReadOnly mtrsPerFoot As Double = CV.FeetToMetersMultiplier
  Private ReadOnly sqMtrsPerAcre As Double = CV.AcresToSquareMetersMultiplier
  Private ReadOnly sqFtPerSqMtr As Double = CV.SquareMetersToSquareFeetMultiplier
  Private ReadOnly coordsPrecision As Integer = CV.CoordinatePrecision
  Private ReadOnly lmuSizePrecision As Integer = CV.LandManagementUnitSizePrecision

  Private Const okayMsg As String = "Okay"
  Private ReadOnly geomPartSplitter As String = CV.GeometryPartSplitter   'use for e.g. holes to divide sets of coords
  Private ReadOnly geomSplitter As String = CV.GeometrySplitter   'use for e.g. multiple polygons to divide sets of coords
   
  Dim myHighPointHelper As New HighPointHelper
  Dim myRidgelineHelper As New RidgelineHelper
  Dim myDivideHelper As New DivideHelper
  Dim myWaterwayHelper As New WaterwayHelper
  Dim myContourHelper As New ContourHelper
  Dim myContourRawHelper As New ContourRawHelper
  Dim myTerraceAreaHelper As New FieldHelper
  Dim myTerraceHelper As New TerraceHelper
  Dim myTerraceReportHelper As New TerraceReportHelper
  Dim myTerraceErrorHelper As New TerraceErrorHelper
  Dim myEquipmentHelper As New EquipmentHelper

  'Use to return info about project export
  Public Structure ReturnProjectExport
    Public fileName As String
    Public eflag As String  'True if soils too big for clipper
    Public info As String
  End Structure

  Private Enum FeatureTypeOptions
    <Description("F")> Field = 0
    <Description("H")> HighPoint
  End Enum

  Private Enum GeometryTypeOptions
    <Description("point")> point = 0
    <Description("line")> line
    <Description("area")> area
  End Enum

  Private Enum ActionTypeOptions
    <Description("add")> add = 0
    <Description("edit")> edit
  End Enum

  Private Function GetDescription(ByVal value As [Enum]) As String
    'Use to get a description attribute from an enum
    'EXAMPLE:
    '<Description("lbs/acre")> LbsPerAcre 'Value listed in enum list
    'Dim desc as string = GetDescription(CType(<enum integer>, <enum name>)) 'generic
    'Dim desc as string = GetDescription(CType(2, GeometryTypeOptions)) 'specific example

    Try
      Dim fi As System.Reflection.FieldInfo = value.[GetType]().GetField(value.ToString())
      Dim attributes As DescriptionAttribute() = _
              DirectCast(fi.GetCustomAttributes(GetType(DescriptionAttribute), False), DescriptionAttribute())
      Dim obj As Object = If((attributes.Length > 0), attributes(0).Description, value.ToString())
      Return obj.ToString
    Catch ex As Exception
      Return Nothing
    End Try
  End Function

#End Region

  ''' <summary>
  ''' Get county names for a state
  ''' </summary>
  <WebMethod()> _
  Public Function GetCounties(ByVal state As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      state = state.Trim
      If String.IsNullOrWhiteSpace(state) Then Throw New ArgumentNullException("state", "No state submitted")
      Dim counties As List(Of String) = StatesCountiesEtc.GetCounties(state, localInfo)
      retVal = String.Join("|", counties)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)
      If counties.Count = 0 Then Throw New ArgumentException("No counties found for state: " & state)

    Catch ex As Exception
      retVal &= String.Format("||{1}", EH.GetCallerMethod(), ex.Message)
    End Try
    Return retVal
  End Function

#Region "Projects"

  ''' <summary>
  ''' Transfer an existing project
  ''' </summary>
  <WebMethod()> _
  Public Function TransferProject(ByVal projectId As Long, ByVal transfereeName As String, ByVal addlText As String) As String
    Dim retVal As String = ""
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try

      PRJ.TransferProject(projectId, transfereeName, addlText, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = callInfo
    Return retVal
  End Function

  ''' <summary>
  ''' Duplicate an existing project
  ''' </summary>
  <WebMethod()> _
  Public Function DuplicateProject(ByVal projectId As Long, ByVal name As String, ByVal notes As String) As ReturnProjectsStructure
    Dim retVal As New ReturnProjectsStructure
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      PRJ.DuplicateProject(projectId, name, notes, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    Try
      retVal = GetProjects()
    Catch ex As Exception
      callInfo &= String.Format("  {0} get projects error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info &= callInfo
    Return retVal
  End Function

  <WebMethod()> _
  Public Function AddProject(ByVal projectdata As String) As ReturnProjectsStructure
    Dim retVal As New ReturnProjectsStructure
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      PRJ.AddProject(projectdata, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    Try
      retVal = GetProjects()
    Catch ex As Exception
      callInfo &= String.Format("  {0} get projects error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info &= callInfo
    Return retVal
  End Function

  <WebMethod()> _
  Public Function EditProject(ByVal projectId As Long, ByVal projectdata As String) As ReturnProjectsStructure
    Dim retVal As New ReturnProjectsStructure
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      PRJ.EditProject(projectId, projectdata, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    Try
      retVal = GetProjects()
    Catch ex As Exception
      callInfo &= String.Format("  {0} get projects error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info &= callInfo
    Return retVal
  End Function

  <WebMethod()> _
  Public Function GetProjects() As ReturnProjectsStructure
    Dim retVal As New ReturnProjectsStructure
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      retVal = PRJ.GetProjects(usrId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      retVal.info &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info &= callInfo
    Return retVal
  End Function

  ''' <summary>
  ''' Export project data
  ''' </summary> 
  ''' <param name="files">delimited list of files to create</param>
  <WebMethod()> _
  Public Function DumpData(ByVal projectId As Long, ByVal projectName As String, ByVal files As String) As ReturnProjectExport
    Dim retVal As ReturnProjectExport = Nothing
    Dim callInfo As String = ""
    Try
      Dim localInfo As String = ""
      Dim timeStamp As String = Date.Now.ToString("s").Replace(":", "")

      Dim prjFoldr As String = CF.GetProjectFolderByProjectId(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Dim downloadFolderName As String = String.Format("{0}{1}", prjFoldr, "\UserDocuments\Downloads\")
      Dim di As New IO.DirectoryInfo(downloadFolderName)
      If Not di.Exists Then di.Create()

      Dim fileNameBase As String = String.Format("{0}{1}", downloadFolderName, projectName)
      Dim zipFileName As String = String.Format("{0}_{1}.zip", fileNameBase, timeStamp) 'e.g. Farm17_2013-09-08t182052.zip

      Dim fileNamesForZipping As List(Of String) = New List(Of String)
      Dim fileName As String = ""

      'Make GIS shapefiles
      fileName = ""
      If files.IndexOf("gis|") > -1 Then
        fileName = downloadFolderName
        Dim gisZipFileName As String = String.Format("{0}{1}_{2}.zip", downloadFolderName, projectName, "GIS") 'e.g. Farm17_GIS.zip
        localInfo = ""
        DD.CreateGisShapefiles(projectId, projectName, fileName, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        fileNamesForZipping.Add(gisZipFileName)
      End If

      'Make GIS XML files
      fileName = ""
      If files.IndexOf("gisxml|") > -1 Then
        localInfo = ""
        fileName = String.Format("{0}.xml", fileNameBase)
        DD.MakeGisXml(projectId, fileName, localInfo)
        If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        fileNamesForZipping.Add(fileName)
      End If

      'Get Clipper files  
      If files.IndexOf("clipper|") > -1 Then
        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim usrTime As String = "__" & usrId.ToString & timeStamp 'need unique id for clipper files
        localInfo = ""
        fileName = DD.CleanFileNameForClipper(projectName, localInfo) & usrTime
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        Dim bbox As String = GTA.GetFeaturesEnvelopeBBox(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        Dim clipInfo As String = DD.GetClipper(projectId, fileName, bbox, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If clipInfo <> "" Then
          Dim clipResponse() As String = clipInfo.Split("&".ToArray, StringSplitOptions.RemoveEmptyEntries)
          Dim zip As String = clipResponse(0).Split("=".ToArray, StringSplitOptions.RemoveEmptyEntries)(1) 'zip file name that clipper used
          Dim myZip As String = downloadFolderName & zip.Replace(".zip", "_clipper.zip").Replace(usrTime, "")
          Dim eflag As String = CStr(clipResponse(1).Split("=".ToArray, StringSplitOptions.RemoveEmptyEntries)(1)) 'flag if soils timed out
          retVal.eflag = eflag
          Dim clipFolder As String = "D:\websites\ClipperRoot\NRCSdata\temp\"
          File.Copy(clipFolder & zip, myZip.Replace(usrTime, ""), True)

          Using zipFile As New Ionic.Zip.ZipFile(myZip)
            For Each entry As Ionic.Zip.ZipEntry In zipFile.EntriesSorted
              'callInfo.Append(entry.FileName & Environment.NewLine)
              entry.FileName = entry.FileName.Replace(usrTime, "")
            Next
            zipFile.Save()
          End Using
          fileNamesForZipping.Add(myZip)
        End If
      End If

      localInfo = ""
      CF.Zip(fileNamesForZipping, zipFileName, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      'retVal = String.Format("{0} zip: {1};", retVal, localInfo)

      Dim fi As New IO.FileInfo(zipFileName)
      'If fi.Exists Then retVal = String.Format("zipfile: {1}; {0}", retVal, MapURL(zipFileName))
      If fi.Exists Then retVal.fileName = MapURL(zipFileName)
      retVal.fileName = MapURL(zipFileName)
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

#End Region

  Public Function MapURL(ByVal Path As String) As String
    Dim url As String = ""
    Dim projFolderIndex As Integer = Path.ToLower.IndexOf("\terraceprojectfolders")
    If projFolderIndex > -1 Then
      Path = Path.Substring(projFolderIndex)
      Dim AppPath As String = HttpContext.Current.Server.MapPath("~")
      url = String.Format("{0}", Path.ToLower.Replace(AppPath, "").Replace("terraceprojectfolders", "UserFolders").Replace("\", "/"))
    End If
    Return url
  End Function

#Region "GIS"

#Region "Terraces"

  ''' <summary>
  ''' Show terrace report.
  ''' </summary>
  <WebMethod()> _
  Public Function ShowTerraceReport(ByVal projectId As Long) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      'Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      'If localInfo.ToLower.Contains("error") Then Throw New ArgumentException(localInfo)

      localInfo = ""
      myTerraceReportHelper.OpenTerraceReport(projectId, localInfo)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)
    Catch ex As Exception
      retVal &= EH.GetCallerMethod() & " error: " & ex.ToString
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Calculate terraces.
  ''' </summary>
  <WebMethod()> _
  Public Function CalculateTerraces(ByVal projectId As Long) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      'If localInfo.ToLower.Contains("error") Then Throw New ArgumentException(localInfo)

      localInfo = ""
      myContourHelper.DeleteAll(projectId, usrId, localInfo)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)

      localInfo = ""
      retVal = Fortran.CalculateTerraces(projectId, localInfo)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)
    Catch ex As Exception
      retVal &= EH.GetCallerMethod() & " error: " & ex.ToString
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Convert terraces from fortran xml.
  ''' </summary>
  <WebMethod()> _
  Public Function LoadTerraces(ByVal projectId As Long) As TerracePackageList
    Dim retVal As New TerracePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      localInfo = ""
      Fortran.LoadTerraces(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " load error: " & ex.ToString
    End Try
    Try
      localInfo = ""
      retVal = myTerraceHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " fetch error: " & ex.ToString
    End Try
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  ''' <summary>
  ''' Update cost per foot option for a terrace.
  ''' </summary>
  <WebMethod()> _
  Public Function SubmitTerraceCost(ByVal projectId As Long, ByVal featureId As Long, ByVal costId As Integer) As String
    Dim retVal As String = ""
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      localInfo = ""
      myTerraceHelper.UpdateTerraceCost(projectId, featureId, costId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " load error: " & ex.ToString
    End Try
    retVal = (retVal & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  ''' <summary>
  ''' Update cost per foot option for a terrace.
  ''' </summary>
  <WebMethod()> _
  Public Function SaveCustom(ByVal projectId As Long, ByVal featureIds As List(Of Long)) As String
    Dim retVal As String = ""
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      localInfo = ""
      myTerraceHelper.UpdateTerraceCustom(projectId, featureIds, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " load error: " & ex.ToString
    End Try
    retVal = (retVal & " " & callInfo.ToString).Trim
    Return retVal
  End Function

#End Region

#Region "Terrace Areas"

  <WebMethod()> _
  Public Function UploadFields(ByVal projectId As Long, ByVal upfileName As String, ByVal upfile As Byte(), ByVal idCol As String) As FieldPackageList
    Dim retVal As New FieldPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim fs As FileStream
    Dim biWriter As BinaryWriter
    Dim filePath As String = Path.Combine(CF.GetProjectFolderByProjectId(projectId, Nothing), "Uploads", upfileName)
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      fs = File.Open(filePath, FileMode.CreateNew)
      biWriter = New BinaryWriter(fs)
      biWriter.Write(upfile)

      localInfo = ""
      CF.InsertProjectSystemMessage(projectId, "soils", localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} upload fields error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    Try
      localInfo = ""
      retVal = myTerraceAreaHelper.GetFields(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      retVal.fieldShapesChanged = True
    Catch ex As Exception
      callInfo &= String.Format("  {0} get fields error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

  <WebMethod()> _
  Public Function DeleteField(ByVal projectId As Long, ByVal id As String) As FieldPackageList
    Dim updateSA As Boolean = False
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim retVal As New FieldPackageList
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myTerraceAreaHelper.DeleteField(projectId, usrId, id, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo : localInfo = ""
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try

    retVal = GetFields(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function AddField(ByVal projectId As Long, ByVal featureData As String) As FieldPackageList
    'http://encosia.com/asp-net-web-services-mistake-manual-json-serialization/?utm_source=feedburner&utm_medium=feed&utm_campaign=Feed%3a%20Encosia%20%28Encosia%29
    'try using EditField instead of String for featureData to see if it auto-deserializes
    Dim retVal As New FieldPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myTerraceAreaHelper.AddField(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} add field error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    Try
      retVal = GetFields(projectId)
      retVal.fieldShapesChanged = True
    Catch ex As Exception
      callInfo &= String.Format("  {0} get fields error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

  <WebMethod()> _
  Public Function EditField(ByVal projectId As Long, ByVal featureId As String, ByVal featureData As String) As FieldPackageList
    Dim retVal As New FieldPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim origShp As String = ""
    Dim newShp As String = ""
    Try
      If String.IsNullOrWhiteSpace(featureId) Then Throw New ArgumentNullException(featureId, "No field identifier found.")

      localInfo = ""
      origShp = GTA.GetFieldShape(projectId, featureId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      localInfo = ""
      myTerraceAreaHelper.EditField(projectId, usrId, featureId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      newShp = GTA.GetFieldShape(projectId, featureId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} edit field error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    Try
      localInfo = ""
      retVal = myTerraceAreaHelper.GetFields(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      If origShp.Trim <> newShp.Trim Then retVal.fieldShapesChanged = True
    Catch ex As Exception
      callInfo &= String.Format("  {0} get fields error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

  <WebMethod()> _
  Public Function GetFields(ByVal projectId As Long) As FieldPackageList

    Dim retVal As New FieldPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Try
        Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        retVal = myTerraceAreaHelper.GetFields(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try

    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

  <WebMethod()> _
  Public Function ImportFields(ByVal projectId As Long, ByVal fileName As String, ByVal idColName As String, _
                               ByVal notesColName As String, ByVal clipExisting As String) As FieldPackageList
    Dim retVal As New FieldPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim clipExistingShapes As Boolean = If(clipExisting.ToLower = "yes", True, False)
      'UP.ImportGisFields(projectId, fileName, idColName, notesColName, clipExistingShapes, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} import fields error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    Try
      retVal = GetFields(projectId)
      retVal.fieldShapesChanged = True
    Catch ex As Exception
      callInfo &= String.Format("  {0} get fields error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

#End Region

#Region "High Points"

  <WebMethod()> _
  Public Function DeleteHighPoint(ByVal projectId As Long, ByVal featureId As String) As HighPointPackageList
    Dim retVal As New HighPointPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myHighPointHelper.Delete(projectId, usrId, featureId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetHighPoints(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function AddHighPoint(ByVal projectId As Long, ByVal featureData As String) As HighPointPackageList
    Dim retVal As New HighPointPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Dim newOid As Long = -1
    Try
      localInfo = ""
      newOid = myHighPointHelper.Insert(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} insert high point error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetHighPoints(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function EditHighPoint(ByVal projectId As Long, ByVal featureId As String, ByVal featureData As String) As HighPointPackageList
    Dim retVal As New HighPointPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
    If localInfo.ToLower.Contains("error") Then Throw New ArgumentException(localInfo)
    Try
      If String.IsNullOrWhiteSpace(featureId) Then Throw New ArgumentNullException(featureId, "No high point identifier found.")

      localInfo = ""
      myHighPointHelper.Update(projectId, usrId, featureId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} edit high point error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetHighPoints(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function GetHighPoints(ByVal projectId As Long) As HighPointPackageList
    Dim retVal As New HighPointPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      retVal = myHighPointHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

#End Region

#Region "Ridgelines"

  <WebMethod()> _
  Public Function DeleteRidgeline(ByVal projectId As Long, ByVal id As String) As RidgelinePackageList
    Dim retVal As New RidgelinePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myRidgelineHelper.Delete(projectId, usrId, id, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetRidgelines(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function AddRidgeline(ByVal projectId As Long, ByVal featureData As String) As RidgelinePackageList
    Dim retVal As New RidgelinePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Dim newOid As Long = -1
    Try
      localInfo = ""
      newOid = myRidgelineHelper.Insert(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} insert ridgeline error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetRidgelines(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function EditRidgeline(ByVal projectId As Long, ByVal featureId As String, ByVal featureData As String) As RidgelinePackageList
    Dim retVal As New RidgelinePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      If String.IsNullOrWhiteSpace(featureId) Then Throw New ArgumentNullException(featureId, "No ridgeline identifier found.")

      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myRidgelineHelper.Update(projectId, usrId, featureId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} edit ridgeline error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetRidgelines(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function GetRidgelines(ByVal projectId As Long) As RidgelinePackageList
    Dim retVal As New RidgelinePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      retVal = myRidgelineHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

#End Region

#Region "Divides"

  <WebMethod()> _
  Public Function UpdateDivideAligning(ByVal projectId As Long, ByVal featureData As String) As DividePackageList
    Dim retVal As New DividePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Try
      localInfo = ""
      myDivideHelper.UpdateDivideAlignment(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} update divide Align error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetDivides(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function UpdateDivideOrdering(ByVal projectId As Long, ByVal featureData As String) As DividePackageList
    Dim retVal As New DividePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Try
      localInfo = ""
      myDivideHelper.UpdateDivideOrdinals(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} update divide order error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetDivides(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function DeleteDivide(ByVal projectId As Long, ByVal id As String) As DividePackageList
    Dim retVal As New DividePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myDivideHelper.Delete(projectId, usrId, id, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetDivides(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function AddDivide(ByVal projectId As Long, ByVal featureData As String) As DividePackageList
    Dim retVal As New DividePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Dim newOid As Long = -1
    Try
      localInfo = ""
      newOid = myDivideHelper.Insert(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myDivideHelper.IncrementOtherDivides(projectId, newOid, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} insert divide error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetDivides(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function EditDivide(ByVal projectId As Long, ByVal featureId As String, ByVal featureData As String) As DividePackageList
    Dim retVal As New DividePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      If String.IsNullOrWhiteSpace(featureId) Then Throw New ArgumentNullException(featureId, "No divide identifier found.")

      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myDivideHelper.Update(projectId, usrId, featureId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} edit divide error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetDivides(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function GetDivides(ByVal projectId As Long) As DividePackageList
    Dim retVal As New DividePackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      retVal = myDivideHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

#End Region

#Region "Waterways"

  <WebMethod()> _
  Public Function UpdateWaterwayAligning(ByVal projectId As Long, ByVal featureData As String) As WaterwayPackageList
    Dim retVal As New WaterwayPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Try
      localInfo = ""
      myWaterwayHelper.UpdateWaterwayAlignment(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} update waterway Align error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetWaterways(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function UpdateWaterwayOrdering(ByVal projectId As Long, ByVal featureData As String) As WaterwayPackageList
    Dim retVal As New WaterwayPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Try
      localInfo = ""
      myWaterwayHelper.UpdateWaterwayOrdinals(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} update waterway order error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetWaterways(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function DeleteWaterway(ByVal projectId As Long, ByVal id As String) As WaterwayPackageList
    Dim retVal As New WaterwayPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myWaterwayHelper.Delete(projectId, usrId, id, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetWaterways(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function AddWaterway(ByVal projectId As Long, ByVal featureData As String) As WaterwayPackageList
    Dim retVal As New WaterwayPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim newOid As Long = -1
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      newOid = myWaterwayHelper.Insert(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myWaterwayHelper.IncrementOtherWaterways(projectId, newOid, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} insert waterway error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetWaterways(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function EditWaterway(ByVal projectId As Long, ByVal featureId As String, ByVal featureData As String) As WaterwayPackageList
    Dim retVal As New WaterwayPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      If String.IsNullOrWhiteSpace(featureId) Then Throw New ArgumentNullException(featureId, "No waterway identifier found.")

      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myWaterwayHelper.Update(projectId, usrId, featureId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} edit waterway error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal = GetWaterways(projectId)
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function GetWaterways(ByVal projectId As Long) As WaterwayPackageList
    Dim retVal As New WaterwayPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      retVal = myWaterwayHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

#End Region

#Region "Contours"

  ''' <summary>
  ''' Delete raw and smooth contours.
  ''' </summary>
  <WebMethod()> _
  Public Function DeleteAllContours(ByVal projectId As Long) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      'If localInfo.ToLower.Contains("error") Then Throw New ArgumentException(localInfo)

      localInfo = ""
      myContourHelper.DeleteAll(projectId, usrId, localInfo)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)

      localInfo = ""
      myContourRawHelper.DeleteAll(projectId, usrId, localInfo)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)

      'remove old records
      Dim myLegacyContourXmlHelper As New Legacy.XmlContourHelper
      localInfo = ""
      myLegacyContourXmlHelper.DeleteAll(projectId, usrId, localInfo)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)

    Catch ex As Exception
      retVal &= EH.GetCallerMethod() & " error: " & ex.ToString
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Check info from old terrace database
  ''' </summary>
  <WebMethod()> _
  Public Function CheckOldContour() As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      UploadTools.WriteContourInfo(localInfo)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)
    Catch ex As Exception
      retVal &= EH.GetCallerMethod() & " error: " & ex.ToString
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Calculate smoothed contours
  ''' </summary>
  <WebMethod()> _
  Public Function CalculateContours(ByVal projectId As Long) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      'If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myContourHelper.DeleteAll(projectId, usrId, localInfo)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)
      localInfo = ""
      retVal = Fortran.CalculateContours(projectId, localInfo)
      If localInfo.Contains("error") Then Throw New ArgumentException(localInfo)
    Catch ex As Exception
      retVal &= EH.GetCallerMethod() & " error: " & ex.ToString
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Convert smoothed contours from fortran xml
  ''' </summary>
  <WebMethod()> _
  Public Function LoadSmoothContours(ByVal projectId As Long) As ContourPackageList
    Dim retVal As New ContourPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      localInfo = ""
      Fortran.LoadSmoothContours(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " load error: " & ex.ToString
    End Try
    Try
      localInfo = ""
      retVal = myContourHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " fetch error: " & ex.ToString
    End Try
    retVal.info = (retVal.info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  ''' <summary>
  ''' Deletes all contours for a project and uploads and stores new contours.
  ''' </summary>
  <WebMethod()> _
  Public Function ImportContours(ByVal projectId As Long, ByVal fileName As String, _
                               ByVal contourCol As String) As ContourRawPackageList
    Dim retVal As New ContourRawPackageList
    Dim callInfo As String = ""
    Dim localInfo As String = ""

    'Bring in new stuff (raw)
    Try
      localInfo = ""
      myContourRawHelper.ImportRawContours(projectId, fileName, contourCol, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " import raw contours error: " & ex.ToString
    End Try

    'Send back new features (raw)
    Try
      localInfo = ""
      Dim helpr As New ContourRawHelper
      retVal = helpr.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      Dim contCount As Integer = retVal.contours.Count
      For contIx As Integer = 0 To contCount - 1
        retVal.contours.Item(contIx).contourRecord.Shape = "" 'nullify shape attr since it's too big for ajax
      Next

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " get contours error: " & ex.ToString
    End Try
    retVal.info = callInfo
    Return retVal
  End Function

#End Region

#End Region

#Region "Equipment"

  <WebMethod()> _
  Public Function SetEquipment(ByVal projectId As Long, ByVal featureId As Long, ByVal featureData As String) As EquipmentPackage
    Dim retVal As New EquipmentPackage
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
    If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Dim newOid As Long = -1

    If featureId < 1 Then
      Try
        localInfo = ""
        Dim newId As Long = myEquipmentHelper.Insert(projectId, usrId, featureData, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} add equipment error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
    Else
      Try
        localInfo = ""
        myEquipmentHelper.Update(projectId, usrId, featureId, featureData, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} edit equipment error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
    End If

    Try
      localInfo = ""
      retVal = myEquipmentHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} get equipment error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.Info = (retVal.Info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function DeleteEquipment(ByVal projectId As Long, ByVal id As String) As EquipmentPackage
    Dim retVal As New EquipmentPackage
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myEquipmentHelper.Delete(projectId, usrId, id, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try

    Try
      localInfo = ""
      retVal = myEquipmentHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} get equipment error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.Info = (retVal.Info & " " & callInfo.ToString).Trim
    Return retVal
  End Function

  <WebMethod()> _
  Public Function AddEquipment(ByVal projectId As Long, ByVal featureData As String) As EquipmentPackage
    Dim retVal As New EquipmentPackage
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      Dim newId As Long = myEquipmentHelper.Insert(projectId, usrId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} add equipment error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try

    Try
      localInfo = ""
      retVal = myEquipmentHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} get equipment error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.Info = callInfo
    Return retVal
  End Function

  <WebMethod()> _
  Public Function EditEquipment(ByVal projectId As Long, ByVal featureId As String, ByVal featureData As String) As EquipmentPackage
    Dim retVal As New EquipmentPackage
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      If String.IsNullOrWhiteSpace(featureId) Then Throw New ArgumentNullException(featureId, "No field identifier found.")

      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      myEquipmentHelper.Update(projectId, usrId, featureId, featureData, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= String.Format("  {0} edit equipment error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    Try
      localInfo = ""
      retVal = myEquipmentHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} get equipment error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try
    retVal.Info = callInfo
    Return retVal
  End Function

  <WebMethod()> _
  Public Function GetEquipment(ByVal projectId As Long) As EquipmentPackage

    Dim retVal As New EquipmentPackage
    Dim callInfo As String = ""
    Dim localInfo As String = ""
    Try
      'Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
      'If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      localInfo = ""
      retVal = myEquipmentHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
    End Try

    retVal.Info = callInfo
    Return retVal
  End Function

#End Region

End Class
