Imports System.IO
Imports CommonFunctions
Imports CommonVariables
Imports UploadTools
Imports NetTopologySuite.Geometries
Imports NetTopologySuite.IO

Partial Class Members_UploadData
  Inherits System.Web.UI.Page

  Private callInfo As String

  Protected Sub Page_PreInit(sender As Object, e As System.EventArgs) Handles Me.PreInit
    'had to do this in preinit for reasons unknown. page load didn't write to hidden inputs.
    Process()
  End Sub

  Protected Sub Page_Load(sender As Object, e As EventArgs)
    'see PreInit for processing
  End Sub

  Protected Sub Process()
    Dim localInfo As String = ""
    callInfo = ""
    Try
      Dim saveName As String = ""
      Dim saveFullName As String = ""
      Dim projectId As String = Request.QueryString("prjid")
      Dim uploadFolder As String = Date.Now.ToString("s").Replace(":", "")

      Dim path__1 As String = Path.Combine(GetProjectFolderByProjectId(projectId, Nothing), "UserDocuments\GISUpload", uploadFolder)
      If Not Directory.Exists(path__1) Then Directory.CreateDirectory(path__1)
      'callInfo &= " path__1: " & path__1 & "  " 'debug

      Dim files As HttpFileCollection = HttpContext.Current.Request.Files
      If files.Count < 1 Then
        callInfo &= " error: no files posted "
        Exit Try
      End If
      For index As Integer = 0 To files.Count - 1
        Dim uploadfile As HttpPostedFile = files(index)
        Dim filename As String = Path.GetFileNameWithoutExtension(uploadfile.FileName)
        Dim extension As String = Path.GetExtension(uploadfile.FileName)
        saveName = filename '& "_" & timeStamp & extension
        saveFullName = Path.Combine(path__1, saveName)
        uploadfile.SaveAs(saveFullName)
      Next

      Dim shpFileFullName As String = ""
      Dim goodCount As Integer = 0
      Dim badCount As Integer = 0
      localInfo = ""
      If Not Unzip(saveFullName, path__1, localInfo) Then
        callInfo &= " unzip error: " & localInfo & "  " : Exit Try
      Else
        Dim factory As New GeometryFactory()
        Dim di As New DirectoryInfo(path__1)
        Dim fis As FileInfo() = di.GetFiles
        For Each fi As FileInfo In fis
          Try
            Dim shapeFileDataReader As New ShapefileDataReader(fi.FullName, factory)
            shpFileFullName = fi.FullName
            goodCount += 1
            'callInfo &= " good: " & fi.FullName & "  " 'debug
          Catch ex As Exception
            badCount += 1
            'callInfo &= " bad: " & fi.FullName & "  " 'debug
          End Try
        Next
      End If

      If goodCount > 0 Then
        localInfo = ""
        Dim shpInfo As GISFieldUploadInfo = GetShapefileInfo(shpFileFullName, localInfo)
        If localInfo.Contains("error") Then callInfo &= " import: " & localInfo & "  "

        If shpInfo IsNot Nothing Then
          '<input type="hidden" id="uxShapefileName" runat="server" />
          uxShapefileName.Value = Path.Combine(uploadFolder, shpInfo.ShapefileName)
          '<input type="hidden" id="uxShapeType" runat="server" />
          uxShapeType.Value = shpInfo.shapeType
          '<input type="hidden" id="uxColCount" runat="server" />
          uxColCount.Value = shpInfo.colCount
          '<input type="hidden" id="uxRowCount" runat="server" />
          uxRowCount.Value = shpInfo.rowCount
          '<input type="hidden" id="uxColumnNames" runat="server" /> 
          uxColumnNames.Value = String.Join("|", shpInfo.Columns)
        End If
      Else
        callInfo &= " no shapefiles found "
      End If
    Catch ex As Exception
      callInfo &= ex.ToString
    End Try
    '<input type="hidden" id="uxInfo" runat="server" />
    uxInfo.Value = callInfo
    'SendEmail(ozzyEmail, GetSiteEmail, "upload", localInfo) 'debug
  End Sub

End Class
