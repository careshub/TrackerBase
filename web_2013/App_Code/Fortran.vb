#Region "Imports"
Imports Microsoft.VisualBasic
Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Reflection.MethodBase
Imports System.Web
Imports CommonFunctions
Imports TerLoc.Model
Imports GeoAPI.Geometries
Imports GIS.GISToolsAddl
Imports EH = ErrorHandler
#End Region

''' <summary>
''' Holds arguments and flags for smoothing contours
''' </summary> 
Public Class SmoothParameters
  'cmd_line = exe_dir & exe_fn & " -P " & projid & " -D " & data_dir & " -E " & exe_dir & db_flag & verbose_flag
  Public Property project_arg As String = "" 'required
  Public Property exe_fn As String = "" 'executable file
  Public Property exe_dir As String = "" 'executable directory
  Public Property data_dir As String = "" 'data directory
  Public Property verbose_flag As String = "" ' -v, -vv, -vd
  Public Property db_flag As String = "" ' -dp, -dd
  Public Property output_flag As String = "" ' -No
  Public Property smooth_flag As String = "" ' -Smo

  ''' <summary>
  ''' Custom ToString for outputting command line arguments
  ''' </summary>
  Public Overrides Function ToString() As String
    Return String.Format(" -P {0} {1} {2} {3} {4} {5} {6} ", _
                    project_arg _
                    , If(String.IsNullOrWhiteSpace(exe_dir), "", " -E " & exe_dir.Trim) _
                    , If(String.IsNullOrWhiteSpace(data_dir), "", " -D " & data_dir.Trim) _
                    , verbose_flag _
                    , db_flag _
                    , smooth_flag _
                    , output_flag _
                    )
  End Function
End Class

Public Class Fortran

#Region "Module declarations"

  Private Shared BR As String = CommonVariables.HtmlLineBreak
  Private Shared dataConn As String = CommonFunctions.GetBaseDatabaseConnString
  Private Shared dataSchema As String = CommonVariables.ProjectProductionSchema
  Private Shared myContourHelper As New ContourHelper
  Private Shared myContourXmlHelper As New ContourXmlHelper
  Private Shared myTerraceHelper As New TerraceHelper
  Private Shared myTerraceAreaHelper As New FieldHelper
  Private Shared myRidgelineHelper As New RidgelineHelper
  Private Shared myWaterwayHelper As New WaterwayHelper
  Private Shared myDivideHelper As New DivideHelper
  Private Shared myHighPointHelper As New HighPointHelper
  Private Shared myEquipmentHelper As New EquipmentHelper
  Private Shared myTerraceErrorHelper As New TerraceErrorHelper
  Private Shared myTerraceReportHelper As New TerraceReportHelper

  Private Shared configDir As String = "C:\Workdata\TerLoc\Config"
  Private Shared configSmooth As String = "ContourSmooth.config"

  ''NOTE: Many commented parts are from old code 
  ''exe_dir = "c:\workspace\ArcIMS\website\terrace\"
  'Private Shared exe_dir As String = "c:\Workdata\AdamPutTerraceStuffHere\ArcIMS_Website_terrace\terrace\"
  ''exe_fn = "Terrace_Run-V5.exe"
  'Private Shared exe_fn As String = "Terrace_Run-V5.exe"
  ''data_dir = "c:\workspace\temp\"
  'Private Shared data_dir As String = "c:\Workdata\TerLoc\temp\"

#End Region

  ''' <summary>
  ''' Load terraces from legacy tables to current tables.
  ''' </summary>
  Public Shared Sub LoadTerraces(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      localInfo = ""
      Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      ' If not keeping old ones, use this. Else, put inside transfer functions.
      localInfo = ""
      myTerraceHelper.DeleteAll(projectId, usrId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      localInfo = ""
      myTerraceHelper.TransferTerraces(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      myTerraceReportHelper.DeleteAll(projectId, usrId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      localInfo = ""
      myTerraceReportHelper.TransferTerraceReports(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      myTerraceErrorHelper.DeleteAll(projectId, usrId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      localInfo = ""
      myTerraceErrorHelper.TransferTerraceErrors(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Load smooth contours from legacy tables to current tables.
  ''' </summary>
  Public Shared Sub LoadSmoothContours(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      localInfo = ""
      myContourHelper.ConvertSmoothXml(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Entry function for creating smooth contours
  ''' </summary>
  Public Shared Function CalculateContours(ByVal projectId As Long, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Dim debugInfo As String = ""
    Try
      debugInfo &= " Calc 1 " & Now.ToString & "  "  ' ----- DEBUG
      localInfo = ""
      LoadTablesForSmoothing(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      debugInfo &= " Calc 2 " & Now.ToString & "  "  ' ----- DEBUG
      localInfo = ""
      Dim smoothInfo As String = SmoothContours(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
      If Not String.IsNullOrWhiteSpace(smoothInfo) Then callInfo &= smoothInfo

      debugInfo &= " Calc 3 " & Now.ToString & "  "  ' ----- DEBUG
      'Converting won't work if Fortran is delayed since record won't be written yet.
      localInfo = ""
      myContourHelper.ConvertSmoothXml(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      debugInfo &= " Calc 4 " & Now.ToString & "  "  ' ----- DEBUG
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    CommonFunctions.SendOzzy("CalculateContours", debugInfo, Nothing) ' ----- DEBUG
    Return retVal
  End Function

  ''' <summary>
  ''' Copy/format data into defined tables for smoothing
  ''' Need projectname, 41 Terrace Area, 11 Farm Area
  ''' </summary> 
  Public Shared Sub LoadTablesForSmoothing(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    localInfo = ""
    LoadProjectNameForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    localInfo = ""
    LoadTerraceAreaForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    localInfo = ""
    LoadXmlContourForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

  End Sub

  ''' <summary>
  ''' Copy/format data into defined tables for smoothing
  ''' </summary> 
  Public Shared Sub LoadProjectNameForFortran(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      Dim thisProject As EditProject = ProjectHelper.GetProjectById(projectId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      localInfo = ""
      Dim thisOp As DataTable = OperationHelper.GetTable(projectId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      Dim opRow As DataRow = Nothing
      If thisOp.Rows.Count > 0 Then opRow = thisOp.Rows(0)
      Dim projectDate = New Date(1976, 7, 4)
      If opRow IsNot Nothing Then
        projectDate = New Date(NullSafeInteger(opRow.Item("StartCalYear"), 1976) _
                              , NullSafeInteger(opRow.Item("StartCalMonth"), 7) _
                              , 4)
      End If

      Dim boundingBox As New ProjectBounds
      localInfo = ""
      boundingBox = ProjectHelper.GetBoundingBox(projectId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      If boundingBox Is Nothing Then
        callInfo &= " No bounding box usable for project. "
        Return
      End If

      localInfo = ""
      Dim equip As EquipmentPackage = myEquipmentHelper.Fetch(projectId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      Dim numMachines As Integer = -1
      Dim rowWidth As Integer = -1
      Dim numRows As Integer = -1
      If equip IsNot Nothing Then
        numMachines = NullSafeInteger(equip.EquipmentRecord.NumberOfMachines, -1)
        rowWidth = NullSafeInteger(equip.EquipmentRecord.MachineRowWidth, -1)
        numRows = NullSafeInteger(equip.EquipmentRecord.NumberOfRows, -1)
      End If

      Dim oldRec As New Legacy.ProjectNameRecord   'insertion record
      With oldRec
        .PROJID = projectId
        .PROJECTNAME = thisProject.Name
        .PROJECTDATE = projectDate
        .MINX = boundingBox.MinX
        .MINY = boundingBox.MinY
        .MAXX = boundingBox.MaxX
        .MAXY = boundingBox.MaxY
        .CLIENTNAME = HttpContext.Current.User.Identity.Name '(255)
        .PLANTECH = HttpContext.Current.User.Identity.Name '(255)
        'Overwrite defaults if necessary
        If numMachines > 0 Then .NOMACHINES = numMachines ' 1 is default
        If rowWidth > 0 Then .ROWWIDTH = rowWidth ' 30 is default
        If numRows > 0 Then .NOROWS = numRows ' 12 is default
        '.MAXCHANNELVEL = 2 'some of these are defaults
        '.MANNINGS = 0.035
        '.SIDESLOPE = 10
        '.RUNOFFCOEFF = 0.7D '(18,2)
        '.RUNOFFINTENSITY = 7
        '.BOTTERRACE_CHANNEL = 0
        '.LANDSLOPE = 0.07D '(18,3)
        .TERRACESPACE = 1 'or null
        .WIDTHMAX = 180
        .WIDTHMIN = 0
      End With

      localInfo = ""
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Dim legHelpr As New Legacy.ProjectNameHelper
      'remove old records
      localInfo = ""
      legHelpr.DeleteAll(projectId, usrId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      'add Proj info into old terrace format
      legHelpr.InsertProjectName(projectId, usrId, oldRec, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Copy/format data into defined tables for smoothing
  ''' </summary> 
  Public Shared Sub LoadTerraceAreaForFortran(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      'get current record from terloc
      localInfo = ""
      Dim terrAreaList As List(Of FieldPackage) = myTerraceAreaHelper.GetFieldsList(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      Dim terrArea As FieldPackage = Nothing
      If terrAreaList IsNot Nothing AndAlso terrAreaList.Count > 0 Then
        terrArea = terrAreaList.Item(0)
      End If

      'validate
      If terrArea Is Nothing Then Throw New ArgumentNullException("Terrace Area Record")
      If String.IsNullOrWhiteSpace(terrArea.fieldRecord.Shape) Then Throw New ArgumentNullException("Terrace Area")

      'setup legacy record
      localInfo = ""
      Dim geom As IGeometry = ConvertWkbToGeometry(terrArea.fieldRecord.Shape, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim coords As String = GetCoordsStringFromGeom(geom, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim utmCoords As String = ConvertLatLonCoordsToUtm(coords, 15, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim utmGeom As IGeometry = CreateMultipolyFromCoordString(utmCoords, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim fcoords As String = FormatFortranCoords(utmCoords, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      Dim oldRec As New Legacy.ProjWorkRecord 'insertion record
      With oldRec
        .PROJID = projectId
        .FEATUREID = Legacy.TerraceFeature.TERRACEAREA
        .FEATURECOORDS = fcoords
        .FEATURELABEL = "TERRACE AREA"
        .FEATURELENGTH = CType(utmGeom, IMultiPolygon).Length
        .PRACTICETYPE = 1
      End With

      localInfo = ""
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Dim legHelpr As New Legacy.ProjWorkHelper
      'remove old records
      localInfo = ""
      legHelpr.DeleteById(projectId, usrId, Legacy.TerraceFeature.TERRACEAREA, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      'add into old terrace format 
      localInfo = ""
      legHelpr.InsertProjWork(projectId, usrId, oldRec, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      'repeat for farm area using same geometry, fortran wants both, I think.
      oldRec.FEATUREID = Legacy.TerraceFeature.FARMAREA
      oldRec.FEATURELABEL = "FARM AREA"
      'remove old records
      localInfo = ""
      legHelpr.DeleteById(projectId, usrId, Legacy.TerraceFeature.FARMAREA, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      'add into old terrace format 
      localInfo = ""
      legHelpr.InsertProjWork(projectId, usrId, oldRec, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Copy/format data into defined tables for smoothing
  ''' </summary> 
  Public Shared Sub LoadXmlContourForFortran(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      Dim oldXml As Legacy.XmlContourFull = Nothing 'insertion record

      'get current ORG record from terloc 
      localInfo = ""
      Dim contXmlList As List(Of ContourXmlPackage) = myContourXmlHelper.GetContourXmlsList(projectId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      If contXmlList IsNot Nothing AndAlso contXmlList.Count > 0 Then
        Dim orgXmls As List(Of ContourXmlPackage) = (From rec In contXmlList Where rec.contourXmlRecord.XmlType = "ORG").ToList
        If orgXmls IsNot Nothing AndAlso orgXmls.Count > 0 Then
          Dim contXml As ContourXmlPackage = orgXmls.Item(0)
          oldXml = New Legacy.XmlContourFull
          With oldXml
            .ProjID = projectId
            .XmlType = "ORG"
            .XmlContour = contXml.contourXmlRecord.XmlContour
          End With
        End If
      End If

      'validate
      If oldXml Is Nothing Then Throw New ArgumentNullException("Contour Xml Record")
      If String.IsNullOrWhiteSpace(oldXml.XmlContour) Then Throw New ArgumentNullException("Contour Xml")

      localInfo = ""
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Dim legXml As New Legacy.XmlContourHelper
      'remove old records
      localInfo = ""
      legXml.DeleteAll(projectId, usrId, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      'add ORG into old terrace format
      oldXml.XmlContour = ContourXmlHelper.AddArcXmlTags(oldXml.XmlContour, Nothing)
      localInfo = ""
      legXml.InsertXmlContour(projectId, usrId, oldXml, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Load parameters from config file.
  ''' </summary> 
  Public Shared Function LoadSmoothParameters(ByRef callInfo As String) As SmoothParameters
    Dim retVal As New SmoothParameters
    Dim localInfo As String = ""
    Dim debugInfo As String = ""
    Try
      Dim propertyInfos As PropertyInfo()
      propertyInfos = GetType(SmoothParameters).GetProperties() '(BindingFlags.[Public] Or BindingFlags.[Static])
      Dim parmType = GetType(SmoothParameters)

      Dim filePath As String = Path.Combine(configDir, configSmooth)
      Dim configLine As String
      Dim flag As String
      Dim val As String
      Dim eqIx As Integer
      Using sr As StreamReader = New StreamReader(filePath)
        Do While sr.Peek() >= 0
          configLine = sr.ReadLine().Trim
          If String.IsNullOrWhiteSpace(configLine) OrElse configLine.Substring(0, 1) = "#" Then Continue Do

          eqIx = configLine.IndexOf("=")
          If eqIx > -1 Then
            flag = configLine.Substring(0, eqIx).Replace("=", "").Trim
            val = configLine.Substring(eqIx).Replace("=", "").Trim
            debugInfo &= BR & configLine ' & BR & flag & ": " & val
            If parmType.GetProperty(flag) IsNot Nothing Then
              CallByName(retVal, flag, CallType.Set, val)
            End If
          End If
        Loop
      End Using

      debugInfo &= BR & BR
      For Each propertyInfo As PropertyInfo In propertyInfos
        debugInfo &= BR & propertyInfo.Name & ": " & propertyInfo.GetValue(retVal, Nothing)
      Next

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    'SendOzzy("SmoothParameters", debugInfo, Nothing) ' ----- DEBUG
    Return retVal
  End Function

  ''' <summary>
  ''' Run the smoothing program
  ''' </summary> 
  Public Shared Function SmoothContours(ByVal projectId As Long, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim parms As SmoothParameters = LoadSmoothParameters(localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      parms.project_arg = projectId

      localInfo = ""
      Dim fortranDir As String = GetFortranFolderByProject(projectId, parms.data_dir, "Smooth", localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      parms.data_dir = fortranDir

      Dim ProcArgs As String = parms.ToString
      CommonFunctions.SendOzzy(EH.GetCallerMethod() & " calc args", ProcArgs, Nothing) ' ----- DEBUG

      Dim procInfo As System.Diagnostics.ProcessStartInfo = Nothing
      Dim myErrstreamReader As System.IO.StreamReader = Nothing

      procInfo = New System.Diagnostics.ProcessStartInfo
      procInfo.FileName = Path.Combine(parms.exe_dir, parms.exe_fn)
      procInfo.Arguments = ProcArgs
      procInfo.CreateNoWindow = False
      procInfo.UseShellExecute = False
      procInfo.RedirectStandardError = True 'this worked to fix Terrace_Manip_Sub line 225 OLDOUT redirect error 
      procInfo.RedirectStandardOutput = True
      procInfo.WorkingDirectory = parms.exe_dir

      Using process As Process = process.Start(procInfo)
        process.WaitForExit() '1 * 60 * 1000 give it 1 minute
        myErrstreamReader = process.StandardError
        retVal = myErrstreamReader.ReadToEnd.ToString
      End Using

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Run the terrace generation program
  ''' </summary> 
  Public Shared Function RunTerraces(ByVal projectId As Long, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim parms As SmoothParameters = LoadSmoothParameters(localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      parms.project_arg = projectId
      parms.smooth_flag = "" ' remove smooth only

      localInfo = ""
      Dim fortranDir As String = GetFortranFolderByProject(projectId, parms.data_dir, "Terraces", localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      parms.data_dir = fortranDir

      Dim ProcArgs As String = parms.ToString
      CommonFunctions.SendOzzy(EH.GetCallerMethod() & " calc args", ProcArgs, Nothing) ' ----- DEBUG

      Dim procInfo As System.Diagnostics.ProcessStartInfo = Nothing
      Dim myErrstreamReader As System.IO.StreamReader = Nothing

      procInfo = New System.Diagnostics.ProcessStartInfo
      procInfo.FileName = Path.Combine(parms.exe_dir, parms.exe_fn)
      procInfo.Arguments = ProcArgs
      procInfo.CreateNoWindow = False
      procInfo.UseShellExecute = False
      procInfo.RedirectStandardError = True 'this worked to fix Terrace_Manip_Sub line 225 OLDOUT redirect error 
      procInfo.RedirectStandardOutput = True
      procInfo.WorkingDirectory = parms.exe_dir

      Using process As Process = process.Start(procInfo)
        process.WaitForExit() '1 * 60 * 1000 give it 1 minute
        myErrstreamReader = process.StandardError
        retVal = myErrstreamReader.ReadToEnd.ToString
      End Using

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Entry function for calculating terraces
  ''' </summary>
  Public Shared Function CalculateTerraces(ByVal projectId As Long, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Dim ozzyDebug As String = ""
    Try
      ozzyDebug &= " Calc 1 " & Now.ToString ' ----- DEBUG
      localInfo = ""
      LoadTablesForTerraces(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      ozzyDebug &= " Calc 2 " & Now.ToString ' ----- DEBUG
      localInfo = ""
      Dim terraceInfo As String = RunTerraces(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
      If Not String.IsNullOrWhiteSpace(terraceInfo) Then callInfo &= terraceInfo

      ozzyDebug &= " Calc 3 " & Now.ToString ' ----- DEBUG
      'localInfo = ""
      'myContourHelper.ConvertTerraceXml(projectId, localInfo)
      'If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      ozzyDebug &= " Calc 4 " & Now.ToString ' ----- DEBUG
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    CommonFunctions.SendOzzy(EH.GetCallerMethod(), ozzyDebug, Nothing) ' ----- DEBUG
    Return retVal
  End Function

  ''' <summary>
  ''' Copy/format data into defined tables for smoothing
  ''' Need projectname, 41 Terrace Area, 11 Farm Area??
  ''' Need 32 RIDGELINE, 33 WATERWAY, 34 DIVIDE, 42 MAX ELEVATION, 35 OUTLET??
  ''' </summary> 
  Public Shared Sub LoadTablesForTerraces(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    localInfo = ""
    LoadProjectNameForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    localInfo = ""
    LoadTerraceAreaForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    '??? need smooth xmlcontour presumably ????
    localInfo = ""
    LoadXmlContourForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    localInfo = ""
    LoadRidgelineForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    localInfo = ""
    LoadWaterwayForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    localInfo = ""
    LoadDivideForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

    localInfo = ""
    LoadHighPointForFortran(projectId, localInfo)
    If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

  End Sub

  ''' <summary>
  ''' Copy/format data into defined tables for fortran
  ''' </summary> 
  Public Shared Sub LoadRidgelineForFortran(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      'get current record from terloc
      localInfo = ""
      Dim featureList As List(Of RidgelinePackage) = myRidgelineHelper.GetRidgelinesList(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      Dim featurePkg As RidgelinePackage = Nothing
      If featureList IsNot Nothing AndAlso featureList.Count > 0 Then
        featurePkg = featureList.Item(0)
      End If

      'validate
      If featurePkg Is Nothing Then Throw New ArgumentNullException("Ridgeline Record")
      If String.IsNullOrWhiteSpace(featurePkg.ridgelineRecord.Shape) Then Throw New ArgumentNullException("Ridgeline")

      'setup legacy record
      localInfo = ""
      Dim geom As IGeometry = ConvertWkbToGeometry(featurePkg.ridgelineRecord.Shape, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim coords As String = GetCoordsStringFromGeom(geom, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim utmCoords As String = ConvertLatLonCoordsToUtm(coords, 15, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim utmGeom As IGeometry = CreateLineStringFromCoordString(utmCoords, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim fcoords As String = FormatFortranCoords(utmCoords, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      Dim oldRec As New Legacy.ProjWorkRecord 'insertion record
      With oldRec
        .PROJID = projectId
        .FEATUREID = Legacy.TerraceFeature.RIDGELINE
        .FEATURECOORDS = fcoords
        .FEATURELABEL = "Ridgeline"
        .FEATURELENGTH = CType(utmGeom, ILineString).Length
        .PRACTICETYPE = 1
      End With

      localInfo = ""
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Dim legHelpr As New Legacy.ProjWorkHelper
      'remove old records
      localInfo = ""
      legHelpr.DeleteById(projectId, usrId, Legacy.TerraceFeature.RIDGELINE, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      'add into old format 
      localInfo = ""
      legHelpr.InsertProjWork(projectId, usrId, oldRec, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Copy/format data into defined tables for fortran
  ''' </summary> 
  Public Shared Sub LoadWaterwayForFortran(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      'get current record from terloc
      localInfo = ""
      Dim featureList As List(Of WaterwayPackage) = myWaterwayHelper.GetWaterwaysList(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      featureList.Sort()
      Dim featurePkg As WaterwayPackage = Nothing
      'If featureList IsNot Nothing AndAlso featureList.Count > 0 Then
      '  featurePkg = featureList.Item(0)
      'End If

      Dim geom As IGeometry
      Dim coords As String
      Dim utmCoords As String
      Dim utmGeom As IGeometry
      Dim fcoords As String = ""
      Dim lengths As String = ""
      Dim oldRec As Legacy.ProjWorkRecord 'insertion record
      Dim numFeatures As Integer = featureList.Count
      For featIx As Integer = 0 To featureList.Count - 1
        featurePkg = featureList.Item(featIx)

        'validate
        If featurePkg Is Nothing Then Continue For ' Throw New ArgumentNullException("Waterway Record")
        If String.IsNullOrWhiteSpace(featurePkg.waterwayRecord.Shape) Then Continue For ' Throw New ArgumentNullException("Waterway")

        'setup legacy record
        localInfo = ""
        geom = ConvertWkbToGeometry(featurePkg.waterwayRecord.Shape, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        localInfo = ""
        coords = GetCoordsStringFromGeom(geom, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        localInfo = ""
        utmCoords = ConvertLatLonCoordsToUtm(coords, 15, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        localInfo = ""
        utmGeom = CreateLineStringFromCoordString(utmCoords, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        localInfo = ""
        fcoords &= "|" & FormatFortranCoords(utmCoords, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        lengths &= "|" & CType(utmGeom, ILineString).Length.ToString("F3")
      Next

      oldRec = New Legacy.ProjWorkRecord
      With oldRec
        .PROJID = projectId
        .FEATUREID = Legacy.TerraceFeature.WATERWAY
        .FEATURECOORDS = fcoords
        .FEATURELABEL = "Waterway"
        .FEATURELENGTH = lengths
        .PRACTICETYPE = 1
      End With

      localInfo = ""
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Dim legHelpr As New Legacy.ProjWorkHelper
      'remove old records
      localInfo = ""
      legHelpr.DeleteById(projectId, usrId, Legacy.TerraceFeature.WATERWAY, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      'add into old format 
      localInfo = ""
      legHelpr.InsertProjWork(projectId, usrId, oldRec, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Copy/format data into defined tables for fortran
  ''' </summary> 
  Public Shared Sub LoadDivideForFortran(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    Dim errStr As String = "0"
    Try
      'get current record from terloc
      localInfo = ""
      Dim featureList As List(Of DividePackage) = myDivideHelper.GetDividesList(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
      errStr &= " count: " & featureList.Count & " "
      featureList.Sort()
      Dim featurePkg As DividePackage = Nothing
      'If featureList IsNot Nothing AndAlso featureList.Count > 0 Then
      '  featurePkg = featureList.Item(0)
      'End If

      errStr &= " 2 "
      Dim geom As IGeometry
      Dim coords As String
      Dim utmCoords As String
      Dim utmGeom As IGeometry
      Dim fcoords As String = ""
      Dim lengths As String = ""
      Dim oldRec As Legacy.ProjWorkRecord 'insertion record
      Dim numFeatures As Integer = featureList.Count
      errStr &= " c:" & numFeatures
      For featIx As Integer = 0 To featureList.Count - 1
        featurePkg = featureList.Item(featIx)

        'validate
        If featurePkg Is Nothing Then Continue For ' Throw New ArgumentNullException("Divide Record")
        If String.IsNullOrWhiteSpace(featurePkg.divideRecord.Shape) Then Continue For ' Throw New ArgumentNullException("Divide")

        'setup legacy record
        localInfo = ""
        geom = ConvertWkbToGeometry(featurePkg.divideRecord.Shape, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        localInfo = ""
        coords = GetCoordsStringFromGeom(geom, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        localInfo = ""
        utmCoords = ConvertLatLonCoordsToUtm(coords, 15, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        localInfo = ""
        utmGeom = CreateLineStringFromCoordString(utmCoords, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        localInfo = ""
        fcoords &= "|" & FormatFortranCoords(utmCoords, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

        lengths &= "|" & CType(utmGeom, ILineString).Length.ToString("F3")
      Next

      errStr &= " 3 "
      oldRec = New Legacy.ProjWorkRecord
      With oldRec
        .PROJID = projectId
        .FEATUREID = Legacy.TerraceFeature.DIVIDE
        .FEATURECOORDS = fcoords
        .FEATURELABEL = "Divide"
        .FEATURELENGTH = lengths
        .PRACTICETYPE = 1
      End With

      localInfo = ""
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      errStr &= " 4 "
      Dim legHelpr As New Legacy.ProjWorkHelper
      'remove old records
      localInfo = ""
      legHelpr.DeleteById(projectId, usrId, Legacy.TerraceFeature.DIVIDE, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      errStr &= " 5 "
      'add into old format 
      localInfo = ""
      legHelpr.InsertProjWork(projectId, usrId, oldRec, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & errStr & " -- " & ex.Message
    End Try
  End Sub

  ''' <summary>
  ''' Copy/format data into defined tables for fortran
  ''' </summary> 
  Public Shared Sub LoadHighPointForFortran(ByVal projectId As Long, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      'get current record from terloc
      localInfo = ""
      Dim featureList As List(Of HighPointPackage) = myHighPointHelper.GetHighPointsList(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      Dim featurePkg As HighPointPackage = Nothing
      If featureList IsNot Nothing AndAlso featureList.Count > 0 Then
        featurePkg = featureList.Item(0)
      End If

      'validate
      If featurePkg Is Nothing Then Throw New ArgumentNullException("High Point Record")
      'If String.IsNullOrWhiteSpace(featurePkg.highPointRecord.Shape) Then Throw New ArgumentNullException("High Point")

      'setup legacy record
      Dim coordsPrecision As Integer = CommonVariables.CoordinatePrecision
      Dim xySplitter As String = CommonVariables.CoordinateSplitter
      Dim ptSplitter As String = CommonVariables.PointSplitter
      Dim coords As String = FormatNumber(featurePkg.highPointRecord.Longitude, coordsPrecision, , TriState.False, TriState.False) & xySplitter & _
                       FormatNumber(featurePkg.highPointRecord.Latitude, coordsPrecision, , TriState.False, TriState.False) & ptSplitter

      localInfo = ""
      Dim utmCoords As String = ConvertLatLonCoordsToUtm(coords, 15, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim utmGeom As IGeometry = CreatePointFromCoordString(utmCoords, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      localInfo = ""
      Dim fcoords As String = FormatFortranCoords(utmCoords, localInfo)
      If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      Dim oldRec As New Legacy.ProjWorkRecord 'insertion record
      With oldRec
        .PROJID = projectId
        .FEATUREID = Legacy.TerraceFeature.MAXELEVATION
        .FEATURECOORDS = fcoords
        .FEATURELABEL = "HighPoint"
        .FEATURELENGTH = CType(utmGeom, IPoint).Length
        .PRACTICETYPE = 1
      End With

      localInfo = ""
      Dim usrId As Guid = UserHelper.GetCurrentUser(localInfo).UserId
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Dim legHelpr As New Legacy.ProjWorkHelper
      'remove old records
      localInfo = ""
      legHelpr.DeleteById(projectId, usrId, Legacy.TerraceFeature.MAXELEVATION, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      'add into old format 
      localInfo = ""
      legHelpr.InsertProjWork(projectId, usrId, oldRec, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
  End Sub

  Public Shared Function FormatFortranCoords(ByVal coords As String, ByRef callInfo As String) As String
    Dim retVal As String = coords
    Try
      'EXAMPLE for non-fortran
      ' 333549.001,217344.563 333550.471,217345.222 333551.940,217345.956 333553.427,217346.859;
      'EXAMPLE for fortran
      ' 333549.001 217344.563;333550.471 217345.222;333551.940 217345.956;333553.427 217346.859

      'avoid problems with similar separators and order of replacement
      Dim uniqueA As String = "fffreddd"
      Dim uniqueB As String = "dddouggg"
      'coords = coords.Replace(" ", ";").Replace(",", " ") 'original need
      retVal = retVal.Replace(" ", uniqueA).Replace(",", uniqueB)
      retVal = retVal.Replace(uniqueA, ";").Replace(uniqueB, " ")
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  Public Shared Function DeFormatFortranCoords(ByVal coords As String, ByRef callInfo As String) As String
    Dim retVal As String = coords
    Try
      'EXAMPLE for non-fortran
      ' 333549.001,217344.563 333550.471,217345.222 333551.940,217345.956 333553.427,217346.859;
      'EXAMPLE for fortran
      ' 333549.001 217344.563;333550.471 217345.222;333551.940 217345.956;333553.427 217346.859

      'avoid problems with similar separators and order of replacement
      Dim uniqueA As String = "fffreddd"
      Dim uniqueB As String = "dddouggg"
      'coords = coords.Replace(" ", ",").Replace(";", " ") 'original need
      retVal = retVal.Replace(" ", uniqueA).Replace(";", uniqueB)
      retVal = retVal.Replace(uniqueA, ",").Replace(uniqueB, " ")
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  Public Shared Function GetFortranFolderByProject(ByVal projectId As Long, _
           ByVal defaultDir As String, ByVal subDir As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      Dim projDir As String = GetProjectFolderByProjectId(projectId, localInfo)
      If Not String.IsNullOrWhiteSpace(projDir) Then
        retVal = Path.Combine(projDir, "Fortran")
        If Not String.IsNullOrWhiteSpace(subDir) Then retVal = Path.Combine(retVal, subDir)
      Else
        retVal = Path.Combine(defaultDir, projectId)
        If Not String.IsNullOrWhiteSpace(subDir) Then retVal = Path.Combine(retVal, subDir)
      End If
      If Not String.IsNullOrWhiteSpace(retVal) Then
        If Not Directory.Exists(retVal) Then Directory.CreateDirectory(retVal)
      End If
    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  Public Shared Function ZipFortranFiles(ByVal projectId As Long, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim localInfo As String = ""
    Try
      'Dim parms As SmoothParameters = LoadSmoothParameters(localInfo)
      'If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      'parms.project_arg = projectId
      'parms.smooth_flag = "" ' remove smooth only

      localInfo = ""
      Dim fortranDir As String = GetFortranFolderByProject(projectId, "", "", localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      'parms.data_dir = fortranDir

      Dim timeStamp As String = Date.Now.ToString("s").Replace(":", "")
      retVal = Path.Combine(fortranDir, String.Format("{0}_{1}.zip", "FortranRun", timeStamp))

      'Dim ProcArgs As String = parms.ToString
      'CommonFunctions.SendOzzy(EH.GetCallerMethod() & " calc args", ProcArgs, Nothing) ' ----- DEBUG

      Dim zipFolders As New List(Of String)
      Dim fortranDI As New DirectoryInfo(fortranDir)
      For Each di As DirectoryInfo In fortranDI.GetDirectories()
        zipFolders.Add(di.FullName)
      Next

      localInfo = ""
      Dim success As Boolean = ZipDirs(zipFolders, retVal, localInfo)
      If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo
      If Not success Then
        callInfo &= "error: Zip file not created."
        retVal = ""
      End If

    Catch ex As Exception
      callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

End Class
