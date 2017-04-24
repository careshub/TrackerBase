Imports Microsoft.VisualBasic
Imports System.Configuration.ConfigurationManager
Imports System.Web

''' <summary>
''' This module must use the url host properties to determine if the current site is dev, demo or live.
'''  
''' Class usage:
''' This class SHALL NOT contain global variables. All variables declared in this class are meant to show
''' a common repository of often used variables. Variables in this class will be assigned to a locally
''' declared variable and will be passed to other functions.
''' WARNING BEGIN
''' MANDATORY USAGE POLICY: DECLARE IN CALLING PROGRAM THUS:
''' 
''' Dim myLocalVariable as VariableType = SiteSpecific.SharedVariableName
''' 
''' Use myLocalVariable inside the calling function.  Do not use SharedVariableName directly in the 
''' calling program.
''' 
''' These properties are also obtainable via CommonFunctions functions (this is actually the preferred usage).
''' </summary> 
Public Class SiteSpecific

  Private Shared thisSite As String = HttpContext.Current.Request.Url.Host

#Region "Site specific"

  ''' <summary>
  ''' Returns host name of current site
  ''' </summary> 
  Public Shared ReadOnly Property SiteName() As String
    Get
      Return thisSite
    End Get
  End Property

  ''' <summary>
  ''' Returns prefix for site based on url host name
  ''' </summary> 
  Public Shared ReadOnly Property SiteSubdomain() As String
    Get
      If HttpContext.Current.Request.Url.HostNameType <> UriHostNameType.Dns Then Return ""
      Dim retVal As String = ""
      Dim parts() As String = thisSite.Replace("www.", "").Replace("terrace", "").Split(New Char() {"."c}, StringSplitOptions.RemoveEmptyEntries)
      For idx As Integer = 0 To parts.Length - 3
        retVal &= parts(idx) & "."
      Next
      retVal = retVal.Trim.TrimEnd(New Char() {"."})
      Return retVal
    End Get
  End Property

  ''' <summary>
  ''' Returns type of prefix for site based on url host name
  ''' </summary> 
  Public Shared ReadOnly Property SiteType() As String
    Get
      Dim retVal As String
      Select Case True
        Case thisSite.ToLower.Contains("demo.") : retVal = "demo"
        Case thisSite.ToLower.Contains("dev.") : retVal = "dev"
        Case Else : retVal = "www"
      End Select
      Return retVal
    End Get
  End Property

  ''' <summary>
  ''' Returns name of database holding main data
  ''' </summary> 
  Public Shared ReadOnly Property ProjectSchemaName() As String
    Get
      Dim retVal As String = "terloc"
      Return retVal
    End Get
  End Property

  ''' <summary>
  ''' Returns name of database holding main data
  ''' </summary> 
  Public Shared ReadOnly Property BaseDatabaseName() As String
    Get
      Dim retVal As String = "terloc"
      Return retVal
    End Get
  End Property

  ''' <summary>
  ''' Returns name of database holding membership data
  ''' </summary> 
  Public Shared ReadOnly Property AspNetDatabaseName() As String
    Get
      Dim retVal As String = "mmptrackerdev"
      Return retVal
    End Get
  End Property

  ''' <summary>
  ''' Returns connection string to database holding main data
  ''' </summary> 
  Public Shared ReadOnly Property BaseDatabaseConnString() As String
    Get
      Dim retVal As String = ConnectionStrings("TerraceDataConnString").ConnectionString
      Return retVal
    End Get
  End Property

  ''' <summary>
  ''' Returns connection string to database holding membership data
  ''' </summary> 
  Public Shared ReadOnly Property AspNetDatabaseConnString() As String
    Get
      Dim retVal As String = ConnectionStrings("AspNetConnString").ConnectionString
      Return retVal
    End Get
  End Property

  ''' <summary>
  ''' Returns name of folder that contains site project folders
  ''' </summary> 
  Public Shared ReadOnly Property ProjectFolderBase() As String
    Get
      Dim retVal As String = "C:\Workdata\terloc\TerraceProjectFolders"
      Return retVal
    End Get
  End Property

#End Region

End Class
