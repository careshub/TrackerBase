Imports Microsoft.VisualBasic
Imports System.Data

''' <summary>
''' DO NOT USE global variables in this class. All variables declared in this class are meant to show
''' a common repository of often used variables. Variables in this class will be assigned to a locally
''' declared variable and will be passed to other functions.
''' WARNING BEGIN
''' MANDATORY USAGE POLICY: DECLARE IN CALLING PROGRAM THUS:
''' 
''' Dim myLocalVariable as VariableType = CommonVariables.SharedVariableName
''' 
''' Use myLocalVariable inside the calling function.  Do not use SharedVariableName directly in the 
''' calling program.
''' </summary> 
Public Class CommonEnums
  ''' <summary>
  ''' Menu tab ids
  ''' </summary>
  Public Enum MenuItemValues
    prjmgmt
    stepone
    steptwo
    stepthree
    stepfour
    about
  End Enum
End Class

''' <summary>
''' DO NOT USE global variables in this class. All variables declared in this class are meant to show
''' a common repository of often used variables. Variables in this class will be assigned to a locally
''' declared variable and will be passed to other functions.
''' WARNING BEGIN
''' MANDATORY USAGE POLICY: DECLARE IN CALLING PROGRAM THUS:
''' 
''' Dim myLocalVariable as VariableType = CommonVariables.SharedVariableName
''' 
''' Use myLocalVariable inside the calling function.  Do not use SharedVariableName directly in the 
''' calling program.
''' </summary> 
Public Class CommonVariables
  ''' <summary>
  ''' Uppercase strings used for True values of a boolean
  ''' </summary>
  ''' <remarks>Convert value to check to uppercase, then test indexOf</remarks>
  Public Shared ReadOnly BooleanTrueValues() As String = {"T", "TRUE", "Y", "YES"}
  Public Shared ReadOnly HtmlLineBreak As String = "<br />"

  Public Shared ReadOnly ozzyEmail As String = "athertonk@missouri.edu"
  Public Shared ReadOnly siteEmailBase As String = "no-reply@"

  Public Shared ReadOnly spaceSeparator As Char() = New Char(0) {" "}
  Public Shared ReadOnly commaSeparator As Char() = New Char(0) {","}
  Public Shared ReadOnly pipeSeparator As Char() = New Char(0) {"|"}

#Region "Session/Cookies"
  Public Shared ReadOnly SessionProjectId As String = "ProjectId"
  Public Shared ReadOnly SessionPageFlag As String = "PageFlag"
  Public Shared ReadOnly SessionUserName As String = "UserName"
#End Region

#Region "GIS constants"
  ''' <summary>
  ''' String to separate one coordinate from another when using coordinates as a string
  ''' </summary>
  Public Shared ReadOnly CoordinateSplitter As String = ","
  ''' <summary>
  ''' String to separate one coordinate pair from another when using coordinates as a string
  ''' </summary>
  Public Shared ReadOnly PointSplitter As String = " "
  ''' <summary>
  ''' String to separate one geometry part from another when using coordinates as a string
  ''' </summary>
  Public Shared ReadOnly GeometryPartSplitter As String = "|"
  ''' <summary>
  ''' String to separate one geometry from another when using coordinates as a string
  ''' </summary>
  Public Shared ReadOnly GeometrySplitter As String = "||"

  ''' <summary>
  ''' Decimal precision for coordinate values
  ''' </summary>
  Public Shared ReadOnly CoordinatePrecision As Integer = 6
  ''' <summary>
  ''' Decimal precision for size attributes of land management units
  ''' </summary>
  Public Shared ReadOnly LandManagementUnitSizePrecision As Integer = 1
  Public Shared ReadOnly FeetToMetersMultiplier As Double = 0.3048
  Public Shared ReadOnly AcresToSquareMetersMultiplier As Double = 4046.8564224
  Public Shared ReadOnly SquareMetersToSquareFeetMultiplier As Double = 10.763910417
#End Region

#Region "Connection constants"
  'See site-specific class for site database names and connection strings
  Public Shared ReadOnly MapDataConnStr As String = ConfigurationManager.ConnectionStrings("MapDataConnString").ConnectionString
  Public Shared ReadOnly ProjectProductionSchema As String = "TerLoc"
  Public Shared ReadOnly AppGuid As Guid = New Guid("072F02F9-86D4-4D17-9662-67F45DBC3183")
  Public Shared ReadOnly AppUserTable As String = "[dbo].[Terrace_Users]"
#End Region

#Region "Folder constants"
  Public Shared ReadOnly BaseProjectFolders() As String = {"Archive", "UserDocuments", "UserDocuments\Downloads", "UserDocuments\Reviewer", "SupportFiles"}
  Public Shared ReadOnly ProjectArchiveFolder As String = "Archive" ' holds uploaded files
  Public Shared ReadOnly ProjectUserFolder As String = "UserDocuments" ' holds user files (uploaded and created)
  Public Shared ReadOnly ProjectDownloadsFolder As String = "UserDocuments\Downloads" ' holds files for reviewer to download
  Public Shared ReadOnly ProjectReviewerFolder As String = "UserDocuments\Reviewer" ' holds files for reviewer to access
  Public Shared ReadOnly ProjectSupportFolder As String = "SupportFiles" ' holds files nec. to run program, e.g. rusle gdb
#End Region

  ''' <summary>
  ''' Tables from which to delete project records - update as needed
  ''' Use this in places where cascade delete is not desired
  ''' </summary>
  Public Shared ReadOnly DeleteProjectDatumTables() As String = { _
            "Operation", "LandManagementUnit"}


#Region "Shapefile attributes"

  ''' <summary>
  ''' Datum definition for use in exporting shapefiles
  ''' </summary>
  Public Shared ProjectionDef As String = "GEOGCS[""GCS_North_American_1983"",DATUM[""D_North_American_1983"",SPHEROID[""GRS_1980"",6378137.0,298.257222101]],PRIMEM[""Greenwich"",0.0],UNIT[""Degree"",0.0174532925199433]]"

  '10 character limit on shp names!!!!

  'http://webhelp.esri.com/arcgisdesktop/9.3/index.cfm?TopicName=Geoprocessing_considerations_for_shapefile_output
  'shapefile max length: text = 254; oid = 9; short = 4; long = 9; float/dbl = 13; date = 8

#Region "Terrace Area"

  ''' <summary>
  ''' Lookup hash with indices as follows: {Name, Type, Length}
  ''' </summary>
  Public Shared ReadOnly TerraceAreaShpFields(,) As Object = { _
      {"AreaOid", GetType(Long), 9} _
  }

  ''' <summary>
  ''' Field lookup for exporting
  ''' </summary> 
  Public Shared LMUToSHP(,) As String = { _
      {"ObjectID", "AreaOid"} _
  }

  ''' <summary>
  ''' Field lookup for importing
  ''' </summary>
  Public Shared SHPToLMU(,) As String = { _
      {"FID", "ObjectID"} _
  }

#End Region

#Region "Contour"

  ''' <summary>
  ''' Lookup hash with indices as follows: {Name, Type, Length}
  ''' </summary>
  Public Shared ReadOnly ContourShpFields(,) As Object = { _
      {"ContourOid", GetType(Long), 9} _
  }

  ''' <summary>
  ''' Field lookup for exporting
  ''' </summary> 
  Public Shared ContourToSHP(,) As String = { _
      {"ObjectID", "ContourOid"} _
  }

#End Region

#Region "Divide"

  ''' <summary>
  ''' Lookup hash with indices as follows: {Name, Type, Length}
  ''' </summary>
  Public Shared ReadOnly DivideShpFields(,) As Object = { _
      {"DivideOid", GetType(Long), 9} _
    , {"Ordinal", GetType(Short), 4} _
  }

  ''' <summary>
  ''' Field lookup for exporting
  ''' </summary> 
  Public Shared DivideToSHP(,) As String = { _
      {"ObjectID", "DivideOid"} _
    , {"Ordinal", "Ordinal"} _
  }

#End Region

#Region "High Point"

  ''' <summary>
  ''' Lookup hash with indices as follows: {Name, Type, Length}
  ''' </summary>
  Public Shared ReadOnly HighPointShpFields(,) As Object = { _
      {"ElevOid", GetType(Long), 9} _
    , {"Elevation", GetType(Single), 9} _
  }

  ''' <summary>
  ''' Field lookup for exporting
  ''' </summary> 
  Public Shared HighPointToSHP(,) As String = { _
      {"ObjectID", "ElevOid"} _
    , {"Elevation", "Elevation"} _
  }

#End Region

#Region "Ridgeline"

  ''' <summary>
  ''' Lookup hash with indices as follows: {Name, Type, Length}
  ''' </summary>
  Public Shared ReadOnly RidgelineShpFields(,) As Object = { _
      {"RidgeOid", GetType(Long), 9} _
  }

  ''' <summary>
  ''' Field lookup for exporting
  ''' </summary> 
  Public Shared RidgelineToSHP(,) As String = { _
      {"ObjectID", "RidgeOid"} _
  }

#End Region

#Region "Waterway"

  ''' <summary>
  ''' Lookup hash with indices as follows: {Name, Type, Length}
  ''' </summary>
  Public Shared ReadOnly WaterwayShpFields(,) As Object = { _
      {"WaterOid", GetType(Long), 9} _
    , {"Ordinal", GetType(Short), 4} _
  }

  ''' <summary>
  ''' Field lookup for exporting
  ''' </summary> 
  Public Shared WaterwayToSHP(,) As String = { _
      {"ObjectID", "WaterOid"} _
    , {"Ordinal", "Ordinal"} _
  }

#End Region

#Region "Terrace"

  ''' <summary>
  ''' Lookup hash with indices as follows: {Name, Type, Length}
  ''' </summary>
  Public Shared ReadOnly TerraceShpFields(,) As Object = { _
      {"TerraceOid", GetType(Long), 9} _
    , {"FeatureID", GetType(Short), 4} _
    , {"Type", GetType(String), 50} _
    , {"Scenario", GetType(Short), 4} _
    , {"Ordinal", GetType(Short), 4} _
    , {"Custom", GetType(Short), 4} _
  }

  ''' <summary>
  ''' Field lookup for exporting
  ''' </summary> 
  Public Shared TerraceToSHP(,) As String = { _
      {"ObjectID", "TerraceOid"} _
    , {"FeatureID", "FeatureID"} _
    , {"Type", "Type"} _
    , {"ScenarioType", "Scenario"} _
    , {"Ordinal", "Ordinal"} _
    , {"Custom", "Custom"} _
  }

#End Region

#End Region

End Class
