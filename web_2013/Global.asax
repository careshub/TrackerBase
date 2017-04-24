<%@ Application Language="VB" %>
<%@ Import Namespace="System.Web.Routing" %>
<%@ Import Namespace="CF=CommonFunctions" %>
<script RunAt="server">
  Dim callInfo As String = ""
  
  Sub Application_Start(ByVal sender As Object, ByVal e As EventArgs)
    ' Code that runs on application startup
    Try
      RegisterRoutes(RouteTable.Routes)
    Catch ex As Exception
      Application("app") = ex.Message
    End Try
  End Sub
    
  Sub Application_End(ByVal sender As Object, ByVal e As EventArgs)
    ' Code that runs on application shutdown
  End Sub
  
  Sub Application_Error(ByVal sender As Object, ByVal e As EventArgs)
    ' Code that runs when an unhandled error occurs
    ' Catch exception and save to session variable, for displaying in ErrorPage.aspx
    Try
      Dim ex As Exception = Server.GetLastError()
    Finally
    End Try
    'Try
    '  ' Session object is not always available, so use try finally block.
    '  ' This will prevent recursive loop.
    '  Session("EncounteredException") = ex
    'Finally
    '  ' To use the Session variable in ErrorPage.aspx we must clear the error and then manualy redirect to the ErrorPage.aspx.
    '  ' Because we manualy redirect, we don't use the "customErrors" tag in the Web.config
    '  Server.ClearError()
    '  Response.Redirect("~/Account/ErrorPage.aspx")
    'End Try
  End Sub

  Sub Session_Start(ByVal sender As Object, ByVal e As EventArgs)
    ' Code that runs when a new session is started
  End Sub

  Sub Session_End(ByVal sender As Object, ByVal e As EventArgs)
    ' Code that runs when a session ends. 
    ' Note: The Session_End event is raised only when the sessionstate mode
    ' is set to InProc in the Web.config file. If session mode is set to StateServer 
    ' or SQLServer, the event is not raised.
  End Sub
  
  Sub RegisterRoutes(routes As RouteCollection)
    'Allows change of url
    routes.MapPageRoute("About", "About", "~/About.aspx")
    routes.MapPageRoute("Login", "Login", "~/Account/Login.aspx")
    routes.MapPageRoute("Register", "Register", "~/Account/Register.aspx")
    routes.MapPageRoute("ChangePassword", "ChangePassword", "~/Account/ChangePassword.aspx")
    routes.MapPageRoute("RecoverPassword", "RecoverPassword", "~/Account/RecoverPassword.aspx")
    routes.MapPageRoute("ProjectHome", "ProjectHome", "~/Members/ProjectHome.aspx")
    routes.MapPageRoute("ProjectMgmt", "ProjectMgmt", "~/Members/ProjectMgmt.aspx")
    routes.MapPageRoute("Print", "MapTemplate", "~/MapTemplate.aspx")
    routes.MapPageRoute("Report", "Report", "~/Members/TerraceRunReport.aspx")
  End Sub

</script>
