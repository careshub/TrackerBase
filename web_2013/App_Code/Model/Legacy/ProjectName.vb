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

  Public Class ProjectNameRecord
    Private Const NULL_VALUE As Integer = -1

    Public PROJECTNMEID As Long = NULL_VALUE
    Public PROJID As String = "" '(255)
    Public PROJECTNAME As String = "" '(255)
    Public PROJECTDATE As DateTime ' smalldatetime
    Public MINX As Double = NULL_VALUE
    Public MINY As Double = NULL_VALUE
    Public MAXX As Double = NULL_VALUE
    Public MAXY As Double = NULL_VALUE
    Public CLIENTNAME As String = "" '(255)
    Public PLANTECH As String = "" '(255)
    Public NOMACHINES As Integer = 1
    Public ROWWIDTH As Integer = 12
    Public NOROWS As Integer = 30
    Public MAXCHANNELVEL As Long = 2
    Public MANNINGS As Double = 0.035
    Public SIDESLOPE As Integer = 10
    Public RUNOFFCOEFF As Decimal = 0.7D '(18,2)
    Public RUNOFFINTENSITY As Integer = 7
    Public BOTTERRACE_CHANNEL As Integer = 0
    Public LANDSLOPE As Decimal = 0.07D '(18,3)
    Public TERRACESPACE As Long = NULL_VALUE
    Public WIDTHMAX As Double = NULL_VALUE
    Public WIDTHMIN As Double = NULL_VALUE
  End Class

  <Serializable()> _
  Public Class ProjectNamePackage
    Public projectNameRecord As ProjectNameRecord
    Public info As String 'Use for error messages, stack traces, etc.
  End Class

  Public Class ProjectNameHelper
    Private dataConn As String = CommonFunctions.GetBaseDatabaseConnString
    Private dataSchema As String = CommonVariables.ProjectProductionSchema

    Public Function DeleteAll(ByVal projectId As Long, ByVal usrId As Guid _
                      , ByRef callInfo As String) As Boolean
      Dim retVal As Boolean = False
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = String.Format(<a>
          DELETE FROM {0}.TERRACE_PROJECTNAME
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
        callInfo &= String.Format("ProjectNameHelper DeleteAll error: {0}", ex.ToString)
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
                    DELETE FROM {0}.TERRACE_PROJECTNAME WHERE PROJECTNMEID = @objId
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
        callInfo &= String.Format("ProjectNameHelper Delete error: {0}", ex.ToString)
      End Try
      Return retVal
    End Function

#Region "Insert"

    Public Sub InsertProjectName(ByVal projectId As Long, ByVal usrId As Guid _
                                , ByRef feature As ProjectNameRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        InsertProjectNameToDatabase(feature, localInfo)
        If localInfo.ToLower.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      End Try
    End Sub

    Private Sub InsertProjectNameToDatabase(ByRef feature As ProjectNameRecord, ByRef callInfo As String)
      Dim localInfo As String = ""
      Try
        Dim cmdText As String = ""
        Dim parm As SqlParameter
        Using conn As New SqlConnection(dataConn)
          If conn.State = ConnectionState.Closed Then conn.Open()
          Using cmd As SqlCommand = conn.CreateCommand()

            Dim insertFields As String = <a>
            [PROJID]
           ,[PROJECTNAME]
           ,[PROJECTDATE]
           ,[MINX]
           ,[MINY]
           ,[MAXX]
           ,[MAXY]
           ,[CLIENTNAME]
           ,[PLANTECH]
           ,[NOMACHINES]
           ,[ROWWIDTH]
           ,[NOROWS]
           ,[MAXCHANNELVEL]
           ,[MANNINGS]
           ,[SIDESLOPE]
           ,[RUNOFFCOEFF]
           ,[RUNOFFINTENSITY]
           ,[BOTTERRACE_CHANNEL]
           ,[LANDSLOPE]
           ,[TERRACESPACE]
           ,[WIDTHMAX]
           ,[WIDTHMIN]
      </a>.Value
            Dim insertValues As String = <a>
            @PROJID
           ,@PROJECTNAME
           ,@PROJECTDATE
           ,@MINX
           ,@MINY
           ,@MAXX
           ,@MAXY
           ,@CLIENTNAME
           ,@PLANTECH
           ,@NOMACHINES
           ,@ROWWIDTH
           ,@NOROWS
           ,@MAXCHANNELVEL
           ,@MANNINGS
           ,@SIDESLOPE
           ,@RUNOFFCOEFF
           ,@RUNOFFINTENSITY
           ,@BOTTERRACE_CHANNEL
           ,@LANDSLOPE
           ,@TERRACESPACE
           ,@WIDTHMAX
           ,@WIDTHMIN
      </a>.Value

            cmdText = "INSERT INTO " & dataSchema & ".TERRACE_PROJECTNAME (" & insertFields & _
              ") Values (" & insertValues & ")  SET @newOid = SCOPE_IDENTITY()"

            With cmd.Parameters
              .Add("@PROJID", SqlDbType.VarChar).Value = feature.PROJID
              .Add("@PROJECTNAME", SqlDbType.VarChar).Value = feature.PROJECTNAME
              .Add("@PROJECTDATE", SqlDbType.SmallDateTime).Value = feature.PROJECTDATE
              .Add("@MINX", SqlDbType.Float).Value = feature.MINX ' If(feature.MINX < 0, DBNull.Value, feature.MINX)
              .Add("@MINY", SqlDbType.Float).Value = feature.MINY
              .Add("@MAXX", SqlDbType.Float).Value = feature.MAXX
              .Add("@MAXY", SqlDbType.Float).Value = feature.MAXY
              .Add("@CLIENTNAME", SqlDbType.VarChar).Value = feature.CLIENTNAME
              .Add("@PLANTECH", SqlDbType.VarChar).Value = feature.PLANTECH
              .Add("@NOMACHINES", SqlDbType.Int).Value = feature.NOMACHINES
              .Add("@ROWWIDTH", SqlDbType.Int).Value = feature.ROWWIDTH
              .Add("@NOROWS", SqlDbType.Int).Value = feature.NOROWS
              .Add("@MAXCHANNELVEL", SqlDbType.BigInt).Value = feature.MAXCHANNELVEL
              .Add("@MANNINGS", SqlDbType.Float).Value = feature.MANNINGS
              .Add("@SIDESLOPE", SqlDbType.Int).Value = feature.SIDESLOPE
              parm = New SqlParameter("@RUNOFFCOEFF", SqlDbType.Decimal)
              With parm
                .Value = feature.RUNOFFCOEFF
                .Precision = 18
                .Scale = 2
              End With
              .Add(parm)
              .Add("@RUNOFFINTENSITY", SqlDbType.Int).Value = feature.RUNOFFINTENSITY
              .Add("@BOTTERRACE_CHANNEL", SqlDbType.Int).Value = feature.BOTTERRACE_CHANNEL
              parm = New SqlParameter("@LANDSLOPE", SqlDbType.Decimal)
              With parm
                .Value = feature.LANDSLOPE
                .Precision = 18
                .Scale = 3
              End With
              .Add(parm)
              .Add("@TERRACESPACE", SqlDbType.BigInt).Value = feature.TERRACESPACE
              .Add("@WIDTHMAX", SqlDbType.Float).Value = feature.WIDTHMAX
              .Add("@WIDTHMIN", SqlDbType.Float).Value = feature.WIDTHMIN
            End With

            cmd.CommandText = cmdText
            Dim newOidParameter As New SqlParameter("@newOid", System.Data.SqlDbType.BigInt)
            newOidParameter.Direction = System.Data.ParameterDirection.Output
            cmd.Parameters.Add(newOidParameter)
            cmd.ExecuteNonQuery()
            feature.PROJECTNMEID = CLng(newOidParameter.Value)
          End Using
        End Using
      Catch ex As Exception
        callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
        Throw
      End Try
    End Sub

#End Region

#Region "Fetch"

    Public Function GetProjectNamesTable(ByVal projectId As Long, ByRef callInfo As String, _
                                       Optional ByVal featureId As Long = Long.MinValue) As DataTable
      Dim retVal As DataTable = Nothing
      Dim cmdText As String = ""
      Try
        Dim localInfo As String = ""
        cmdText = String.Format(<a>SELECT *
          FROM {0}.[TERRACE_PROJECTNAME] 
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

    Public Function ExtractProjectNameFromTableRow(ByVal dr As DataRow, ByRef callInfo As String) As ProjectNamePackage


      Return Nothing



      'Dim retVal As ProjectNamePackage = Nothing
      'Dim feature As ProjectNameRecord
      'Dim localInfo As String = ""


      'TODO: finish this
      'Try
      '  Try
      '    feature = New ProjectNameRecord
      '    With feature
      '      '.ContID = NullSafeLong(dr.Item("CONTID"), -1)
      '      '.PROJID = NullSafeLong(dr.Item("PROJID"), -1)
      '      '.XmlType = NullSafeString(dr.Item("XMLTYPE"), "")
      '      '.XmlContour = NullSafeString(dr.Item("XMLCONTOUR"), "")
      '      '.XmlDoc = If(Not String.IsNullOrWhiteSpace(.XmlContour), New XDocument(.XmlContour), Nothing)

      '      .PROJID = NullSafeString(dr.Item("PROJID"), "")
      '      .PROJECTNAME = NullSafeString(dr.Item("PROJECTNAME"), "")
      '      .PROJECTDATE = Date.Parse(NullSafeString(dr.Item("PROJECTDATE"), ""))
      '      .MINX = 1
      '      .MINY = 2
      '      .MAXX = 3
      '      .MAXY = 4
      '      .CLIENTNAME = "" '(255)
      '      .PLANTECH = "" '(255)
      '      .NOMACHINES = 1
      '      .ROWWIDTH = 30
      '      .NOROWS = 12
      '      .MAXCHANNELVEL = 2
      '      .MANNINGS = 0.035
      '      .SIDESLOPE = 10
      '      .RUNOFFCOEFF = 0.7D '(18,2)
      '      .RUNOFFINTENSITY = 7
      '      .BOTTERRACE_CHANNEL = 0
      '      .LANDSLOPE = 0.07D '(18,3)
      '      .TERRACESPACE = 1 'or null
      '      .WIDTHMAX = 180
      '      .WIDTHMIN = 0
      '    End With
      '  Catch ex As Exception
      '    Throw New Exception("Extract ProjectNameRecord (" & callInfo & ")", ex)
      '  End Try

      '  retVal = New ProjectNamePackage
      '  With retVal
      '    .projectNameRecord = feature
      '    .info = ""
      '  End With

      'Catch ex As Exception
      '  callInfo &= EH.GetCallerMethod() & " error: " & ex.Message
      'End Try
      'Return retVal
    End Function

#End Region

  End Class

End Namespace