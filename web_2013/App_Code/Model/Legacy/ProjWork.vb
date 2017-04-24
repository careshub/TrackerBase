Option Explicit On
Option Strict On

Imports System.Data
Imports System.Data.SqlClient
Imports System.Reflection.MethodBase
Imports System.Web.Script.Serialization
Imports System.Transactions
Imports EH = ErrorHandler
Imports CommonFunctions
Imports GIS.GISToolsAddl
Imports GGeom = GeoAPI.Geometries

Namespace TerLoc.Model.Legacy

  ''' <summary>
  ''' IDs from db TERRACEFEATURE table
  ''' </summary>
  Public Enum TerraceFeature
    FARMAREA = 11
    GPSPOINTS = 13
    CONTOURPOINTS = 14
    WATERSOURCE = 26
    WATERSUPPLY = 27
    WATERWAYAREA = 28
    WEIRBOX = 29
    WELL = 30
    POOLAREA = 31
    RIDGELINE = 32
    WATERWAY = 33
    DIVIDE = 34
    OUTLET = 35
    KEYTERRACE = 36
    PARALLELTERRACE = 37
    UNDERGROUNDOUTLET = 39
    CONVENTIONALTERRACE = 40
    TERRACEAREA = 41
    MAXELEVATION = 42
    TERRACEERROR = 43
    TERRACEREPORT = 44
    CUSTOMTERRACE = 45
  End Enum

  Public Class ProjWorkRecord
    Private Const NULL_VALUE As Integer = -1

    Public PROJWORKID As Long = NULL_VALUE
    Public PROJID As String = "" '(255)
    Public FEATUREID As Integer = NULL_VALUE
    Public FEATURECOORDS As String = ""
    Public FEATURELABEL As String = ""
    Public FEATURELENGTH As String = ""
    Public PRACTICETYPE As Integer = NULL_VALUE
    Public TYPETERRACE As String = ""
    Public TERRACEDATE As DateTime = Nothing
    Public SCENARIOTYPE As Integer = NULL_VALUE
    Public LABEL_COORDS As String = ""
    Public LABEL_COORDSTER As String = ""
    Public COSTSHARE As Decimal = NULL_VALUE
    Public COST As String = ""
  End Class

  <Serializable()> _
  Public Class ProjWorkPackage
    Public projWorkRecord As ProjWorkRecord
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class ProjWorkHelper
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private dataSchema As String = CommonVariables.ProjectProductionSchema

    Public Function DeleteAll(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = String.Format(<a>
          DELETE FROM {0}.TERRACE_PROJWORK
          WHERE PROJID = @projectId
        </a>.Value, dataSchema)

        Dim parm As New SqlParameter("@projectId", SqlDbType.VarChar)
        parm.Value = projectId

        Using conn As New SqlConnection(dataConn)
          If Not conn.State = ConnectionState.Open Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()
            Try
              cmd.CommandText = cmdText
              cmd.Parameters.Add(parm)
              cmd.ExecuteNonQuery()
              retVal = True
            Catch ex As Exception
              Throw
            End Try
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("ProjWorkHelper DeleteAll error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function DeleteById(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByVal featureId As TerraceFeature, ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Try
        Dim cmdText As String = ""
        Using conn As New SqlConnection(dataConn)
          If Not conn.State = ConnectionState.Open Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()
            Try
              cmdText = String.Format(<a>
                DELETE FROM {0}.TERRACE_PROJWORK 
                WHERE [PROJID] = @projectId AND [FEATUREID] = @featureId
              </a>.Value, dataSchema)

              cmd.Parameters.Add("@projectId", SqlDbType.VarChar).Value = CStr(projectId)
              cmd.Parameters.Add("@featureId", SqlDbType.BigInt).Value = CLng(featureId)
              cmd.CommandText = cmdText
              cmd.ExecuteNonQuery()
              retVal = True
            Catch ex As Exception
              Throw
            End Try
          End Using
        End Using
      Catch ex As Exception
        callInfo &= String.Format("ProjWorkHelper DeleteById error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

    Public Function Delete(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByVal featureId As String, ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Try
        Dim cmdText As String = ""
        If featureId <> "" And IsNumeric(featureId) Then
          Using conn As New SqlConnection(dataConn)
            If Not conn.State = ConnectionState.Open Then conn.Open()
            Using cmd As SqlCommand = conn.CreateCommand()
              Try

                cmdText = String.Format(<a>
                    DELETE FROM {0}.TERRACE_PROJWORK WHERE PROJWORKID = @objId
                    </a>.Value, dataSchema)

                cmd.Parameters.Add("@objId", SqlDbType.BigInt).Value = CLng(featureId)
                cmd.CommandText = cmdText
                cmd.ExecuteNonQuery()
                retVal = True
              Catch ex As Exception
                Throw
              End Try
            End Using
          End Using
        End If
      Catch ex As Exception
        callInfo &= String.Format("ProjWorkHelper Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Insert"

    Public Sub InsertProjWork(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByRef feature As ProjWorkRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        InsertProjWorkToDatabase(feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Private Sub InsertProjWorkToDatabase(ByRef feature As ProjWorkRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        'Dim parm As SqlParameter
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            'I think only limited values are needed based on nulls in old db
            Dim insertFields As String = <a>
             [PROJID]
            ,[FEATUREID]
            ,[FEATURECOORDS]
            ,[FEATURELABEL]
            ,[FEATURELENGTH]
            ,[PRACTICETYPE]
            --,[TYPETERRACE]
            --,[TERRACEDATE]
            --,[SCENARIOTYPE]
            --,[LABEL_COORDS]
            --,[LABEL_COORDSTER]
            --,[COSTSHARE]
            --,[COST]
      </a>.Value
            Dim insertValues As String = <a>
             @PROJID
            ,@FEATUREID
            ,@FEATURECOORDS
            ,@FEATURELABEL
            ,@FEATURELENGTH
            ,@PRACTICETYPE
            --,@TYPETERRACE
            --,@TERRACEDATE
            --,@SCENARIOTYPE
            --,@LABEL_COORDS
            --,@LABEL_COORDSTER
            --,@COSTSHARE
            --,@COST
      </a>.Value

            cmdText = "INSERT INTO " & dataSchema & ".TERRACE_PROJWORK (" & insertFields & _
              ") Values (" & insertValues & ")  SET @newOid = SCOPE_IDENTITY()"

            With cmd.Parameters
              .Add("@PROJID", SqlDbType.VarChar).Value = feature.PROJID
              .Add("@FEATUREID", SqlDbType.Int).Value = feature.FEATUREID
              .Add("@FEATURECOORDS", SqlDbType.VarChar).Value = feature.FEATURECOORDS
              .Add("@FEATURELABEL", SqlDbType.VarChar).Value = feature.FEATURELABEL
              .Add("@FEATURELENGTH", SqlDbType.VarChar).Value = feature.FEATURELENGTH
              .Add("@PRACTICETYPE", SqlDbType.Int).Value = feature.PRACTICETYPE
              '.Add("@TYPETERRACE", SqlDbType.VarChar).Value = feature.TYPETERRACE
              '.Add("@TERRACEDATE", SqlDbType.DateTime).Value = feature.TERRACEDATE
              '.Add("@SCENARIOTYPE", SqlDbType.Int).Value = feature.SCENARIOTYPE
              '.Add("@LABEL_COORDS", SqlDbType.VarChar).Value = feature.LABEL_COORDS
              '.Add("@LABEL_COORDSTER", SqlDbType.VarChar).Value = feature.LABEL_COORDSTER
              'parm = New SqlParameter("@COSTSHARE", SqlDbType.Decimal)
              'With parm
              '  .Value = feature.COSTSHARE
              '  .Precision = 18
              '  .Scale = 2
              'End With
              '.Add(parm)
              '.Add("@COST", SqlDbType.VarChar).Value = feature.COST
            End With

            cmd.CommandText = cmdText
            Dim newOidParameter As New SqlParameter("@newOid", System.Data.SqlDbType.BigInt)
            newOidParameter.Direction = System.Data.ParameterDirection.Output
            cmd.Parameters.Add(newOidParameter)
            cmd.ExecuteNonQuery()
            feature.PROJWORKID = CLng(newOidParameter.Value)
          End Using
        End Using
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
        Throw
      End Try
    End Sub

#End Region

#Region "Fetch"

    Public Function GetProjectWorkTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>SELECT *
          FROM {0}.[TERRACE_PROJWORK] 
          WHERE PROJID = @projectId</a>.Value, dataSchema)

        Dim parms As New List(Of SqlParameter)
        Dim parm As New SqlParameter("@projectId", SqlDbType.VarChar)
        parm.Value = projectId
        parms.Add(parm)

        localInfo = ""
        retVal = GetDataTable(dataConn, cmdText, parms.ToArray, localInfo)
        If localInfo.IndexOf("error") > -1 Then callInfo &= localInfo

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

    Public Function ExtractProjWorkFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As ProjWorkPackage


      Return Nothing



      'Dim retVal As ProjWorkPackage = Nothing
      'Dim feature As ProjWorkRecord
      'Dim localInfo As String = ""

      ''TODO: finish this
      'Try
      '  Try
      '    Dim tmpObj As Object
      '    Dim tmpDate As Date
      '    feature = New ProjWorkRecord
      '    With feature
      '      .PROJWORKID = NullSafeLong(dr.Item("PROJWORKID"), -1)
      '      .PROJID = NullSafeString(dr.Item("PROJID"), "")
      '      .FEATUREID = NullSafeInteger(dr.Item("FEATUREID"), -1)
      '      .FEATURECOORDS = NullSafeString(dr.Item("FEATURECOORDS"), "")
      '      .FEATURELABEL = NullSafeString(dr.Item("FEATURELABEL"), "")
      '      .FEATURELENGTH = NullSafeString(dr.Item("FEATURELENGTH"), "")
      '      .PRACTICETYPE = NullSafeInteger(dr.Item("PRACTICETYPE"), -1)
      '      .TYPETERRACE = NullSafeString(dr.Item("TYPETERRACE"), "")
      '      tmpObj = dr.Item("TERRACEDATE")
      '      If Not IsDBNull(tmpObj) AndAlso Date.TryParse(tmpObj.ToString, tmpDate) Then
      '        .TERRACEDATE = tmpDate
      '      End If
      '      .SCENARIOTYPE = NullSafeInteger(dr.Item("SCENARIOTYPE"), -1)
      '      .LABEL_COORDS = NullSafeString(dr.Item("LABEL_COORDS"), "")
      '      .LABEL_COORDSTER = NullSafeString(dr.Item("LABEL_COORDSTER"), "")
      '      .COSTSHARE = NullSafeDecimal(dr.Item("COSTSHARE"), -1)
      '      .COST = NullSafeString(dr.Item("COST"), "")
      '    End With
      '  Catch ex As Exception
      '    Throw New Exception("Extract ProjWork Package (" & callInfo & ")", ex)
      '  End Try

      '  retVal = New ProjWorkPackage
      '  With retVal
      '    .projWorkRecord = feature
      '    .info = ""
      '  End With

      'Catch ex As Exception
      '  callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      'End Try
      'Return retVal
    End Function

    Public Function ExtractProjWorkRecordFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As ProjWorkRecord
      Dim retVal As ProjWorkRecord = Nothing
      Dim localInfo As String = ""
      Try
        Dim tmpObj As Object
        Dim tmpDate As DateTime
        retVal = New ProjWorkRecord
        With retVal
          .PROJWORKID = NullSafeLong(dr.Item("PROJWORKID"), -1)
          .PROJID = NullSafeString(dr.Item("PROJID"), "")
          .FEATUREID = NullSafeInteger(dr.Item("FEATUREID"), -1)
          .FEATURECOORDS = NullSafeString(dr.Item("FEATURECOORDS"), "")
          .FEATURELABEL = NullSafeString(dr.Item("FEATURELABEL"), "")
          .FEATURELENGTH = NullSafeString(dr.Item("FEATURELENGTH"), "")
          .PRACTICETYPE = NullSafeInteger(dr.Item("PRACTICETYPE"), -1)
          .TYPETERRACE = NullSafeString(dr.Item("TYPETERRACE"), "")
          tmpObj = dr.Item("TERRACEDATE")
          If Not IsDBNull(tmpObj) AndAlso DateTime.TryParse(tmpObj.ToString, tmpDate) Then
            .TERRACEDATE = tmpDate
          End If
          .SCENARIOTYPE = NullSafeInteger(dr.Item("SCENARIOTYPE"), -1)
          .LABEL_COORDS = NullSafeString(dr.Item("LABEL_COORDS"), "")
          .LABEL_COORDSTER = NullSafeString(dr.Item("LABEL_COORDSTER"), "")
          .COSTSHARE = NullSafeDecimal(dr.Item("COSTSHARE"), -1)
          .COST = NullSafeString(dr.Item("COST"), "")
        End With

      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
      Return retVal
    End Function

#End Region

  End Class

End Namespace