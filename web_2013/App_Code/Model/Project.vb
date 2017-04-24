Option Explicit On
Option Strict On

#Region "Imports"
Imports Microsoft.VisualBasic
Imports System.Data
Imports System.Data.SqlClient
Imports System.IO
Imports System.Reflection
Imports System.Reflection.MethodBase
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Json
Imports System.Web.Script.Serialization
Imports CF = CommonFunctions
Imports CV = CommonVariables
Imports GTA = GIS.GISToolsAddl
Imports Mapping
Imports EH = ErrorHandler

#End Region

Namespace TerLoc.Model

  Public Class Project
    Private NULL_VALUE As Short = -1

    Public ObjectID As Integer = NULL_VALUE
    Public Name As String = ""
    Public Folder As String = ""
    Public OwnerGuid As Guid = Guid.Empty
    Public Created As DateTime = Nothing
    Public CreatorGuid As Guid = Guid.Empty
    Public Edited As DateTime = Nothing
    Public EditorGuid As Guid = Guid.Empty
    'not in db
    Public Owner As String = ""
    Public Creator As String = ""
    Public Editor As String = ""
  End Class

  '<Serializable()> _
  'Public Class ProjectPackage
  '  Public projectRecord As Project
  '  Public datumRecord As MDL.ProjectDatum
  '  Public operationRecord As MDL.OperationRecord
  '  Public operationDatum As MDL.ProjectDatum
  '  Public Role As RoleStructure
  '  Public info As String 'Use for error messages, stack traces, etc.
  'End Class

  '<Serializable()> _
  'Public Class ProjectPackageList
  '  Public projects As New List(Of ProjectPackage)
  '  Public info As String 'Use for error messages, stack traces, etc.
  'End Class

  ''' <summary>
  ''' Bounding box of all relevant geometries for the project. Used in fortran.
  ''' </summary>
  Public Class ProjectBounds
    Private NULL_VALUE As Integer = -1
    Public MinX As Integer = NULL_VALUE
    Public MaxX As Integer = NULL_VALUE
    Public MinY As Integer = NULL_VALUE
    Public MaxY As Integer = NULL_VALUE

    Public Function Format() As String
      Return String.Format("MinX {0}, MaxX {1}, MinY {2}, MaxY {3}", MinX, MaxX, MinY, MaxY)
    End Function
  End Class

  ''' <summary>
  ''' Updateable attributes for a project that can be selectively used wherever updates are performed
  ''' </summary>
  ''' <remarks></remarks>
  Public Class EditProject
    Public Name As String = "" 'Project table
    Public Notes As String = "" 'ProjectDatum table
    Public OperationId As Long = -1 'Operation/ProjectDatum table
    Public OperationName As String = ""
    Public Address As String = ""
    Public City As String = ""
    Public Zip As String = ""
    Public Contact As String = ""
    Public ContactOfficePhone As String = ""
    Public ContactHomePhone As String = ""
    Public ContactEmail As String = ""
    Public StartCalYear As Integer = -1
    Public StartCalMonth As Integer = -1
    Public State As String = ""
    Public County As String = ""
  End Class

  Public Class ReturnProject
    Public Project As ProjectStructure
    Public OperationDatum As ProjectDatum
    Public Operation As OperationRecord
    Public Role As RoleStructure
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class ProjectStructure
    Public ObjectID As Integer
    Public Name As String = ""
    Public Folder As String = ""
    Public OwnerGuid As Guid = Guid.Empty
    Public Owner As String 'name of project owner
    Public Created As DateTime
    Public CreatorGuid As Guid = Guid.Empty
    Public Creator As String = "" 'name of datum creator
    Public Edited As DateTime
    Public EditorGuid As Guid = Guid.Empty
    Public Editor As String = "" 'name of last datum editor
    Public info As String = "" 'Use for error messages, stack traces, etc.
  End Class

  Public Class RoleStructure
    Public ObjectID As Integer
    Public Created As DateTime
    Public CreatorGuid As Guid = Guid.Empty
    Public Creator As String 'name of role creator
    Public Edited As DateTime
    Public EditorGuid As Guid = Guid.Empty
    Public Editor As String = "" 'name of last role editor
    Public EmailSent As Boolean
    Public RoleID As Integer
    Public RoleName As String = ""
    Public info As String = "" 'Use for error messages, stack traces, etc.
  End Class

  Public Class ReturnProjectsStructure
    Public projects As List(Of ReturnProject)
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class ProjectHelper

#Region "Module declarations"
    Private Shared serializer As New JavaScriptSerializer
    Private Shared dataSchema As String = CF.GetDataSchemaName
    Private Shared dataConn As String = CF.GetBaseDatabaseConnString
    Private Shared myTerraceAreaHelper As New FieldHelper

#End Region

#Region "Delete"

    ''' <summary>
    ''' Delete record from Project table matching ProjectId.
    ''' </summary>
    Public Shared Function DeleteProjectRecordByProjectId(ByVal projectId As Long, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = "DELETE FROM " & dataSchema & ".Project WHERE ObjectId=@projectId "
            cmd.Parameters.Add("@projectId", SqlDbType.BigInt).Value = projectId

            If conn.State = ConnectionState.Closed Then conn.Open()
            retVal = cmd.ExecuteNonQuery()
          End Using
        End Using

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Sub DeleteProject(ByVal projectId As Long, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId

        Dim project As DataRow = GetProjectRow(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'Dim projectName As String = CF.NullSafeString("Name")
        Dim projectFolder As String = CF.NullSafeString(project.Item("Folder"), "")

        'Dim sSelect As String = ""
        'sSelect = "SELECT ObjectID, [Name], Folder, OwnerGuid FROM " & dataSchema & ".Project where ObjectID = " & projectId & ""

        'Using conn As New SqlConnection(dataConn)
        '  Using cmd As SqlCommand = conn.CreateCommand()
        '    cmd.CommandText = sSelect
        '    If conn.State = ConnectionState.Closed then conn.Open()
        '    Using dataReadr As SqlDataReader = cmd.ExecuteReader
        '      While dataReadr.Read()
        '        If dataReadr(0) IsNot Nothing Then
        '          projectName = dataReadr(1).ToString
        '          projectFolder = dataReadr(2).ToString
        '        End If
        '      End While
        '    End Using
        '  End Using
        'End Using

        If String.IsNullOrWhiteSpace(projectFolder) Then Return

        localInfo = ""
        ProjectDatumHelper.DeleteAllProjectDatumRecordsByProjectId(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        DeleteProjectRecordByProjectId(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        CF.DeleteProjectRolePermissionByProjectIdByUserId(projectId, usrId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        CF.DeleteProjectFiles(projectFolder, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        CF.DeleteProjectFolders(projectFolder, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
    End Sub

#End Region

#Region "Insert"

    Public Shared Sub AddProject(ByVal projectdata As String, Optional ByRef callInfo As String = "")
      Dim project As New EditProject
      Dim localInfo As String = ""
      Try
        Try
          project = serializer.Deserialize(Of EditProject)(projectdata)
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (project deserialization): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

        localInfo = ""
        AddProject(project, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
    End Sub

    Public Shared Function AddProject_ORIG(ByVal project As EditProject, Optional ByRef callInfo As String = "") As Long
      Dim retVal As Long = -1 'return new project id
      Dim localInfo As String = ""
      Dim errStep As String = " 0 "
      Try
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        errStep &= " 1 "
        Dim roleId As Integer = 3 'override for now

        Dim projectId As Long = -1 'gets return value of insert
        Dim cmdText As String = ""
        Dim prm As SqlParameter

        'from aspx 
        roleId = 3 'Project Manager 
        Dim projectFolderName As String = CF.GetProjectFolderBase & "\" & CF.GenerateGUID.ToString

        '  Insert Data into the project table
        Try
          Using conn As New SqlConnection(dataConn)
            Using cmd As SqlCommand = conn.CreateCommand()

              cmdText = "INSERT INTO " & dataSchema & ".Project " & _
                "(Name, Folder, OwnerGuid, Created, CreatorGuid, Edited, EditorGuid) " & _
                "VALUES (@Name, @Folder, @OwnerGuid, @Created, @CreatorGuid, @Edited, @EditorGuid) " & _
                "SET @newOid = SCOPE_IDENTITY();"
              cmd.CommandText = cmdText

              prm = New SqlParameter("@Name", SqlDbType.VarChar, 50)
              prm.Value = project.Name.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@Folder", SqlDbType.VarChar, 100)
              prm.Value = projectFolderName
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@Created", SqlDbType.DateTime)
              prm.Value = Now
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@OwnerGuid", SqlDbType.UniqueIdentifier)
              prm.Value = usrId
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@CreatorGuid", SqlDbType.UniqueIdentifier)
              prm.Value = usrId
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@Edited", SqlDbType.DateTime)
              prm.Value = Now
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@EditorGuid", SqlDbType.UniqueIdentifier)
              prm.Value = usrId
              cmd.Parameters.Add(prm)

              Dim newOidParameter As New SqlParameter("@newOid", SqlDbType.BigInt)
              newOidParameter.Direction = ParameterDirection.Output
              cmd.Parameters.Add(newOidParameter)

              If conn.State = ConnectionState.Closed Then conn.Open()
              cmd.ExecuteNonQuery()
              projectId = CInt(newOidParameter.Value)

            End Using
          End Using

        Catch ex As Exception
          callInfo &= String.Format("  {0} error (Create project): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return retVal
        End Try

        If projectId < 1 Then Throw New Exception("Error on project record creation. Aborting project creation.")
        retVal = projectId

        ' create project folders
        localInfo = ""
        CF.CreateBaseProjectFolders(projectFolderName, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        '  Insert Data into the roles table
        Try
          Using conn As New SqlConnection(dataConn)
            Using cmd As SqlCommand = conn.CreateCommand()
              cmdText = "INSERT INTO " & dataSchema & ".PermissionRoles (ProjectOid,RoleID,CreatorGuid,DateCreated,DateUpdated) " & _
                      "VALUES (@ProjectOid, @RoleID, @CreatorGuid, @DateCreated, @DateUpdated) SET @newOid = SCOPE_IDENTITY();"

              cmd.CommandText = cmdText
              prm = New SqlParameter("@ProjectOid", SqlDbType.BigInt)
              prm.Value = projectId
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@RoleID", SqlDbType.BigInt)
              prm.Value = CF.NullSafeSqlLong(roleId, -1)
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@CreatorGuid", SqlDbType.UniqueIdentifier)
              prm.Value = usrId
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@DateCreated", SqlDbType.DateTime)
              prm.Value = Now
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@DateUpdated", SqlDbType.DateTime)
              prm.Value = Now
              cmd.Parameters.Add(prm)

              Dim newOidParameter As New SqlParameter("@newOid", SqlDbType.BigInt)
              newOidParameter.Direction = ParameterDirection.Output
              cmd.Parameters.Add(newOidParameter)

              If conn.State = ConnectionState.Closed Then conn.Open()
              cmd.ExecuteNonQuery()
            End Using
          End Using
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (Create role): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return retVal
        End Try

        '  Insert Data into the operation table
        Dim datumId As Long = 0
        Try
          Dim operationID As Integer = -1 'insert failure value
          Dim stateName As String = project.State
          Dim cntyVal As String = project.County
          Dim spaceIx As Integer = cntyVal.IndexOf(" ")
          Dim countyName As String = cntyVal.Substring(spaceIx).Trim
          Dim countyFull() As String = cntyVal.Split(CV.spaceSeparator)
          Dim countyID As Integer = 0
          Dim stateAbbr As String = stateName 'project.State is abbrev. from Operation table 'CF.GetStateAbbr(stateName) TODO: check this
          localInfo = ""
          If stateAbbr.Length > 2 Then stateAbbr = StatesCountiesEtc.GetStateAbbr(stateName, localInfo) 'just in case
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

          Dim insertFields As String = ""
          Dim insertValues As String = ""
          Dim sendNotes As String = project.Notes.Trim

          localInfo = ""
          datumId = ProjectDatumHelper.CreateNewProjectDatum(projectId, usrId, sendNotes, localInfo) ' new datum for operation
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & "New operation datum: " & localInfo

          If datumId > 0 Then
            insertFields &= "ObjectID,OperationName,State,CountyName,CountyCode,Address,City,Zip,StartCalMonth,StartCalYear,Contact,ContactOfficePhone,ContactHomePhone,ContactEmail"
            insertValues &= "@ObjectID,@OperationName,@State,@CountyName,@CountyCode,@Address,@City,@Zip,@StartCalMonth,@StartCalYear,@Contact,@ContactOfficePhone,@ContactHomePhone,@ContactEmail"

            Using conn As New SqlConnection(dataConn)
              Using cmd As SqlCommand = conn.CreateCommand()
                cmdText = "insert into " & dataSchema & ".Operation (" & insertFields & ") Values (" & insertValues & ")"
                cmd.CommandText = cmdText

                prm = New SqlParameter("@ObjectID", SqlDbType.BigInt)
                prm.Value = datumId
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@OperationName", SqlDbType.NVarChar, 30)
                prm.Value = project.OperationName
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@State", SqlDbType.NVarChar, 2)
                prm.Value = stateAbbr
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@CountyName", SqlDbType.NVarChar, 50)
                prm.Value = countyName
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@CountyCode", SqlDbType.SmallInt)
                prm.Value = CInt(StatesCountiesEtc.RemoveRegionCodeFromSubRegionCode(countyFull(0), Nothing))
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@Address", SqlDbType.NVarChar, 30)
                prm.Value = project.Address
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@Zip", SqlDbType.NVarChar, 10)
                prm.Value = project.Zip
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@City", SqlDbType.NVarChar, 20)
                prm.Value = project.City
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@Contact", SqlDbType.NVarChar, 30)
                prm.Value = project.Contact
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@ContactOfficePhone", SqlDbType.NVarChar, 14)
                prm.Value = project.ContactOfficePhone
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@ContactHomePhone", SqlDbType.NVarChar, 14)
                prm.Value = project.ContactHomePhone
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@ContactEmail", SqlDbType.NVarChar, 40)
                prm.Value = project.ContactEmail
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@StartCalMonth", SqlDbType.SmallInt)
                prm.Value = project.StartCalMonth
                cmd.Parameters.Add(prm)

                prm = New SqlParameter("@StartCalYear", SqlDbType.SmallInt)
                prm.Value = project.StartCalYear
                cmd.Parameters.Add(prm)

                If conn.State = ConnectionState.Closed Then conn.Open()
                operationID = cmd.ExecuteNonQuery()

              End Using
            End Using
          End If

        Catch ex As Exception
          callInfo &= String.Format("  {0} error (Create operation): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return retVal
        End Try

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function AddProject(ByVal project As EditProject, Optional ByRef callInfo As String = "") As Long
      Dim retVal As Long = -1 'return new project id
      Dim localInfo As String = ""
      Try
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim roleId As Integer = 3 'override for now
        Dim projectId As Long = -1 'gets return value of insert 
        Dim projectFolderName As String = CF.GetProjectFolderBase & "\" & CF.GenerateGUID.ToString

        '  Insert Data into the project table 
        localInfo = ""
        projectId = AddProject_InsertProject(project, usrId, projectFolderName, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If projectId < 1 Then Throw New Exception("Error on project record creation. Aborting project creation.")
        retVal = projectId
        'callInfo &= Environment.NewLine & "error: projectId " & projectId ' ----- DEBUG

        '  Create project folders
        localInfo = ""
        Dim foldersCreated As Boolean = CF.CreateBaseProjectFolders(projectFolderName, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If Not foldersCreated Then
          callInfo &= Environment.NewLine & "error: project folder creation failed."
        End If
        'callInfo &= Environment.NewLine & "error: foldersCreated " & foldersCreated ' ----- DEBUG

        '  Insert Data into the roles table 
        localInfo = ""
        Dim rolesInserted As Integer = AddProject_InsertPermissionRoles(projectId, usrId, roleId, projectFolderName, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If rolesInserted < 1 Then
          callInfo &= Environment.NewLine & "error: no roles inserted."
        End If
        'callInfo &= Environment.NewLine & "error: rolesInserted " & rolesInserted ' ----- DEBUG

        '  Insert Data into the operation table 
        localInfo = ""
        Dim operationId As Long = AddProject_InsertOperation(project, projectId, usrId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If operationId < 1 Then
          callInfo &= Environment.NewLine & "error: operation insertion failed."
        End If
        'callInfo &= Environment.NewLine & "error: operationId " & operationId ' ----- DEBUG

        'Return retVal ' ----- DEBUG
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function AddProject_InsertProject(ByVal project As EditProject, ByVal usrId As Guid, _
                                             ByVal folderName As String, ByRef callInfo As String) As Long
      Dim retVal As Long = -1 'return new project id
      Try
        Dim cmdText As String = "INSERT INTO " & dataSchema & ".Project " & _
          "(Name, Folder, OwnerGuid, Created, CreatorGuid, Edited, EditorGuid) " & _
          "VALUES (@Name, @Folder, @OwnerGuid, @Created, @CreatorGuid, @Edited, @EditorGuid) " & _
          "SET @newOid = SCOPE_IDENTITY();"

        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = cmdText
            Dim newOidParameter As New SqlParameter("@newOid", SqlDbType.BigInt)
            newOidParameter.Direction = ParameterDirection.Output

            With cmd.Parameters
              .Add(newOidParameter)
              .Add("@Name", SqlDbType.VarChar, 50).Value = project.Name.Trim
              .Add("@Folder", SqlDbType.VarChar, 100).Value = folderName
              .Add("@Created", SqlDbType.DateTime).Value = Now
              .Add("@OwnerGuid", SqlDbType.UniqueIdentifier).Value = usrId
              .Add("@CreatorGuid", SqlDbType.UniqueIdentifier).Value = usrId
              .Add("@Edited", SqlDbType.DateTime).Value = Now
              .Add("@EditorGuid", SqlDbType.UniqueIdentifier).Value = usrId
            End With

            If conn.State = ConnectionState.Closed Then conn.Open()
            cmd.ExecuteNonQuery()
            retVal = CInt(newOidParameter.Value)
          End Using
        End Using

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function AddProject_InsertPermissionRoles(ByVal projectId As Long, ByVal usrId As Guid, _
                                         ByVal roleId As Long, ByVal folderName As String, ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1 'return num rows inserted
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = "INSERT INTO " & dataSchema & ".PermissionRoles (ProjectOid,RoleID,CreatorGuid,DateCreated,DateUpdated) " & _
                "VALUES (@ProjectOid, @RoleID, @CreatorGuid, @DateCreated, @DateUpdated) SET @newOid = SCOPE_IDENTITY();"

        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = cmdText

            Dim newOidParameter As New SqlParameter("@newOid", SqlDbType.BigInt)
            newOidParameter.Direction = ParameterDirection.Output
            With cmd.Parameters
              .Add(newOidParameter)
              .Add("@ProjectOid", SqlDbType.BigInt).Value = projectId
              .Add("@RoleID", SqlDbType.BigInt).Value = CF.NullSafeSqlLong(roleId, -1)
              .Add("@CreatorGuid", SqlDbType.UniqueIdentifier).Value = usrId
              .Add("@DateCreated", SqlDbType.DateTime).Value = Now
              .Add("@DateUpdated", SqlDbType.DateTime).Value = Now
            End With

            If conn.State = ConnectionState.Closed Then conn.Open()
            retVal = cmd.ExecuteNonQuery()
          End Using
        End Using

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function AddProject_InsertOperation(ByVal project As EditProject, ByVal projectId As Long, _
                                            ByVal usrId As Guid, ByRef callInfo As String) As Long
      Dim retVal As Long = -1 'return new operation id
      Dim localInfo As String = ""
      Try
        '  Insert Data into the operation table 
        Dim sendNotes As String = project.Notes.Trim
        localInfo = ""
        retVal = ProjectDatumHelper.CreateNewProjectDatum(projectId, usrId, sendNotes, localInfo) ' new datum for operation
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & "New operation datum: " & localInfo

        Dim stateName As String = project.State
        Dim cntyVal As String = project.County
        Dim spaceIx As Integer = cntyVal.IndexOf(" ")
        Dim countyName As String = cntyVal.Substring(spaceIx).Trim
        Dim countyFull() As String = cntyVal.Split(CV.spaceSeparator)
        Dim countyID As Integer = 0
        Dim stateAbbr As String = stateName 'project.State is abbrev. from Operation table 'CF.GetStateAbbr(stateName) TODO: check this
        localInfo = ""
        If stateAbbr.Length > 2 Then stateAbbr = StatesCountiesEtc.GetStateAbbr(stateName, localInfo) 'just in case
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim insertFields As String = ""
        Dim insertValues As String = ""
        Dim insertCount As Integer = 0
        If retVal > 0 Then
          insertFields &= "ObjectID,OperationName,State,CountyName,CountyCode,Address,City,Zip,StartCalMonth,StartCalYear,Contact,ContactOfficePhone,ContactHomePhone,ContactEmail"
          insertValues &= "@ObjectID,@OperationName,@State,@CountyName,@CountyCode,@Address,@City,@Zip,@StartCalMonth,@StartCalYear,@Contact,@ContactOfficePhone,@ContactHomePhone,@ContactEmail"

          Dim cmdText As String = "insert into " & dataSchema & ".Operation (" & insertFields & ") Values (" & insertValues & ")"
          Using conn As New SqlConnection(dataConn)
            Using cmd As SqlCommand = conn.CreateCommand()
              cmd.CommandText = cmdText

              With cmd.Parameters
                .Add("@ObjectID", SqlDbType.BigInt).Value = retVal
                .Add("@OperationName", SqlDbType.NVarChar, 30).Value = project.OperationName
                .Add("@State", SqlDbType.NVarChar, 2).Value = stateAbbr
                .Add("@CountyName", SqlDbType.NVarChar, 50).Value = countyName
                .Add("@CountyCode", SqlDbType.SmallInt).Value = CInt(StatesCountiesEtc.RemoveRegionCodeFromSubRegionCode(countyFull(0), Nothing))
                .Add("@Address", SqlDbType.NVarChar, 30).Value = project.Address
                .Add("@Zip", SqlDbType.NVarChar, 10).Value = project.Zip
                .Add("@City", SqlDbType.NVarChar, 20).Value = project.City
                .Add("@Contact", SqlDbType.NVarChar, 30).Value = project.Contact
                .Add("@ContactOfficePhone", SqlDbType.NVarChar, 14).Value = project.ContactOfficePhone
                .Add("@ContactHomePhone", SqlDbType.NVarChar, 14).Value = project.ContactHomePhone
                .Add("@ContactEmail", SqlDbType.NVarChar, 40).Value = project.ContactEmail
                .Add("@StartCalMonth", SqlDbType.SmallInt).Value = project.StartCalMonth
                .Add("@StartCalYear", SqlDbType.SmallInt).Value = project.StartCalYear
              End With

              'Dim debug As String = cmdText.Replace("@ObjectID", cmd.Parameters.Item("@ObjectID").Value.ToString) _
              '                      .Replace("@OperationName", cmd.Parameters.Item("@OperationName").Value.ToString) _
              '                      .Replace("@State", cmd.Parameters.Item("@State").Value.ToString) _
              '                      .Replace("@CountyName", cmd.Parameters.Item("@CountyName").Value.ToString) _
              '                      .Replace("@CountyCode", cmd.Parameters.Item("@CountyCode").Value.ToString) _
              '                      .Replace("@Address", cmd.Parameters.Item("@Address").Value.ToString) _
              '                      .Replace("@Zip", cmd.Parameters.Item("@Zip").Value.ToString) _
              '                      .Replace("@City", cmd.Parameters.Item("@City").Value.ToString) _
              '                      .Replace("@Contact", cmd.Parameters.Item("@Contact").Value.ToString) _
              '                      .Replace("@ContactOfficePhone", cmd.Parameters.Item("@ContactOfficePhone").Value.ToString) _
              '                      .Replace("@ContactHomePhone", cmd.Parameters.Item("@ContactHomePhone").Value.ToString) _
              '                      .Replace("@ContactEmail", cmd.Parameters.Item("@ContactEmail").Value.ToString) _
              '                      .Replace("@StartCalMonth", cmd.Parameters.Item("@StartCalMonth").Value.ToString) _
              '                      .Replace("@StartCalYear", cmd.Parameters.Item("@StartCalYear").Value.ToString)
              'CF.SendOzzy("Operation", debug, Nothing)

              If conn.State = ConnectionState.Closed Then conn.Open()
              insertCount = cmd.ExecuteNonQuery()
            End Using
          End Using
        End If
        If insertCount = 0 Then
          callInfo &= Environment.NewLine & "error: no operation inserted."
        End If

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

#End Region

#Region "Update"

    Public Shared Sub EditProject(ByVal projectId As Long, ByVal projectdata As String, ByRef callInfo As String)
      Dim project As New EditProject
      Dim localInfo As String = ""
      Try
        Try
          project = serializer.Deserialize(Of EditProject)(projectdata)
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (project deserialization): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try
        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Dim roleId As Integer = 3 'override for now

        Dim cmdText As String = ""
        Dim prm As SqlParameter

        Try
          ' Get correct datum
          localInfo = ""
          Dim opDatum As Long = -1
          opDatum = OperationHelper.GetOperationIdByProjectId(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

          ' Update projectdatum table
          Using conn As New SqlConnection(dataConn)
            Using cmd As SqlCommand = conn.CreateCommand()
              cmdText = "UPDATE PD " & _
                " SET [Notes] = @Notes " & _
                "    ,[Edited] = @Edited " & _
                "    ,[EditorGuid] = @EditorGuid " & _
                " FROM " & dataSchema & ".[ProjectDatum] as PD " & _
                " WHERE PD.ObjectId= @ObjectId "

              cmd.CommandText = cmdText

              prm = New SqlParameter("@Notes", SqlDbType.VarChar, 100)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.Notes.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@Edited", SqlDbType.DateTime)
              prm.Direction = ParameterDirection.Input
              prm.Value = Now
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@EditorGuid", SqlDbType.UniqueIdentifier)
              prm.Direction = ParameterDirection.Input
              prm.Value = usrId
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@ObjectId", SqlDbType.BigInt)
              prm.Direction = ParameterDirection.Input
              prm.Value = opDatum
              cmd.Parameters.Add(prm)

              If conn.State = ConnectionState.Closed Then conn.Open()
              cmd.ExecuteNonQuery()

            End Using
          End Using

        Catch ex As Exception
          callInfo &= String.Format("  {0} error (Update projectdatum table): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

        Try
          ' Update project table
          Using conn As New SqlConnection(dataConn)
            Using cmd As SqlCommand = conn.CreateCommand()
              cmdText = "UPDATE P " & _
                " SET [Name] = @Name " & _
                "    ,[Edited] = @Edited " & _
                "    ,[EditorGuid] = @EditorGuid " & _
                " FROM " & dataSchema & ".[Project] as P " & _
                " WHERE P.ObjectID= @ProjectId "

              cmd.CommandText = cmdText

              prm = New SqlParameter("@Name", SqlDbType.VarChar, 50)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.Name.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@Edited", SqlDbType.DateTime)
              prm.Direction = ParameterDirection.Input
              prm.Value = Now
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@EditorGuid", SqlDbType.UniqueIdentifier)
              prm.Direction = ParameterDirection.Input
              prm.Value = usrId
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@ProjectId", SqlDbType.Int)
              prm.Direction = ParameterDirection.Input
              prm.Value = projectId
              cmd.Parameters.Add(prm)

              If conn.State = ConnectionState.Closed Then conn.Open()
              cmd.ExecuteNonQuery()

            End Using
          End Using

        Catch ex As Exception
          callInfo &= String.Format("  {0} error (Update project table): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

        Try
          ' Update operation table 
          Using conn As New SqlConnection(dataConn)
            Using cmd As SqlCommand = conn.CreateCommand()
              cmdText = " UPDATE Op " & _
                " SET [OperationName] = @OperationName " & _
                "    ,[Address] = @Address " & _
                "    ,[City] = @City " & _
                "    ,[Zip] = @Zip " & _
                "    ,[Contact] = @Contact " & _
                "    ,[ContactOfficePhone] = @ContactOfficePhone " & _
                "    ,[ContactHomePhone] = @ContactHomePhone " & _
                "    ,[ContactEmail] = @ContactEmail " & _
                "    ,[StartCalYear] = @StartCalYear " & _
                "    ,[StartCalMonth] = @StartCalMonth " & _
                " FROM " & dataSchema & ".[Operation] as Op " & _
                " WHERE Op.ObjectID= @OperationId "

              cmd.CommandText = cmdText

              prm = New SqlParameter("@OperationName", SqlDbType.NVarChar, 30)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.OperationName.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@Address", SqlDbType.NVarChar, 30)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.Address.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@City", SqlDbType.NVarChar, 20)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.City.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@Zip", SqlDbType.NVarChar, 10)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.Zip.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@Contact", SqlDbType.NVarChar, 30)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.Contact.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@ContactOfficePhone", SqlDbType.NVarChar, 14)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.ContactOfficePhone.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@ContactHomePhone", SqlDbType.NVarChar, 14)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.ContactHomePhone.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@ContactEmail", SqlDbType.NVarChar, 40)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.ContactEmail.Trim
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@StartCalMonth", SqlDbType.SmallInt)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.StartCalMonth
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@StartCalYear", SqlDbType.SmallInt)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.StartCalYear
              cmd.Parameters.Add(prm)

              prm = New SqlParameter("@OperationId", SqlDbType.Int)
              prm.Direction = ParameterDirection.Input
              prm.Value = project.OperationId
              cmd.Parameters.Add(prm)

              If conn.State = ConnectionState.Closed Then conn.Open()
              cmd.ExecuteNonQuery()
            End Using
          End Using
        Catch ex As Exception
          callInfo &= String.Format("  {0} error (Update operation table): {1}  ", EH.GetCallerMethod(), ex.Message)
          Return
        End Try

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
    End Sub

#End Region

#Region "Retrieve"

    ''' <summary>
    ''' Get project record by ID.
    ''' </summary>
    Public Shared Function Fetch(ByVal projectId As Long, ByRef callInfo As String) As Project
      Dim retVal As New Project
      Dim localInfo As String = ""
      Try
        Dim dr As DataRow = GetProjectRow(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        retVal = ExtractProjectFromRow(dr, localInfo)
        If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Get row from database.
    ''' </summary>
    Private Shared Function GetProjectRow(ByVal projectId As Long, ByRef callInfo As String) As DataRow
      Dim retVal As DataRow = Nothing
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = String.Format(<a>
            SELECT *
            FROM {0}.[Project]
            WHERE [ObjectID]= @projectId</a>.Value, dataSchema)

        Dim parm As New SqlParameter("@projectId", SqlDbType.BigInt)
        parm.Value = projectId

        Dim dt As DataTable = CF.GetDataTable(dataConn, cmdText, parm, localInfo)
        If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then retVal = dt.Rows(0)

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function ExtractProjectFromRow(ByVal dr As DataRow, Optional ByRef callInfo As String = "") As Project
      Dim retVal As New Project
      Try
        With retVal
          .ObjectID = CF.NullSafeInteger(dr.Item("ObjectID"), -1)
          .Name = CF.NullSafeString(dr.Item("Name"), "")
          .Folder = CF.NullSafeString(dr.Item("Folder"), "")
          .OwnerGuid = CF.NullSafeGuid(dr.Item("OwnerGuid"))
          .Created = CF.NullSafeDateTime(dr.Item("Created"))
          .CreatorGuid = CF.NullSafeGuid(dr.Item("CreatorGuid"))
          .Edited = CF.NullSafeDateTime(dr.Item("Edited"))
          .EditorGuid = CF.NullSafeGuid(dr.Item("EditorGuid"))
          ''not in db
          '.Owner As String = ""
          '.Creator As String = ""
          '.Editor As String = "" 
        End With

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function GetProjectById(ByVal projectId As Long, ByRef callInfo As String) As EditProject
      Dim retVal As EditProject = Nothing
      Dim localInfo As String = ""
      Try
        Dim projects As DataTable

        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Try
          localInfo = ""
          projects = GetProjectsTable(usrId, localInfo, projectId)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

          'Fill in names for ids
          localInfo = ""
          projects = UpdateNames(projects, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw New Exception("ProjectsTable (" & callInfo & ")", ex)
        End Try

        For Each dr As DataRow In projects.Rows
          Try
            localInfo = ""
            retVal = ExtractEditProjectFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
          Catch ex As Exception
            Throw New Exception("Project (" & callInfo & ")", ex)
          End Try
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function GetProjects(ByVal usrId As Guid, ByRef callInfo As String) As ReturnProjectsStructure
      Dim retVal As New ReturnProjectsStructure
      Dim retProjects As List(Of ReturnProject)
      Dim retInfo As String = ""
      Dim localInfo As String = ""
      Try
        retProjects = GetProjectsList(usrId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        retVal.projects = retProjects
        retVal.info = retInfo
      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function GetProjectsList(ByVal usrId As Guid, Optional ByRef callInfo As String = "") As List(Of ReturnProject)
      Dim retVal As New List(Of ReturnProject)
      Dim retProject As ReturnProject
      Dim project As ProjectStructure
      Dim operationDatum As New ProjectDatum
      Dim operation As OperationRecord
      Dim role As RoleStructure
      Dim localInfo As String = ""
      Dim tmpDate As Date
      Try

        localInfo = ""
        Dim projects As DataTable = GetProjectsTable(usrId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'Fill in names for ids
        localInfo = ""
        projects = UpdateNames(projects, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        For Each dr As DataRow In projects.Rows
          Try
            project = New ProjectStructure
            With project
              .ObjectID = CF.NullSafeInteger(dr.Item("prjID"), -1) '" PRJ.Objectid  as prjID  " & _
              .Name = CF.NullSafeString(dr.Item("prjName"), "") '" , (PRJ.Name) as prjName   " & _
              .Folder = CF.NullSafeString(dr.Item("Folder"), "") '" , RIGHT( PRJ.Folder, CHARINDEX('\', REVERSE( PRJ.Folder ) ) -1 ) AS Folder  " & _
              .OwnerGuid = CF.NullSafeGuid(dr.Item("prjOwnerID")) '" , PRJ.OwnerGuid as prjOwnerID     " & _
              .Owner = CF.NullSafeString(dr.Item("prjOwner"), "") '.Add(New DataColumn("prjOwner"))
              If Not IsDBNull(dr.Item("prjCreated")) Then
                If Date.TryParse(dr.Item("prjCreated").ToString, tmpDate) Then
                  .Created = tmpDate '" , PRJ.Created as prjCreated " & _
                End If
              End If
              .CreatorGuid = CF.NullSafeGuid(dr.Item("prjCreatorId")) '" , PRJ.CreatorGuid as prjCreatorId " & _
              .Creator = CF.NullSafeString(dr.Item("prjCreator"), "") '.Add(New DataColumn("prjCreator"))
              If Not IsDBNull(dr.Item("prjEdited")) Then
                If Date.TryParse(dr.Item("prjEdited").ToString, tmpDate) Then
                  .Edited = tmpDate '" , PRJ.Edited as prjEdited   " & _
                End If
              End If
              .EditorGuid = CF.NullSafeGuid(dr.Item("prjEditorId")) '" , PRJ.EditorGuid as prjEditorId   " & _
              .Editor = CF.NullSafeString(dr.Item("prjEditor"), "") '.Add(New DataColumn("prjEditor"))
              .info = ""
            End With
          Catch ex As Exception
            Throw New Exception("ProjectStructure", ex)
          End Try

          Try
            role = New RoleStructure
            With role
              .ObjectID = CF.NullSafeInteger(dr.Item("RoleId"), -1) '" ,PRles.[ObjectID] as RoleId  " & _
              If Not IsDBNull(dr.Item("RoleCreatedDate")) Then
                If Date.TryParse(dr.Item("RoleCreatedDate").ToString, tmpDate) Then
                  .Created = tmpDate '" ,PRles.[DateCreated] as RoleCreatedDate  " & _
                End If
              End If
              .CreatorGuid = CF.NullSafeGuid(dr.Item("RoleCreatorId"))     '" ,PRles.CreatorGuid as RoleCreatorId  " & _
              .Creator = CF.NullSafeString(dr.Item("RoleCreatedBy"), "") '.Add(New DataColumn("RoleCreatedBy"))
              If Not IsDBNull(dr.Item("RoleEditedDate")) Then
                If Date.TryParse(dr.Item("RoleEditedDate").ToString, tmpDate) Then
                  .Edited = tmpDate '" ,PRles.[DateUpdated] as RoleEditedDate   " & _
                End If
              End If
              .EditorGuid = CF.NullSafeGuid(dr.Item("RoleEditorId")) '" ,PRles.EditorGuid as RoleEditorId  " & _
              .Editor = CF.NullSafeString(dr.Item("RoleEditedBy"), "")  '.Add(New DataColumn("RoleEditedBy"))
              '.EmailSent = dr.Item("RoleEmailSent")    '" ,PRles.[EmailSent] as RoleEmailSent  " & _
              .RoleID = CF.NullSafeInteger(dr.Item("RoleRoleId"), -1)      '" ,PRles.[RoleID]  as RoleRoleId  " & _
              '.RoleName = CF.NullSafeString(dr.Item("RoleName"), "")    '" , URles.RoleName         " & _
              .info = ""
            End With
          Catch ex As Exception
            Throw New Exception("RoleStructure", ex)
          End Try

          operation = New OperationRecord
          Try
            localInfo = ""
            Dim opPkg As OperationPackage = OperationHelper.Fetch(project.ObjectID, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
            If opPkg.OperationRecord IsNot Nothing Then
              operation = opPkg.OperationRecord
            End If
            If opPkg.DatumRecord IsNot Nothing Then
              operationDatum = opPkg.DatumRecord
            End If
          Catch ex As Exception
            Throw New Exception("OperationPackage", ex)
          End Try

          retProject = New ReturnProject
          With retProject
            .Project = project
            .Operation = operation
            .OperationDatum = operationDatum
            .Role = role
            .info = ""
          End With

          retVal.Add(retProject)
        Next

      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1} ({2}) ", _
                                  EH.GetCallerMethod(), ex.Message, ex.InnerException.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function GetProjectsTable(ByVal usrId As Guid, Optional ByRef callInfo As String = "", _
                                            Optional ByVal projectId As Long = Integer.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try

        ' -- right now, have to include Operation table even though not used in calling function.
        ' -- otherwise, get multiple, multiple projects.
        ' -- need to rewrite at some point.

        Dim localInfo As String = ""
        Dim ifProj As String = If(projectId > 0, " AND PD.ProjectId = @projectId ", "")
        cmdText = String.Format(<a>
          SELECT PRJ.Objectid  as prjID   
           , (PRJ.Name) as prjName    
           , RIGHT( PRJ.Folder, CHARINDEX('\', REVERSE( PRJ.Folder ) ) -1 ) AS Folder   
           , PRJ.OwnerGuid as prjOwnerID      
           , PRJ.Created as prjCreated  
           , PRJ.CreatorGuid as prjCreatorId  
           , PRJ.Edited as prjEdited    
           , PRJ.EditorGuid as prjEditorId    
           , PD.ObjectID  as opID   
           , PD.Created  as opCreated   
           , PD.CreatorGuid   as opCreatorId   
           , PD.Edited  as opEdited   
           , PD.EditorGuid   as opEditorId   
           , PD.Notes AS opNotes		        
           , PD.GUID AS opGuid		        
           ,PRles.[ObjectID] as RoleId   
           ,PRles.[DateCreated] as RoleCreatedDate   
           ,PRles.CreatorGuid as RoleCreatorId   
           ,PRles.[DateUpdated] as RoleEditedDate    
           ,PRles.EditorGuid as RoleEditorId   
           ,PRles.[EmailSent] as RoleEmailSent   
           ,PRles.[RoleID]  as RoleRoleId   
           , URles.RoleName          
           , OP.ObjectID as OperationId   
           , OP.OperationName as Operation    
           , OP.[Address]   
           , OP.City      
           , OP.[State]            
           , OP.[Zip]            
           , REPLACE(OP.CountyName,' County','') AS County     
           , OP.StartCalYear      
           , OP.StartCalMonth    
           , OP.[Contact]   
           , OP.[ContactOfficePhone]   
           , OP.[ContactHomePhone]   
           , OP.[ContactEmail]   
           , OP.[CountyCode]  
          FROM {0}.PermissionRoles as PRles       
          INNER JOIN {0}.Project as PRJ ON PRles.ProjectOid = PRJ.ObjectID       
          INNER JOIN {0}.ProjectDatum as PD ON PRJ.ObjectID = PD.ProjectId        
          INNER JOIN {0}.Operation as OP ON PD.ObjectID = OP.ObjectID       
          LEFT OUTER JOIN {0}.UserRoles as URles ON PRles.RoleID = URles.RoleID       
          WHERE (PRles.CreatorGuid = @usrId or PRles.UserGuid = @usrId)    
            {2} 
          ORDER BY prjName 
                            </a>.Value, dataSchema, usrId.ToString, ifProj)

        Dim parameters As New List(Of SqlParameter)
        Dim parameter As New SqlParameter("@usrId", SqlDbType.UniqueIdentifier)
        parameter.Value = usrId
        parameters.Add(parameter)
        parameter = New SqlParameter("@projectId", SqlDbType.BigInt)
        parameter.Value = projectId
        parameters.Add(parameter)

        localInfo = ""
        retVal = CF.GetDataTable(dataConn, cmdText, parameters.ToArray, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Function ExtractEditProjectFromTableRow(ByVal dr As DataRow, Optional ByRef callInfo As String = "") As EditProject
      Dim retVal As New EditProject
      Try
        With retVal
          .Name = CF.NullSafeString(dr.Item("prjName"), "")
          .Notes = CF.NullSafeString(dr.Item("opNotes"), "")
          .OperationId = CF.NullSafeInteger(dr.Item("OperationId"), -1)
          .OperationName = CF.NullSafeString(dr.Item("Operation"), "")
          .Address = CF.NullSafeString(dr.Item("Address"), "")
          .City = CF.NullSafeString(dr.Item("City"), "")
          .Zip = CF.NullSafeString(dr.Item("Zip"), "")
          .Contact = CF.NullSafeString(dr.Item("Contact"), "")
          .ContactOfficePhone = CF.NullSafeString(dr.Item("ContactOfficePhone"), "")
          .ContactHomePhone = CF.NullSafeString(dr.Item("ContactHomePhone"), "")
          .ContactEmail = CF.NullSafeString(dr.Item("ContactEmail"), "")
          .StartCalYear = CF.NullSafeInteger(dr.Item("StartCalYear"), -1)
          .StartCalMonth = CF.NullSafeInteger(dr.Item("StartCalMonth"), -1)
          .State = CF.NullSafeString(dr.Item("State"), "")
          .County = CF.NullSafeString(dr.Item("CountyCode"), "") & " " & CF.NullSafeString(dr.Item("County"), "") & " County"
        End With
      Catch ex As Exception
        callInfo &= String.Format("{0} error: {1}", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

#End Region

#Region "Other"

    Public Shared Function UpdateNames(ByVal tbl As DataTable, Optional ByRef callInfo As String = "") As DataTable
      Dim retVal As DataTable = Nothing

      retVal = tbl.Copy
      With retVal.Columns
        .Add(New DataColumn("prjOwner", GetType(String)))
        .Add(New DataColumn("prjCreator", GetType(String)))
        .Add(New DataColumn("prjEditor", GetType(String)))
        .Add(New DataColumn("opCreator", GetType(String)))
        .Add(New DataColumn("opEditor", GetType(String)))
        .Add(New DataColumn("RoleCreatedBy", GetType(String)))
        .Add(New DataColumn("RoleEditedBy", GetType(String)))
      End With

      Return retVal 'Don't update until more than one user can be on a project, but need columns for other functions

      'Dim cmdText As String = ""
      'Dim localInfo As String = ""
      'Try
      '  localInfo = ""
      '  Dim usrs As UserList = UserHelper.Fetch(localInfo)  'CF.GetSiteUsers(localInfo)
      '  If localInfo.ToLower.Contains("error") Then callInfo &= localInfo

      '  Dim Name As IEnumerable(Of DataRow)
      '  Dim NameList As List(Of DataRow)
      '  Dim usrsEnum = usrs.Users.AsEnumerable
      '  For Each dr As DataRow In retVal.Rows
      '    With dr 
      '      Name = From usr In usrsEnum
      '            Where CF.NullSafeInteger(usr.Item("UserID"), -99) = CF.NullSafeInteger(.Item("prjOwnerID"), -88)
      '      If Name.Count > 0 Then
      '        NameList = Name.ToList()
      '        .Item("prjOwner") = String.Join(" ", Name(0).Item("FirstName"), Name(0).Item("LastName"))
      '      End If

      '      Name = From usr In usrsEnum
      '            Where CF.NullSafeInteger(usr.Item("UserID"), -99) = CF.NullSafeInteger(.Item("prjCreatorId"), -88)
      '      If Name.Count > 0 Then
      '        NameList = Name.ToList()
      '        .Item("prjCreator") = String.Join(" ", Name(0).Item("FirstName"), Name(0).Item("LastName"))
      '      End If

      '      Name = From usr In usrsEnum
      '            Where CF.NullSafeInteger(usr.Item("UserID"), -99) = CF.NullSafeInteger(.Item("prjEditorId"), -88)
      '      If Name.Count > 0 Then
      '        NameList = Name.ToList()
      '        .Item("prjEditor") = String.Join(" ", Name(0).Item("FirstName"), Name(0).Item("LastName"))
      '      End If

      '      Name = From usr In usrsEnum
      '            Where CF.NullSafeInteger(usr.Item("UserID"), -99) = CF.NullSafeInteger(.Item("opCreatorId"), -88)
      '      If Name.Count > 0 Then
      '        NameList = Name.ToList()
      '        .Item("opCreator") = String.Join(" ", Name(0).Item("FirstName"), Name(0).Item("LastName"))
      '      End If

      '      Name = From usr In usrsEnum
      '            Where CF.NullSafeInteger(usr.Item("UserID"), -99) = CF.NullSafeInteger(.Item("opEditorId"), -88)
      '      If Name.Count > 0 Then
      '        NameList = Name.ToList()
      '        .Item("opEditor") = String.Join(" ", Name(0).Item("FirstName"), Name(0).Item("LastName"))
      '      End If

      '      Name = From usr In usrsEnum
      '            Where CF.NullSafeInteger(usr.Item("UserID"), -99) = CF.NullSafeInteger(.Item("RoleCreatorId"), -88)
      '      If Name.Count > 0 Then
      '        NameList = Name.ToList()
      '        .Item("RoleCreatedBy") = String.Join(" ", Name(0).Item("FirstName"), Name(0).Item("LastName"))
      '      End If

      '      Name = From usr In usrsEnum
      '            Where CF.NullSafeInteger(usr.Item("UserID"), -99) = CF.NullSafeInteger(.Item("RoleEditorId"), -88)
      '      If Name.Count > 0 Then
      '        NameList = Name.ToList()
      '        .Item("RoleEditedBy") = String.Join(" ", Name(0).Item("FirstName"), Name(0).Item("LastName"))
      '      End If

      '    End With
      '  Next

      'Catch ex As Exception
      '  callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      'End Try
      'Return retVal
    End Function

    Public Shared Function GetBoundingBox(ByVal projectId As Long, Optional ByRef callInfo As String = "") As ProjectBounds
      Dim retVal As New ProjectBounds
      Dim cmdText As String = ""
      Dim localInfo As String = ""
      Try
        'TODO: find out what should be used. Right now, just use contour xml values
        Dim myContourXmlHelper As New TerLoc.Model.ContourXmlHelper
        localInfo = ""
        Dim xmlList As ContourXmlPackageList = myContourXmlHelper.Fetch(projectId, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= Environment.NewLine & localInfo

        Dim recs As List(Of ContourXmlPackage) = xmlList.contourXmls
        Dim XDoc As XDocument = Nothing
        For Each pkg As ContourXmlPackage In recs
          If pkg.contourXmlRecord.XmlType <> "SMO" Then
            XDoc = pkg.contourXmlRecord.XmlDoc
          End If
        Next
        If XDoc Is Nothing Then Return retVal

        Dim root As XElement = XDoc.Root
        'set opposite values for comparison later
        retVal.MinX = Integer.MaxValue
        retVal.MaxX = Integer.MinValue
        retVal.MinY = Integer.MaxValue
        retVal.MaxY = Integer.MinValue

        Dim tmp As Integer
        For Each xelem As XElement In root.Descendants("ENVELOPE")
          tmp = CF.NullSafeInteger(xelem.Attribute("minx").Value, Integer.MinValue)
          If 0 <= tmp AndAlso tmp < retVal.MinX Then retVal.MinX = tmp

          tmp = CF.NullSafeInteger(xelem.Attribute("maxx").Value, Integer.MinValue)
          If 0 <= tmp AndAlso retVal.MaxX < tmp Then retVal.MaxX = tmp

          tmp = CF.NullSafeInteger(xelem.Attribute("miny").Value, Integer.MinValue)
          If 0 <= tmp AndAlso tmp < retVal.MinY Then retVal.MinY = tmp

          tmp = CF.NullSafeInteger(xelem.Attribute("maxy").Value, Integer.MinValue)
          If 0 <= tmp AndAlso retVal.MaxY < tmp Then retVal.MaxY = tmp
        Next

        If retVal.MinX = Integer.MaxValue OrElse _
          retVal.MaxX = Integer.MinValue OrElse _
          retVal.MinY = Integer.MaxValue OrElse _
          retVal.MaxY = Integer.MinValue Then
          retVal = New ProjectBounds
          Throw New ArgumentException("Bounds not set properly")
        End If

      Catch ex As Exception
        callInfo &= Environment.NewLine & String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
      Return retVal
    End Function

    Public Shared Sub TransferProject(ByVal projectId As Long, ByVal transfereeName As String, ByVal addltext As String, _
                                      ByRef callInfo As String)
      Dim project As EditProject
      Dim localInfo As String = ""
      Try
        'Throw New NotImplementedException("Duplicate project is not implemented")

        localInfo = ""
        Dim transferee As User = UserHelper.Fetch(transfereeName, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim transfereeId As Guid = transferee.UserId
        If transfereeId = Guid.Empty Then Throw New ArgumentNullException("User", _
          "User was not found for name: " & transfereeName & " in " & Membership.ApplicationName)

        'get original project
        localInfo = ""
        project = GetProjectById(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If project Is Nothing Then Throw New ArgumentNullException("Project", "Project was not found for id: " & projectId)

        localInfo = ""
        Dim newProjectId As Long = AddProject(project, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If newProjectId < 0 Then
          callInfo &= Environment.NewLine & "No new project id was created when adding project."
          Return
        End If

        'change owner in permission roles
        localInfo = ""
        CF.UpdatePermissionRolesCreator(newProjectId, transfereeId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'check owner was changed in permission roles
        localInfo = ""
        Dim prjOwnerId As Guid = CF.GetPermissionRolesCreator(newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        If prjOwnerId <> transfereeId Then
          localInfo = ""
          DeleteProject(newProjectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          Throw New Exception("The transferred project was deleted because the role owner could not be updated.")
        End If

        'change owner in project table
        localInfo = ""
        CF.UpdateProjectOwner(newProjectId, transfereeId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'check owner was changed in project table
        localInfo = ""
        prjOwnerId = CF.GetProjectOwner(newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        If prjOwnerId <> transfereeId Then
          localInfo = ""
          DeleteProject(newProjectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          Throw New Exception("The transferred project was deleted because the project owner could not be updated.")
        End If

        localInfo = ""
        DuplicateOtherTables(projectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        Dim userEmail As String = transferee.Email
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Try
          Dim checkEmail As New System.Net.Mail.MailAddress(userEmail)
        Catch ex As Exception
          userEmail = ""
        End Try

        localInfo = ""
        Dim sender As User = UserHelper.GetCurrentUser(localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        Dim senderEmail As String = sender.Email
        Try
          Dim checkEmail As New System.Net.Mail.MailAddress(senderEmail)
        Catch ex As Exception
          senderEmail = ""
        End Try

        Dim toAddr As String = userEmail
        Dim fromAddr As String = senderEmail
        Dim ccAddr As String = senderEmail
        Dim emailText As String = ""
        emailText = "Dear " & transfereeName & "," & _
          "<br />" & _
          "A TerLoc project has been transferred to you from user " & sender.UserName & ". " & _
          "The project should show up on your Project Mgmt page at <a href='terrace.missouri.edu'>terrace.missouri.edu</a> with the name " & project.Name & "." & _
          "<br />" & _
          "If you don’t see the project (you may need to refresh the page if you already have it open), please forward this email to " & _
          "Kevin Atherton at <a href='mailto:athertonk@missouri.edu'>athertonk@missouri.edu</a> for troubleshooting and " & _
          "keep the sender cc’ed above so they know something went wrong." & _
          "<br />" & _
          "If you think you received this project in error, please contact the sender at the cc’ed email address above." & _
          "<br />" & _
          "Thank you." & _
          "<br />" & _
          "<br />" & _
          addltext

        emailText = "Hello " & transfereeName & "," & _
          "<br />" & _
          "I've transferred a TerLoc project to you. " & _
          "The project should show up on your Project Mgmt page at <a href='terrace.missouri.edu'>terrace.missouri.edu</a> with the name " & project.Name & "." & _
          "<br />" & _
          "If you don’t see the project (you may need to refresh the page if you already have it open), please reply back " & _
          "so I know something went wrong and " & _
          "add Kevin Atherton (<a href='mailto:athertonk@missouri.edu'>athertonk@missouri.edu</a>) to the recipients so he can troubleshoot. " & _
          "<br />" & _
          "If I accidently sent you this project in error, please let me know." & _
          "<br />" & _
          "Thank you." & _
          "<br />" & _
          "<br />" & _
          addltext

        If String.IsNullOrWhiteSpace(userEmail) AndAlso String.IsNullOrWhiteSpace(senderEmail) Then
          'callInfo &= Environment.NewLine & "debug error: no emails"
          Return 'can't do anything here
        End If
        If String.IsNullOrWhiteSpace(senderEmail) Then
          'callInfo &= Environment.NewLine & "debug error: no sender email"
          fromAddr = "no-reply@terrace.missouri.edu"
          ccAddr = Nothing
          emailText = "Dear " & transfereeName & "," & _
            "<br />" & _
            "A TerLoc project has been transferred to you from user " & sender.UserName & ". " & _
            "The project should show up on your Project Mgmt page at <a href='terrace.missouri.edu'>terrace.missouri.edu</a> with the name " & project.Name & "." & _
            "<br />" & _
            "If you don’t see the project (you may need to refresh the page if you already have it open), please forward this email to " & _
            "Kevin Atherton at <a href='mailto:athertonk@missouri.edu'>athertonk@missouri.edu</a> for troubleshooting. " & _
            "<br />" & _
            sender.UserName & " does not have an email in our system, so " & _
            "if you think you received this project in error or have other questions, please forward this email to Kevin Atherton." & _
            "<br />" & _
            "Thank you." & _
            "<br />" & _
            "<br />" & _
            addltext
        End If
        If String.IsNullOrWhiteSpace(userEmail) Then
          'callInfo &= Environment.NewLine & "debug error: no receiver email"
          toAddr = senderEmail
          ccAddr = Nothing
          fromAddr = "no-reply@terrace.missouri.edu"
          emailText = "The user, " & transfereeName & ", that you transferred a project to does not have an email in our system. " & _
            "Please contact them to verify the project was transferred correctly." & _
            "<br />" & _
            "Thank you."
        End If

        'callInfo &= Environment.NewLine & "debug error toAddr: " & toAddr
        'callInfo &= Environment.NewLine & "debug error fromAddr: " & fromAddr
        'callInfo &= Environment.NewLine & "debug error ccAddr: " & ccAddr
        localInfo = ""
        CF.SendEmail(toAddr, fromAddr, "TerLoc project transfer (" & project.Name & ")", emailText, localInfo, ccAddr)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
    End Sub

    Public Shared Sub DuplicateProject(ByVal projectId As Long, ByVal name As String, ByVal notes As String, ByRef callInfo As String)
      Dim project As EditProject
      Dim localInfo As String = ""
      Dim newProjectId As Long = -1
      Dim errStep As Integer = 0
      Try
        'Throw New NotImplementedException("Duplicate project is not implemented")

        'get original project
        localInfo = ""
        project = GetProjectById(projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        If project Is Nothing Then Throw New ArgumentNullException("Project was not found for id: " & projectId)
        errStep += 1
        project.Name = name
        errStep += 1
        project.Notes = notes
        errStep += 1

        localInfo = ""
        newProjectId = AddProject(project, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        errStep += 1
        If newProjectId < 0 Then
          callInfo &= Environment.NewLine & "New project id was not created."
          Return
        End If

        'callInfo &= Environment.NewLine & "error: " & project.Name ' ----- DEBUG
        'Return ' ----- DEBUG

        localInfo = ""
        DuplicateOtherTables(projectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("  {0} error (step: {2}): {1}  ", EH.GetCallerMethod(), ex.Message, errStep)
      End Try
    End Sub

    Public Shared Sub DuplicateOtherTables(ByVal oldProjectId As Long, ByVal newProjectId As Long, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim myContourHelper As New ContourHelper
        localInfo = ""
        myContourHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myContourRawHelper As New ContourRawHelper
        localInfo = ""
        myContourRawHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        'copy ContourXml ???
        'Dim myContourXmlHelper As New MDL.ContourXmlHelper
        'localInfo = ""
        'myContourXmlHelper.Duplicate(projectId, newProjectId, localInfo)
        'If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myDivideHelper As New DivideHelper
        localInfo = ""
        myDivideHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myEquipmentHelper As New EquipmentHelper
        localInfo = ""
        myEquipmentHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myHighPointHelper As New HighPointHelper
        localInfo = ""
        myHighPointHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myRidgelineHelper As New RidgelineHelper
        localInfo = ""
        myRidgelineHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myTerraceHelper As New TerraceHelper
        localInfo = ""
        myTerraceHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myFieldHelper As New FieldHelper
        localInfo = ""
        myFieldHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myTerraceErrorHelper As New TerraceErrorHelper
        localInfo = ""
        myTerraceErrorHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myTerraceReportHelper As New TerraceReportHelper
        localInfo = ""
        myTerraceReportHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim myWaterwayHelper As New WaterwayHelper
        localInfo = ""
        myWaterwayHelper.Duplicate(oldProjectId, newProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= String.Format("  {0} error: {1}  ", EH.GetCallerMethod(), ex.Message)
      End Try
    End Sub

#End Region

  End Class

End Namespace
