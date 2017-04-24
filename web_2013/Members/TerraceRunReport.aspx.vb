Option Explicit On
Option Strict On

#Region "Imports"
'Imports Microsoft.VisualBasic
'Imports System
'Imports System.Collections.Generic
'Imports System.ComponentModel
'Imports System.Data
'Imports System.Data.SqlClient
'Imports System.ServiceModel
'Imports System.Web.Script.Serialization
'Imports System.Xml
'Imports System.Xml.Linq

Imports System.IO
Imports System.Reflection.MethodBase
Imports EH = ErrorHandler
Imports CommonFunctions
Imports CommonVariables
Imports TerLoc.Model
 
#End Region

Partial Class TerraceRunReport
  Inherits System.Web.UI.Page

#Region "Module variables"
  Dim BR As String = HtmlLineBreak

  Dim projectNotFoundMsg As String = "*** Project ID is not found. Please reselect a project from Project Mgmt. ***"
  Dim noErrorsMsg As String = "*** No errors found for this project. ***"
  Dim noReportMsg As String = "*** No report found for this project. ***"
  Dim accessDeniedMsg As String = "*** You do not have access to this project. ***"
  Dim projectId As Long = -1

#End Region

  Public Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
    Dim localInfo As String = ""
    Try
      uxInfo.InnerHtml = ""
      CType(Master.FindControl("uxSiteHeader"), HtmlControl).Visible = False

      'projectId = NullSafeLong(Session(SessionProjectId), -1) 'not reliable?
      projectId = NullSafeLong(Request.QueryString("project"), -1)

      If projectId < 1 Then
        uxInfo.InnerHtml = projectNotFoundMsg
        uxErrors.Visible = False
        uxReport.Visible = False
        uxOpenReport.Visible = False
        Return
      End If

      localInfo = ""
      Dim usr As User = UserHelper.GetCurrentUser(localInfo)
      If localInfo.ToLower.Contains("error") Then uxInfo.InnerHtml &= localInfo

      localInfo = ""
      Dim project As Project = ProjectHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then uxInfo.InnerHtml &= localInfo

      If usr.UserId <> project.OwnerGuid Then
        uxInfo.InnerHtml = accessDeniedMsg
        uxErrors.Visible = False
        uxReport.Visible = False
        uxOpenReport.Visible = False
        Return
      End If

      LoadErrors()
      LoadReport()
      MakeFortran()

      If (uxErrors.Visible = False Or String.IsNullOrWhiteSpace(uxErrors.Text)) And _
        (uxReport.Visible = False Or String.IsNullOrWhiteSpace(uxReport.Text)) Then
        uxOpenReport.Visible = False
      End If
    Catch ex As Exception
      uxInfo.InnerHtml &= BR & String.Format(" {0} error: {1} ", "Page_Load", ex.Message)
    End Try
  End Sub

  Protected Sub LoadErrors()
    Dim myTerraceErrorHelper As New TerraceErrorHelper
    Dim pkg As TerraceErrorPackageList
    Dim localInfo As String = ""
    Try
      pkg = myTerraceErrorHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then uxInfo.InnerHtml &= localInfo
      If pkg Is Nothing OrElse pkg.terraceErrors Is Nothing OrElse pkg.terraceErrors.Count < 1 Then
        uxErrorsInfo.InnerHtml = noErrorsMsg
        uxErrors.Visible = False
        Return
      End If

      Dim reportText As String = ""
      Dim errorsInfo As String = "Errors"
      Dim rec As TerraceErrorRecord = Nothing
      For pkgIx As Integer = 0 To pkg.terraceErrors.Count - 1
        rec = pkg.terraceErrors(pkgIx).terraceErrorRecord
        reportText &= rec.ErrorMessage & Environment.NewLine
      Next
      If rec IsNot Nothing Then errorsInfo &= " run date: " & rec.FortranDate.ToString
      uxErrorsInfo.InnerHtml = errorsInfo
      uxErrors.Text = reportText

    Catch ex As System.Exception
      uxInfo.InnerHtml &= BR & String.Format(" {0} error: {1} ", "Load Errors", ex.Message)
    End Try
  End Sub

  Protected Sub LoadReport()
    Dim myTerraceReportHelper As New TerraceReportHelper
    Dim pkg As TerraceReportPackageList
    Dim localInfo As String = ""
    Try
      pkg = myTerraceReportHelper.Fetch(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then uxInfo.InnerHtml &= localInfo
      If pkg Is Nothing OrElse pkg.terraceReports Is Nothing OrElse pkg.terraceReports.Count < 1 Then
        uxReportInfo.InnerHtml = noReportMsg
        uxReport.Visible = False
        Return
      End If

      Dim reportText As String = ""
      Dim reportInfo As String = "Report"
      Dim rec As TerraceReportRecord = Nothing
      For pkgIx As Integer = 0 To Math.Min(0, pkg.terraceReports.Count - 1) 'just want first one if there
        rec = pkg.terraceReports(pkgIx).terraceReportRecord
        reportText &= rec.Report
      Next
      If rec IsNot Nothing Then reportInfo &= " run date: " & rec.FortranDate.ToString
      uxReportInfo.InnerHtml = reportInfo
      uxReport.Text = reportText

    Catch ex As System.Exception
      uxInfo.InnerHtml &= BR & String.Format(" {0} error: {1} ", "Load Report", ex.Message)
    End Try
  End Sub

  Protected Sub MakeFortran() 
    Dim localInfo As String = ""
    Try
      Dim fortranZipFileName As String = Fortran.ZipFortranFiles(projectId, localInfo)
      If localInfo.ToLower.Contains("error") Then uxInfo.InnerHtml &= localInfo

      'C:\Workdata\terloc\TerraceProjectFolders\4b3c8e85-0c63-4ad2-8a56-2a25deb839ab\Fortran\FortranRun_2016-06-20T104716.zip
      Dim folderBase As String = "C:\Workdata\terloc\TerraceProjectFolders\"
      Dim stripped As String = fortranZipFileName.Substring(folderBase.Length)
      stripped = "/UserFolders/" & stripped.Replace("\", "/")

      uxFortranRunZip.HRef = stripped
      uxFortranRunZip.InnerText = Path.GetFileNameWithoutExtension(fortranZipFileName)
    Catch ex As System.Exception
      uxInfo.InnerHtml &= BR & String.Format(" {0} error: {1} ", "Make Fortran Download", ex.Message)
    End Try
  End Sub

  Protected Sub uxOpenReport_Click(sender As Object, e As EventArgs) Handles uxOpenReport.Click
    Dim fileName As String = "TerraceRun_" & GetProjectNameByProjectId(projectId, Nothing) & ".txt"
    Dim reportText As String = ""
    Dim callInfo As String = ""
    Dim localInfo As String = ""

    ' Get the report text.
    'Dim myTerraceReportHelper As New TerraceReportHelper
    'Dim pkg As TerraceReportPackageList
    'Try
    '  pkg = myTerraceReportHelper.Fetch(projectId, localInfo)
    '  If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
    '  reportText = pkg.terraceReports(0).terraceReportRecord.Report
    'Catch ex As Exception
    '  callInfo &= String.Format(" {0} error: {1} ", ErrorHandler.GetCallerMethod(), ex.ToString)
    'End Try
    If uxErrorsInfo.InnerHtml <> noErrorsMsg Then reportText &= "Errors: " & Environment.NewLine & uxErrors.Text & Environment.NewLine
    If uxReportInfo.InnerHtml <> noReportMsg Then reportText &= "Report: " & Environment.NewLine & uxReport.Text

    ' Write a file with the text.
    Dim filePath As String = GetProjectFolderByProjectId(projectId, localInfo)
    Dim fullFile As String = Path.Combine(filePath, fileName)
    'Dim fi As New FileInfo(fullFile)
    'If fi.Exists Then fi.Delete()
    Using outputFile As New StreamWriter(fullFile)
      outputFile.WriteLine(reportText)
    End Using

    ' Open the file.
    Try
      If File.Exists(fullFile) Then
        Response.ContentType = "application/text"
        Response.AppendHeader("Content-Disposition", "attachment; filename=" & fullFile)
        Response.WriteFile(fullFile)
        Response.Flush()
        Response.Close()
      Else
        callInfo &= "Report file does not exist for download."
      End If

    Catch ex As System.Exception
      callInfo &= String.Format(" {0} error: {1} ", EH.GetCallerMethod(), ex.ToString)
    End Try
    If Not String.IsNullOrWhiteSpace(callInfo) Then
      uxInfo.InnerHtml = callInfo
    End If
  End Sub

End Class
