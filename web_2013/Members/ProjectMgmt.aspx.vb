#Region "Imports"

Imports System.Reflection.MethodBase
Imports System.Web.Script.Serialization

Imports CE = CommonEnums
Imports CV = CommonVariables
Imports CF = CommonFunctions 
Imports EH = ErrorHandler
Imports MDL = TerLoc.Model

#End Region

Partial Class ProjectMgmt
  Inherits System.Web.UI.Page

#Region "Module variables"
  Dim callInfo As String = ""
  Dim dataDb As String
  Dim dataConn As String
  Dim usrName As String = ""
  Dim usrId As Guid = Guid.Empty
  Dim projectName As String = ""
  Dim projectId As Integer = -1
  Dim roleName As String = ""
  Dim roleId As Integer = -1
  Dim sessionPrjId As String = CV.SessionProjectId
  Dim sessionPgFlag As String = CV.SessionPageFlag
  Dim hostUrl As String
#End Region

  Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
    callInfo = ""
    Try
      Page.Title = CF.GetPageTitle("Project Mgmt")
      hostUrl = CF.GetSiteName
      If False = HttpContext.Current.User.Identity.IsAuthenticated Then Response.Redirect("Login")
      CType(Master.FindControl("uxHiddenPageFlag"), HiddenField).Value = CE.MenuItemValues.prjmgmt.ToString

      'Need this info to get existing data 
      Dim usr As MDL.User = MDL.UserHelper.GetCurrentUser(callInfo)
      If callInfo.ToLower.Contains("error") Then uxInfo.Text &= Environment.NewLine & "Page load error: " & callInfo & ";"
      usrId = usr.UserId
      If usr.UserId = Guid.Empty Then Response.Redirect("Login")
      usrName = MDL.UserHelper.GetUserFullName(usr, Nothing)

      If Not Page.IsPostBack Then
        CType(Master.FindControl("uxHiddenProjectId"), HiddenField).Value = CF.NullSafeString(Session(sessionPrjId), "")
      End If

      OnPageLoad()
    Catch ex As Exception
      uxInfo.Text &= String.Format("{0}: {1}", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.ToString)
    End Try
  End Sub

  Protected Sub OnPageLoad()
    Try
      If Not Page.IsPostBack And Not Page.IsCallback Then
        Dim LoadInfo As String = ""
        LoadInfo += LoadProjects()
        LoadInfo += LoadProjectsInfo()
        paramHolder.Text = LoadInfo
      End If
    Catch ex As Exception
      uxInfo.Text = String.Format("{0}: {1}", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.ToString)
    End Try
  End Sub

  Public Function LoadProjects() As String
    Dim retVal As String = ""
    callInfo = ""
    Dim localInfo As String = ""
    Dim html As New StringBuilder("<script type='text/javascript' id='loadprojects'>" & Environment.NewLine)
    Dim projects As New MDL.ReturnProjectsStructure
    Try
      Dim serializer As New JavaScriptSerializer
      Try
        projects = MDL.ProjectHelper.GetProjects(usrId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= localInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.ToString)
      End Try

      Dim projectsJson As String
      Dim projCount As Integer = projects.projects.Count
      If projCount = 0 Then
        uxNoProjects.Text = "You don't have any projects created. Use the button to create a new project."
        'NEW WHEN ROLES: uxNoProjects.Text = "You don't have any projects created or assigned to you. Use the button to create a new project."
        projectsJson = "{""d"":{""__type"":""Projects+ReturnProjectsStructure""," & serializer.Serialize(projects).TrimStart("{"c) & "}"
      Else
        localInfo = ""
        projectsJson = "{""d"":{""__type"":""Projects+ReturnProjectsStructure""," & serializer.Serialize(projects).TrimStart("{"c) & "}"
      End If
      html.Append("projectsJson=" & projectsJson & ";" & Environment.NewLine)

    Catch ex As Exception
      callInfo &= String.Format("{0}: {1}", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.ToString)
      CF.SendEmail("athertonk@missouri.edu", "no-reply@" & CF.GetSiteName(), "Load Projects error", callInfo, Nothing)
    End Try
    html.Append("var projectinfostuff='" & callInfo.Replace(Environment.NewLine, "") & "';" & Environment.NewLine)
    html.Append("</script>")
    uxInfo.Text &= callInfo
    retVal = html.ToString
    Return retVal
  End Function

  Public Function LoadProjectsInfo() As String
    Dim retVal As String = ""
    callInfo = ""
    Dim localInfo As String = ""
    Dim html As New StringBuilder("<script type='text/javascript' id='loadprojectsinfo'>" & Environment.NewLine)
    Try
      Dim states As List(Of String) = StatesCountiesEtc.GetStates(localInfo)
      html.Append("states='" & String.Join("|", states) & "';" & Environment.NewLine)

    Catch ex As Exception
      callInfo &= String.Format("{0}: {1}", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.ToString)
    End Try
    html.Append("</script>")
    uxInfo.Text &= callInfo
    retVal = html.ToString
    Return retVal
  End Function

  Protected Sub uxOpenProject_Click(sender As Object, e As System.EventArgs) Handles uxOpenProject.Click
    Try
      'Master hidden fields not saved on page load, must use session or cookie
      Session(sessionPrjId) = CType(Master.FindControl("uxHiddenProjectId"), HiddenField).Value
      Response.Redirect("/ProjectHome")
    Catch ex As Exception
      uxInfo.Text = String.Format("{0}: {1}", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.ToString)
    End Try
  End Sub

  Protected Sub uxDeleteProject_Click(sender As Object, e As System.EventArgs) Handles uxDeleteProject.Click

    Dim LoadInfo As String = ""
    Try
      projectId = CInt(CType(Master.FindControl("uxHiddenProjectId"), HiddenField).Value)
      If projectId > 0 Then
        callInfo = ""
        MDL.ProjectHelper.DeleteProject(projectId, callInfo)
        If callInfo.ToLower.Contains("error") Then LoadInfo &= Environment.NewLine & "Delete error: " & callInfo & ";"
      End If
    Catch ex As Exception
      LoadInfo &= Environment.NewLine & "Delete Project error: " & ex.Message & ";"
    End Try

    LoadInfo &= LoadProjects()
    paramHolder.Text = LoadInfo
  End Sub

End Class

