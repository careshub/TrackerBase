Option Strict On

#Region "Imports"

Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports System.Web.Script.Serialization
Imports System.Transactions
Imports EH = ErrorHandler
Imports CommonFunctions
Imports CV = CommonVariables 
Imports GeoAPI.Geometries
Imports GTA = GIS.GISToolsAddl
Imports NetTopologySuite.Operation.Polygonize
Imports SNAP = NetTopologySuite.Operation.Overlay.Snap
Imports NetTopologySuite.Geometries.Utilities 

#End Region

Namespace TerLoc.Model

  ''' <summary>
  ''' Updateable attributes for a field that can be selectively used wherever updates are performed
  ''' </summary> 
  <Serializable()> _
  Public Class FieldRecord
    Private Const NULL_VALUE As Integer = -1
    Public info As String 'Use for error messages, stack traces, etc.

    Public ObjectID As Long = -1 'LMU/ProjectDatum table
    Public Notes As String = ""  'ProjectDatum table 'this group from here on is from form
    Public FieldName As String = ""  'LMU table
    Public SoilKey As String = ""
    Public WatershedCode As String = ""
    Public FsaFarmNum As Integer = NULL_VALUE
    Public FsaTractNum As Integer = NULL_VALUE
    Public FsaFieldNum As Integer = NULL_VALUE
    Public CustomSoilKey As Integer = NULL_VALUE 'sent from form for now

    Public RegionId As String = "" 'for now no different from operation 'this group from here on is added before submission
    Public SubregionId As String = "" 'for now no different from operation
    Public Coords As String = "" 'not in db

    Public TotalArea As Single = NULL_VALUE 'this group from here on is calc'ed/added in code-behind
    Public Shape As String = ""

    Public Soils As String = ""
    Public SoilsDate As Long = NULL_VALUE 'send to sql as .ticks

  End Class

  <Serializable()> _
  Public Class FieldPackage
    Public fieldRecord As FieldRecord
    Public fieldDatum As ProjectDatum
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  <Serializable()> _
  Public Class FieldPackageList
    Public fields As List(Of FieldPackage)
    Public fieldShapesChanged As Boolean = False
    Public info As String 'Use for error messages, stack traces, etc.
  End Class
   
  Public Class FieldHelper 
    Private Shared dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private Shared dataSchema As String = CommonVariables.ProjectProductionSchema
    Private ReadOnly coordsPrecision As Integer = CV.CoordinatePrecision
    Private ReadOnly sqMtrsPerAcre As Double = CV.AcresToSquareMetersMultiplier

#Region "Create"

    Public Sub InsertFieldToDatabase(ByVal editField As FieldRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try

        Dim cmdText As String = ""
        Dim prm As SqlParameter

        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = "ObjectID " & _
              ",[FieldName] " & _
              ",[TotalArea] " & _
              ",[SoilKey] " & _
              ",[WatershedCode] " & _
              ",[FsaFarmNum] " & _
              ",[FsaFieldNum] " & _
              ",[FsaTractNum] " & _
              ",[Shape] " & _
              ",[CustomSoilKey] " & _
              ",[Soils] " & _
              ",[RegionId] " & _
              ",[SubregionId] " & _
              ",[SoilsDate] "

            Dim insertValues As String = "@ObjectID" & _
              ",@FieldName " & _
              ",@TotalArea " & _
              ",@SoilKey " & _
              ",@WatershedCode " & _
              ",@FsaFarmNum " & _
              ",@FsaFieldNum " & _
              ",@FsaTractNum " & _
              ",@Shape " & _
              ",@CustomSoilKey " & _
              ",@Soils " & _
              ",@RegionId " & _
              ",@SubregionId " & _
              ",@SoilsDate "

            cmdText = "INSERT INTO " & dataSchema & ".LandManagementUnit (" & insertFields & ") Values (" & insertValues & ")"

            ' (<ObjectID, bigint,>
            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = editField.ObjectID

            ' ,<FieldName, NVarChar(50),>
            cmd.Parameters.Add("@FieldName", SqlDbType.NVarChar).Value = editField.FieldName

            ' ,<TotalArea, decimal(38,1),>
            cmd.Parameters.Add("@TotalArea", SqlDbType.Decimal).Value = editField.TotalArea

            ' ,<SoilKey, NVarChar(12),>
            prm = cmd.Parameters.Add(New SqlParameter("@SoilKey", SqlDbType.NVarChar))
            prm.Value = If(String.IsNullOrWhiteSpace(editField.SoilKey), CObj(DBNull.Value), editField.SoilKey)

            ' ,<WatershedCode, NVarChar(12),>
            prm = cmd.Parameters.Add(New SqlParameter("@WatershedCode", SqlDbType.NVarChar))
            prm.Value = If(String.IsNullOrWhiteSpace(editField.WatershedCode), CObj(DBNull.Value), editField.WatershedCode)

            ' ,<FsaFarmNum, int,>
            prm = cmd.Parameters.Add(New SqlParameter("@FsaFarmNum", SqlDbType.Int))
            prm.Value = If(editField.FsaFarmNum < 0, CObj(DBNull.Value), editField.FsaFarmNum)

            ' ,<FsaFieldNum, smallint,>
            prm = cmd.Parameters.Add(New SqlParameter("@FsaFieldNum", SqlDbType.SmallInt))
            prm.Value = If(editField.FsaFieldNum < 0, CObj(DBNull.Value), editField.FsaFieldNum)

            ' ,<FsaTractNum, int,>
            prm = cmd.Parameters.Add(New SqlParameter("@FsaTractNum", SqlDbType.Int))
            prm.Value = If(editField.FsaTractNum < 0, CObj(DBNull.Value), editField.FsaTractNum)

            ' ,<Shape, NVarChar,>
            prm = cmd.Parameters.Add(New SqlParameter("@Shape", SqlDbType.NVarChar))
            prm.Value = If(String.IsNullOrWhiteSpace(editField.Shape), CObj(DBNull.Value), editField.Shape)

            ' ,<CustomSoilKey, bit,>)
            prm = cmd.Parameters.Add(New SqlParameter("@CustomSoilKey", SqlDbType.Bit))
            prm.Value = If(editField.CustomSoilKey <> 1, 0, 1)

            ' ,<Soils, nvarchar(max),>
            prm = cmd.Parameters.Add(New SqlParameter("@Soils", SqlDbType.NVarChar))
            prm.Value = If(String.IsNullOrWhiteSpace(editField.Soils), CObj(DBNull.Value), editField.Soils)

            ' ,<RegionId, nchar(2),>
            prm = cmd.Parameters.Add(New SqlParameter("@RegionId", SqlDbType.NChar))
            prm.Value = If(String.IsNullOrWhiteSpace(editField.RegionId), CObj(DBNull.Value), editField.RegionId)

            ' ,<SubregionId, nchar(3),>
            prm = cmd.Parameters.Add(New SqlParameter("@SubregionId", SqlDbType.NChar))
            prm.Value = If(String.IsNullOrWhiteSpace(editField.SubregionId), CObj(DBNull.Value), editField.SubregionId)

            ' ,<SoilsDate, datetime,>
            prm = cmd.Parameters.Add(New SqlParameter("@SoilsDate", SqlDbType.DateTime))
            prm.Value = If(editField.SoilsDate < 0, CObj(DBNull.Value), New DateTime(editField.SoilsDate))

            cmd.CommandText = cmdText
            If conn.State = ConnectionState.Closed Then conn.Open()
            cmd.ExecuteNonQuery()
            'callInfo &= Space(10) & "error cmdText: " & insertCommand.CommandText
          End Using
        End Using
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Public Sub AddField(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal fieldData As String, ByRef callInfo As String)
      Dim field As New FieldRecord
      Dim localInfo As String = ""
      Try
        Try
          field = DeserializeJson(Of FieldRecord)(fieldData)
        Catch ex As Exception
          callInfo &= EH.GetCallerMethod() & " error (field deserialization): " & ex.Message
          Return
        End Try

        localInfo = ""
        AddField(projectId, usrId, field, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Public Function AddField(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal field As FieldRecord, ByRef callInfo As String) As Long
      Dim localInfo As String = ""
      Dim datumId As Long = -1
      Try
        Try
          'Create new datum for field
          localInfo = ""
          datumId = ProjectDatumHelper.CreateNewProjectDatum(projectId, usrId, field.Notes, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          If datumId <= 0 Then 'failure
            Throw New ArgumentOutOfRangeException("ObjectID", datumId, "New datum id for field was out of bounds.")
          End If
          field.ObjectID = datumId

          If String.IsNullOrWhiteSpace(field.Shape) Then
            Dim wkb As String = ""
            If Not String.IsNullOrWhiteSpace(field.Coords) Then
              Dim newCoords As String 'Store clipped field coords
              Dim acres As Single = -1
              Dim spracres As Single = -1

              'Set coords precision 
              Dim coords As String = field.Coords
              coords = HttpContext.Current.Server.UrlDecode(coords)
              localInfo = ""
              coords = GTA.TrimCoords(coords, coordsPrecision, localInfo)
              If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

              'Calc/clip field and update
              localInfo = ""
              newCoords = CalculateNewFieldFromCoords(coords, acres, spracres, wkb, projectId, localInfo)
              If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
              With field
                .Coords = newCoords
                If acres >= 0 Then .TotalArea = acres
                If wkb.Length > 0 Then .Shape = wkb
              End With
            End If
          End If

        Catch ex As Exception
          callInfo &= ex.Message
          Return datumId
        End Try

        localInfo = ""
        InsertFieldToDatabase(field, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return datumId
    End Function

#End Region

#Region "Retrieve"

    ''' <summary>
    ''' Returns Guid from LMU table matching LMU id
    ''' </summary>
    Public Shared Function GetLmuGuidById(ByVal projectId As Long, ByVal lmuId As Long, ByRef callInfo As String) As Guid
      Dim retVal As Guid = Nothing
      Try
        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = String.Format("SELECT {1}.GUID " & _
                " FROM {0}.ProjectDatum AS {1} WHERE {1}.ObjectID=@featId", dataSchema, "PD")
            Dim param As New SqlParameter("@featId", SqlDbType.BigInt)
            param.Value = lmuId
            cmd.Parameters.Add(param)

            If conn.State = ConnectionState.Closed Then conn.Open()
            Dim readr As SqlDataReader = cmd.ExecuteReader
            While readr.Read
              Try
                If Not IsDBNull(readr("GUID")) Then
                  retVal = readr.GetGuid(0)
                End If
              Catch ex As Exception
              End Try
            End While
          End Using
        End Using

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    ''' <summary>
    ''' Returns ObjectId from LMU table matching Name ID.
    ''' </summary>
    Public Shared Function GetLmuIdByName(ByVal projectId As Long, ByVal fieldName As String, _
                             ByRef callInfo As String) As Integer
      Dim retVal As Integer = -1
      Try
        fieldName = FormatInsertValue(fieldName, GetType(String)).ToString
        If (fieldName = "NULL") Then
          callInfo &= "Name value is NULL" : Return retVal
        End If

        Dim cmdText As String = String.Format(<a>
          Select LMU.ObjectID
          FROM {0}.LandManagementUnit AS LMU 
          INNER JOIN {0}.ProjectDatum ON LMU.ObjectID= {0}.ProjectDatum.ObjectID 
          INNER JOIN {0}.Project ON {0}.Project.ObjectID= {0}.ProjectDatum.ProjectID 
          WHERE Project.ObjectID= @projectId AND LMU.FieldName= @fieldName)
                              </a>.Value, dataSchema)

        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()
            cmd.CommandText = cmdText
            cmd.Parameters.Add("@projectId", SqlDbType.BigInt).Value = projectId
            cmd.Parameters.Add("@fieldName", SqlDbType.NVarChar).Value = fieldName

            If conn.State = ConnectionState.Closed Then conn.Open()
            Dim readr As SqlDataReader = cmd.ExecuteReader
            While readr.Read
              Try
                retVal = CInt(readr(0))
              Catch ex As Exception 'error if cant convert to integer
                retVal = -1
              End Try
            End While
          End Using
        End Using

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function GetFields(ByVal projectId As Long, ByRef callInfo As String) As FieldPackageList
      Dim retVal As New FieldPackageList
      Dim retFields As List(Of FieldPackage)
      Dim localInfo As String = ""
      Try

        localInfo = ""
        retFields = GetFieldsList(projectId, localInfo)
        If localInfo.Contains("error") Then callInfo &= Environment.NewLine & "List: " & localInfo

        Dim lmuId As Long
        If retFields IsNot Nothing Then
          For Each fld As FieldPackage In retFields
            lmuId = fld.fieldRecord.ObjectID
          Next
        End If

        retVal.fields = retFields
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function GetFieldsList(ByVal projectId As Long, ByRef callInfo As String) As List(Of FieldPackage)
      Dim retVal As List(Of FieldPackage) = Nothing
      Dim retField As FieldPackage = Nothing
      Dim field As FieldRecord = Nothing
      Dim datum As ProjectDatum = Nothing
      Dim localInfo As String = ""
      Try
        Dim fields As DataTable

        Try
          localInfo = ""
          fields = New DataTable("Fields")
          fields = GetFieldsTable(projectId, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
          'callInfo &= String.Format("  {0} error: {1}  ", MethodIdentifier(), fields.Rows(0).Item("ObjectID"))

          'Fill in names for ids
          localInfo = ""
          fields = UpdateNames(fields, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)
        Catch ex As Exception
          Throw
        End Try

        For Each dr As DataRow In fields.Rows
          Try
            localInfo = ""
            retField = ExtractFieldFromTableRow(dr, localInfo)
            If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

            If retField IsNot Nothing Then
              If retVal Is Nothing Then retVal = New List(Of FieldPackage)
              retVal.Add(retField)
            End If
          Catch ex As Exception
            Throw
          End Try
        Next

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function GetFieldsTable(ByVal projectId As Long, ByRef callInfo As String, Optional ByVal featureId As Integer = Integer.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format("Select  *  " & _
        " FROM {0}.LandManagementUnit as {1}     " & _
        " INNER JOIN {0}.ProjectDatum as {2} ON {2}.ObjectID = {1}.ObjectID    " & _
        " WHERE {2}.ProjectID = {3}   " & _
        If(featureId > 0, " AND {1}.ObjectID = " & featureId & " ", " ORDER BY FieldName "), dataSchema, "FT", "PD", projectId)

        localInfo = ""
        retVal = GetDataTable(dataConn, cmdText, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function ExtractFieldFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As FieldPackage
      Dim retVal As New FieldPackage
      Dim field As New FieldRecord
      Dim datum As New ProjectDatum
      Dim localInfo As String = ""
      Try
        Dim tmpDateTime As DateTime

        Try
          With field
            .info = ""
            .ObjectID = NullSafeInteger(dr.Item("ObjectID"), -1)
            .FieldName = NullSafeString(dr.Item("FieldName"), "")     'can be null
            .TotalArea = NullSafeSingle(dr.Item("TotalArea"), -1)     'can be null
            .RegionId = NullSafeString(dr.Item("RegionId"), "")   'can be null
            .SubregionId = NullSafeString(dr.Item("SubregionId"), "")   'can be null
            .SoilKey = NullSafeString(dr.Item("SoilKey"), "")     'can be null
            .WatershedCode = NullSafeString(dr.Item("WatershedCode"), "")   'can be null
            .FsaFarmNum = NullSafeInteger(dr.Item("FsaFarmNum"), -1)  'can be null
            .FsaFieldNum = NullSafeInteger(dr.Item("FsaFieldNum"), -1)  'can be null
            .FsaTractNum = NullSafeInteger(dr.Item("FsaTractNum"), -1)  'can be null
            .Shape = NullSafeString(dr.Item("Shape"), "")   'can be null
            If Not String.IsNullOrWhiteSpace(.Shape) Then
              .Coords = GTA.GetCoordsForWkb(dr("Shape").ToString.Trim, localInfo)
              If localInfo.Contains("error") Then .info &= String.Format(" {0}: {1}", "coords", localInfo)
            End If
            .Soils = NullSafeString(dr.Item("Soils"), "")     'can be null
            If DateTime.TryParse(NullSafeString(dr.Item("SoilsDate"), ""), tmpDateTime) Then .SoilsDate = tmpDateTime.Ticks
            .CustomSoilKey = NullSafeInteger(dr.Item("CustomSoilKey"), 0)  'can be null
            .Notes = NullSafeString(dr.Item("Notes"), "")
          End With
        Catch ex As Exception
          Throw
        End Try

        localInfo = ""
        datum = ProjectDatumHelper.ExtractFromRow(dr, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

        With retVal
          .fieldRecord = field
          .fieldDatum = datum
          .info = ""
        End With

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

#End Region

#Region "Update"

    Public Sub UpdateFieldToDatabase(ByVal field As FieldRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try

        Dim cmdText As String = ""
        Dim prm As SqlParameter

        Using conn As New SqlConnection(dataConn)
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim flds As String() = New String() { _
              "[FieldName] ", "[TotalArea] ", _
              "[SoilKey] ", "[WatershedCode] ", _
              "[FsaFarmNum] ", "[FsaFieldNum] ", "[FsaTractNum] ", _
              "[Shape] ", "[CustomSoilKey] "}

            Dim vals As String() = New String() { _
              "@FieldName ", "@TotalArea ", _
              "@SoilKey ", "@WatershedCode ", _
              "@FsaFarmNum ", "@FsaFieldNum ", "@FsaTractNum ", _
              "@Shape ", "@CustomSoilKey "}

            Dim sql As New StringBuilder("UPDATE ")

            Try
              sql.Append("" & dataSchema & ".LandManagementUnit ")
              If flds.Length > 0 Then
                sql.Append(" SET ")
                For i As Integer = 0 To flds.Length - 1
                  sql.Append(flds(i) & "=")
                  sql.Append(vals(i))
                  If i <> flds.Length - 1 Then sql.Append(", ")
                Next
              End If
              sql = New StringBuilder(sql.ToString.TrimEnd(","c))

              sql.Append(" WHERE ObjectID = " & field.ObjectID)

            Catch ex As Exception
              callInfo &= EH.GetCallerMethod() & " sql creation error: " & ex.Message
              Return
            End Try

            ' (<ObjectID, bigint,>
            cmd.Parameters.Add("@ObjectID", SqlDbType.BigInt).Value = field.ObjectID

            ' ,<FieldName, nvarchar(50),>
            cmd.Parameters.Add("@FieldName", SqlDbType.NVarChar).Value = field.FieldName

            ' ,<TotalArea, decimal(38,1),>
            cmd.Parameters.Add("@TotalArea", SqlDbType.Decimal).Value = field.TotalArea

            ' ,<SoilKey, NVarChar(12),>
            prm = cmd.Parameters.Add(New SqlParameter("@SoilKey", SqlDbType.NVarChar))
            prm.Value = If(String.IsNullOrWhiteSpace(field.SoilKey), CObj(DBNull.Value), field.SoilKey)

            ' ,<WatershedCode, NVarChar(12),>
            prm = cmd.Parameters.Add(New SqlParameter("@WatershedCode", SqlDbType.NVarChar))
            prm.Value = If(String.IsNullOrWhiteSpace(field.WatershedCode), CObj(DBNull.Value), field.WatershedCode)

            ' ,<FsaFarmNum, int,>
            prm = cmd.Parameters.Add(New SqlParameter("@FsaFarmNum", SqlDbType.Int))
            prm.Value = If(field.FsaFarmNum < 0, CObj(DBNull.Value), field.FsaFarmNum)

            ' ,<FsaFieldNum, smallint,>
            prm = cmd.Parameters.Add(New SqlParameter("@FsaFieldNum", SqlDbType.SmallInt))
            prm.Value = If(field.FsaFieldNum < 0, CObj(DBNull.Value), field.FsaFieldNum)

            ' ,<FsaTractNum, int,>
            prm = cmd.Parameters.Add(New SqlParameter("@FsaTractNum", SqlDbType.Int))
            prm.Value = If(field.FsaTractNum < 0, CObj(DBNull.Value), field.FsaTractNum)

            ' ,<Shape, NVarChar,>
            prm = cmd.Parameters.Add(New SqlParameter("@Shape", SqlDbType.NVarChar))
            prm.Value = If(String.IsNullOrWhiteSpace(field.Shape), CObj(DBNull.Value), field.Shape)

            ' ,<CustomSoilKey, bit,>)
            prm = cmd.Parameters.Add(New SqlParameter("@CustomSoilKey", SqlDbType.Bit))
            prm.Value = If(Not String.IsNullOrWhiteSpace(field.SoilKey) AndAlso field.CustomSoilKey = 1, 1, 0)

            cmd.CommandText = sql.ToString
            If conn.State = ConnectionState.Closed Then conn.Open()
            cmd.ExecuteNonQuery()
          End Using
        End Using
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Public Sub EditField(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featureId As String, ByVal fielddata As String, ByRef callInfo As String)
      Dim field As New FieldRecord
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim isOkToUpdate = True
      Try
        Try
          field = DeserializeJson(Of FieldRecord)(fielddata)
          field.ObjectID = CInt(featureId)
        Catch ex As Exception
          callInfo &= EH.GetCallerMethod() & " error (field deserialization): " & ex.Message
          Return
        End Try

        localInfo = ""
        EditField(projectId, usrId, field, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Public Sub EditField(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal field As FieldRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim isOkToUpdate = True
      Try
        Try
          Dim newCoords As String 'Store clipped field coords
          Dim acres As Double = -1
          Dim spracres As Double = -1
          Dim wkb As String = ""

          'Set coords precision 
          Dim coords As String = field.Coords
          coords = HttpContext.Current.Server.UrlDecode(coords)
          localInfo = ""
          coords = GTA.TrimCoords(coords, coordsPrecision, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

          'Calc/clip field and update
          localInfo = ""
          newCoords = CalculateEditFieldFromCoords(coords, acres, spracres, wkb, projectId, field.ObjectID, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
          If String.IsNullOrWhiteSpace(newCoords) Then isOkToUpdate = False
          With field
            .Coords = newCoords
            If acres >= 0 Then .TotalArea = CSng(acres)
            If wkb.Length > 0 Then .Shape = wkb
          End With

        Catch ex As Exception
          callInfo &= ex.Message
          Return
        End Try

        If isOkToUpdate Then
          localInfo = ""
          UpdateFieldToDatabase(field, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
        End If

        Try
          'Update Project Datum 
          localInfo = ""
          Dim pdUpdated As Integer = UpdateProjectDatumByDatumId(field.ObjectID, usrId, field.Notes, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Catch ex As Exception
        End Try

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

#End Region

#Region "Delete"

    Public Sub DeleteField(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByVal featureId As String, ByRef callInfo As String)

      Dim localInfo As String = ""
      Dim datumId As Integer = -1
      Dim numRecs As Integer = 0
      Try
        Dim cmdText As String = ""
        If featureId <> "" And IsNumeric(featureId) Then
          cmdText = "DELETE FROM " & dataSchema & ".ProjectDatum WHERE ObjectID = " & featureId

          Using conn As New SqlConnection(dataConn)
            Using cmd As SqlCommand = conn.CreateCommand()
              cmd.CommandText = cmdText
              If conn.State = ConnectionState.Closed Then conn.Open()
              numRecs = cmd.ExecuteNonQuery
            End Using
          End Using
        End If
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

#End Region

#Region "Other"
     
    ''' <summary>
    ''' Calculate acres, spreadable acres, and well-known binary string from coords
    ''' </summary> 
    ''' <returns>Clipped coords</returns>
    ''' <remarks></remarks>
    Public Function CalculateEditFieldFromCoords(ByVal coords As String, ByRef acres As Double, ByRef spracres As Double, ByRef wkb As String,
                                               ByVal projectId As Long, ByVal featureId As Long, ByRef callInfo As String) As String
      Dim retVal As String = "" 'return clipped coords string
      Dim localInfo As String = ""
      Dim funcCallInfo As New StringBuilder()
      Try
        'create poly 
        localInfo = ""
        'callInfo &= String.Format(" error: {0}", coords) 'DEBUG
        Dim newPoly As IMultiPolygon = GTA.CreateMultipolyFromCoordString(coords, localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)
        If newPoly Is Nothing Then
          callInfo = "error: Points did not create a valid shape. Make sure paths don't cross themselves or each other. " + funcCallInfo.ToString
          'TODO: add "Use edit to add a geometry to the field."
          Return retVal
        End If

        localInfo = ""
        retVal = CalculateEditFieldFromPoly(newPoly, acres, spracres, wkb, projectId, featureId, localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      callInfo &= funcCallInfo.ToString
      Return retVal
    End Function

    Public Function CalculateEditFieldFromPoly(ByVal newPoly As IMultiPolygon, ByRef acres As Double, ByRef spracres As Double, ByRef wkb As String,
                                               ByVal projectId As Long, ByVal featureId As Long, ByRef callInfo As String) As String
      Dim retVal As String = "" 'return clipped coords string
      Dim localInfo As String = ""
      Dim funcCallInfo As New StringBuilder()
      Try
        localInfo = ""
        acres = GTA.GetAreaFromLatLngMultiPolygon(newPoly, localInfo) / sqMtrsPerAcre
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)
        localInfo = ""
        retVal = GTA.GetCoordsStringFromGeom(newPoly, localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)
        localInfo = ""
        retVal = GTA.TrimCoords(retVal, coordsPrecision, localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)

        'Convert field shape to wkb
        localInfo = ""
        wkb = ""
        If newPoly IsNot Nothing Then wkb = GTA.ConvertGeometryToWkb(CType(newPoly, IGeometry), localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      callInfo &= funcCallInfo.ToString
      Return retVal
    End Function

    ''' <summary>
    ''' Calculate acres, spreadable acres, and well-known binary string from coords
    ''' </summary> 
    ''' <returns>Clipped coords</returns>
    ''' <remarks></remarks>
    Public Function CalculateNewFieldFromCoords(ByVal coords As String, ByRef acres As Single, ByRef spracres As Single, ByRef wkb As String,
                                               ByVal projectId As Long, ByRef callInfo As String) As String
      Dim retVal As String = "" 'return clipped coords string
      Dim localInfo As String = ""
      Dim funcCallInfo As New StringBuilder()
      Try
        'create poly
        Dim newPoly As IMultiPolygon = GTA.CreateMultipolyFromCoordString(coords, localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo) : localInfo = ""
        If newPoly Is Nothing Then
          callInfo = "error: Points did not create a valid shape. Make sure paths don't cross themselves." + funcCallInfo.ToString
          Return ""
        End If

        localInfo = ""
        retVal = CalculateNewFieldFromPoly(newPoly, acres, spracres, wkb, projectId, localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message & funcCallInfo.ToString
      End Try
      Return retVal
    End Function

    Public Function CalculateNewFieldFromPoly(ByVal newPoly As IMultiPolygon, ByRef acres As Single, _
              ByRef spracres As Single, ByRef wkb As String, ByVal projectId As Long, ByRef callInfo As String) As String
      Dim retVal As String = "" 'return trimmed coords string
      Dim localInfo As String = ""
      Dim funcCallInfo As New StringBuilder()
      Try

        acres = CSng(GTA.GetAreaFromLatLngMultiPolygon(newPoly, localInfo) / sqMtrsPerAcre)
        localInfo = ""
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)
        localInfo = ""
        retVal = GTA.GetCoordsStringFromGeom(newPoly, localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)
        localInfo = ""
        retVal = GTA.TrimCoords(retVal, coordsPrecision, localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo)

        'Convert field shape to wkb
        localInfo = ""
        wkb = GTA.ConvertGeometryToWkb(CType(newPoly, IGeometry), localInfo)
        If localInfo.ToLower.Contains("error") Then funcCallInfo.Append(Environment.NewLine & localInfo) : localInfo = ""

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message & funcCallInfo.ToString
      End Try
      Return retVal
    End Function

    Public Function SnapMultipoly(ByVal poly As IGeometry, ByRef callInfo As String) As IMultiPolygon
      Dim retVal As IMultiPolygon = Nothing
      Dim localInfo As String = ""
      Try
        Dim snappd As IGeometry = SNAP.GeometrySnapper.SnapToSelf(CType(poly, IGeometry), 0.000001, True) 'deal with slivers
        retVal = GTA.CreateMultipolyFromCoordString(GTA.GetCoordsStringFromGeom(snappd, localInfo), localInfo)
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function Polygonize(ByVal geometry As IGeometry, ByRef callInfo As String) As IGeometry
      Dim polyArray As ICollection(Of IGeometry) = Nothing
      Try
        Dim lines As ICollection(Of IGeometry) = LineStringExtracter.GetLines(geometry)
        Dim polygonizer As New Polygonizer
        polygonizer.Add(lines)
        Dim polys As ICollection(Of IGeometry) = polygonizer.GetPolygons
        polyArray = NetTopologySuite.Geometries.GeometryFactory.ToGeometryArray(polys)
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return geometry.Factory.CreateGeometryCollection(polyArray.ToArray)
    End Function

    Public Sub Duplicate(ByVal origProjectId As Long, ByVal newProjectId As Long, Optional ByRef callInfo As String = "")
      Dim localInfo As String = ""
      Try
        Dim feat As FieldRecord
        Dim pkg As FieldPackage
        Dim featureList As FieldPackageList

        localInfo = ""
        Dim usrId As Guid = UserHelper.GetCurrentUser(Nothing).UserId
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        localInfo = ""
        featureList = GetFields(origProjectId, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo

        Dim origFeatId As Long
        Dim newFeatId As Long
        For Each pkg In featureList.fields
          feat = pkg.fieldRecord
          origFeatId = feat.ObjectID

          localInfo = ""
          newFeatId = AddField(newProjectId, usrId, feat, localInfo)
          If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & "orig id " & origFeatId & ", new id " & newFeatId & ": " & localInfo
        Next
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

#End Region

  End Class

End Namespace