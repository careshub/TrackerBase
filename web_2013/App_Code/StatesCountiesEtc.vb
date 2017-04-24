
Imports System.Data
Imports System.Data.SqlClient  
Imports System.Reflection.MethodBase

Imports CF = CommonFunctions
Imports CV = CommonVariables
Imports EH = ErrorHandler

Public Class StatesCountiesEtc

  Private Shared mapDataConnStr As String = CV.MapDataConnStr

  ''' <summary>
  ''' Gets the state Abbr for the full statename.
  ''' </summary>
  Public Shared Function GetStateAbbr(ByRef stateName As String, ByRef callInfo As String) As String  
    Dim retVal As String = ""
    Dim cmdText As String = <a>
        SELECT Distinct(StateAbbr) as stateAbbr
        FROM usaSTATE10WM WHERE (STATENAME = @stateName )
                            </a>.Value
    Try 
      Using conn As New SqlConnection(mapDataConnStr)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = cmdText
          With cmd.Parameters
            .Add("@stateName", SqlDbType.NVarChar).Value = stateName.Trim
          End With

          If cmd.Connection.State <> ConnectionState.Open Then conn.Open()
          Using reader As SqlDataReader = cmd.ExecuteReader
            While reader.Read()
              retVal = CF.NullSafeString(reader("stateAbbr"), "")
            End While
          End Using
        End Using
      End Using

    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Get lat/lon bounding box for a region.
  ''' </summary> 
  Public Shared Function GetRegionLatLons(ByRef stateName As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim cmdText As String = <a>
        SELECT minLon, minLat, maxLon, maxLat 
        FROM usaSTATE10WM WHERE (STATENAME = @stateName )
                            </a>.Value
    Try
      Dim minLon As String = ""
      Dim minLat As String = ""
      Dim maxLon As String = ""
      Dim maxLat As String = ""
      Using conn As New SqlConnection(mapDataConnStr)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = cmdText
          With cmd.Parameters
            .Add("@stateName", SqlDbType.NVarChar).Value = stateName.Trim
          End With

          If cmd.Connection.State <> ConnectionState.Open Then conn.Open()
          Using reader As SqlDataReader = cmd.ExecuteReader
            While reader.Read()
              minLon = CF.NullSafeString(reader("minLon"), "")
              minLat = CF.NullSafeString(reader("minLat"), "")
              maxLon = CF.NullSafeString(reader("maxLon"), "")
              maxLat = CF.NullSafeString(reader("maxLat"), "")
            End While
          End Using
        End Using
      End Using
      If String.IsNullOrWhiteSpace(minLon) OrElse String.IsNullOrWhiteSpace(minLat) OrElse _
         String.IsNullOrWhiteSpace(maxLon) OrElse String.IsNullOrWhiteSpace(maxLat) Then
        retVal = ""
      Else
        retVal = String.Format("{0},{1} {2},{3}", minLon, minLat, maxLon, maxLat)
      End If

    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Get lat/lon bounding box for a subregion.
  ''' </summary> 
  Public Shared Function GetSubRegionLatLons(ByRef stateName As String, ByRef cntyName As String, ByRef callInfo As String) As String
     Dim retVal As String = ""
    'check against both name columns
    Dim cmdText As String = <a>
        SELECT minLon, minLat, maxLon, maxLat 
        FROM usaCOUNTY10WM WHERE (STATENAME = @stateName 
           AND ( [CountyName] = @cntyName OR [CountyShortNAME] = @cntyName ))
                            </a>.Value
    Try
      Dim minLon As String = ""
      Dim minLat As String = ""
      Dim maxLon As String = ""
      Dim maxLat As String = ""
      Using conn As New SqlConnection(mapDataConnStr)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = cmdText
          With cmd.Parameters
            .Add("@stateName", SqlDbType.NVarChar).Value = stateName.Trim
            .Add("@cntyName", SqlDbType.NVarChar).Value = cntyName.Trim
          End With

          If cmd.Connection.State <> ConnectionState.Open Then conn.Open()
          Using reader As SqlDataReader = cmd.ExecuteReader
            While reader.Read()
              minLon = CF.NullSafeString(reader("minLon"), "")
              minLat = CF.NullSafeString(reader("minLat"), "")
              maxLon = CF.NullSafeString(reader("maxLon"), "")
              maxLat = CF.NullSafeString(reader("maxLat"), "")
            End While
          End Using
        End Using
      End Using
      If String.IsNullOrWhiteSpace(minLon) OrElse String.IsNullOrWhiteSpace(minLat) OrElse _
         String.IsNullOrWhiteSpace(maxLon) OrElse String.IsNullOrWhiteSpace(maxLat) Then
        retVal = ""
      Else
        retVal = String.Format("{0},{1} {2},{3}", minLon, minLat, maxLon, maxLat)
      End If

    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Fills state info fields that can be found based on other parameters.
  ''' </summary>
  Public Shared Sub GetAllStateInfoFromSomeStateInfo(ByRef stateFid As String, ByRef stateAbbr As String, _
                                                          ByRef stateName As String, ByRef callInfo As String)
    'Dim cmdText As String = "SELECT [STFID],[StateAbbr],[StateName] FROM [MapData].[MapData].[usaSTATE10WM] WHERE STFID='" & stateFid & _
    '        "' OR {fn LCASE(StateAbbr)}='" & stateAbbr.ToLower & "' OR {fn LCASE(STATENAME)}='" & stateName.ToLower & "'"

    Try
      Dim cmdText As String = <a>
            SELECT [STFID],[StateAbbr],[StateName] 
            FROM [MapData].[MapData].[usaSTATE10WM] 
            WHERE STFID=@stfid OR {fn LCASE(StateAbbr)}=@stateAbbr OR {fn LCASE(STATENAME)}=@stateName 
                              </a>.Value

      Using conn As New SqlConnection(mapDataConnStr)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = cmdText
          With cmd.Parameters
            .Add("@stfid", SqlDbType.NVarChar).Value = stateFid
            .Add("@stateAbbr", SqlDbType.NVarChar).Value = stateAbbr
            .Add("@stateName", SqlDbType.NVarChar).Value = stateName
          End With

          If cmd.Connection.State <> ConnectionState.Open Then conn.Open()
          Using dataReadr As SqlDataReader = cmd.ExecuteReader
            While dataReadr.Read()
              'only overwrite if something was found
              Dim tmp As String = CF.NullSafeString(dataReadr("STFID"), "")
              If Not String.IsNullOrWhiteSpace(tmp) Then stateFid = tmp
              tmp = CF.NullSafeString(dataReadr("StateAbbr"), "")
              If Not String.IsNullOrWhiteSpace(tmp) Then stateAbbr = tmp
              tmp = CF.NullSafeString(dataReadr("StateName"), "")
              If Not String.IsNullOrWhiteSpace(tmp) Then stateName = tmp
            End While
          End Using
        End Using
      End Using
    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.Message)
    End Try
  End Sub

  ''' <summary>
  ''' Fills county info fields that can be found based on other parameters.
  ''' </summary>
  Public Shared Sub GetAllCountyInfoFromSomeCountyInfo(ByRef cntyFid As String, ByRef cntyName As String, _
                                      ByVal stateFid As String, ByVal stateName As String, ByRef callInfo As String)
    Try
      If stateFid.Trim = "" And stateName.Trim = "" Then callInfo &= " error: " & "No state info" : Exit Sub
      If cntyFid.Trim = "" And cntyName.Trim = "" Then callInfo &= " error: " & "No county info" : Exit Sub

      Dim cmdText As String
      If stateFid.Length = 1 Then stateFid = "0" & stateFid
      If cntyFid.Length > 0 Then
        If cntyFid.Length = 1 Then cntyFid = "00" & cntyFid
        If cntyFid.Length = 2 Then cntyFid = "0" & cntyFid
      End If

      cmdText = "SELECT STFID,CountyShortNAME FROM usaCOUNTY10WM WHERE (" & Space(1)
      If stateFid.Length > 0 Then
        cmdText &= " STFID LIKE '" & stateFid & "%'" & Space(1)
      End If
      If stateFid.Length > 0 AndAlso stateName.Length > 0 Then cmdText &= " OR " & Space(1)
      If stateName.Length > 0 Then
        cmdText &= " {fn LCASE(STATENAME)}='" & stateName.ToLower & "'" & Space(1)
      End If
      cmdText &= ") AND (" & Space(1)
      If cntyFid.Length > 0 Then
        cmdText &= " STFID LIKE '%" & cntyFid & "'" & Space(1)
      End If
      If cntyFid.Length > 0 AndAlso cntyName.Length > 0 Then cmdText &= Space(1) & " OR " & Space(1)
      If cntyName.Length > 0 Then
        cmdText &= " {fn LCASE(CountyShortNAME)} LIKE '" & cntyName.ToLower & "%'" & Space(1)
      End If
      cmdText &= ")" & Space(1)

      Using conn As New SqlConnection(mapDataConnStr)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = cmdText

          If cmd.Connection.State <> ConnectionState.Open Then conn.Open()
          Using reader As SqlDataReader = cmd.ExecuteReader
            While reader.Read()
              'only overwrite if something was found
              Dim tmp As String = CF.NullSafeString(reader("STFID"), "")
              If Not String.IsNullOrWhiteSpace(tmp) Then
                cntyFid = tmp
                If tmp.Length = 4 Then
                  cntyFid = tmp.Substring(1, 3)
                ElseIf tmp.Length = 5 Then
                  cntyFid = tmp.Substring(2, 3)
                End If
              End If
              tmp = CF.NullSafeString(reader("CountyShortNAME"), "")
              If Not String.IsNullOrWhiteSpace(tmp) Then cntyName = tmp
            End While
          End Using
        End Using
      End Using
    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.Message)
    End Try
  End Sub

  ''' <summary>
  ''' Get state names.
  ''' </summary> 
  Public Shared Function GetStates(ByRef callInfo As String) As List(Of String)
    Dim retVal As New List(Of String)
    Dim localInfo As String = ""
    Try
      Dim dataCmd As String = "SELECT STATENAME FROM usaSTATE10WM WHERE (STATENAME <> '') ORDER BY STATENAME"
      Dim states As DataTable = CF.GetDataTable(mapDataConnStr, dataCmd, localInfo)

      For Each state As DataRow In states.Rows
        retVal.Add(CF.NullSafeString(state.Item("STATENAME")))
      Next

    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Get county names for a state.
  ''' </summary>
  ''' <returns>List of strings formatted as: 5-digit fips + ' ' + county name</returns>
  Public Shared Function GetCounties(ByVal state As String, ByRef callInfo As String) As List(Of String)
    Dim retVal As New List(Of String)
    Dim localInfo As String = ""
    Try
      Dim dataCmd As String = "SELECT (Cast(STFID AS VARCHAR(10)) + ' ' + CountyNAME) AS SFNAME FROM usaCOUNTY10WM WHERE (STATENAME = @Param1) ORDER BY STATENAME"
      Dim parameters As New List(Of SqlParameter)
      Dim param As New SqlParameter("@Param1", SqlDbType.VarChar)
      param.Value = state
      parameters.Add(param)

      Dim counties As DataTable = CF.GetDataTable(mapDataConnStr, dataCmd, parameters.ToArray, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo
      For Each county As DataRow In counties.Rows
        retVal.Add(CF.NullSafeString(county.Item("SFNAME")))
      Next

    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Removes state fips from full fips.
  ''' </summary>
  Public Shared Function RemoveRegionCodeFromSubRegionCode(ByVal fipsCode As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Try
      If Not String.IsNullOrWhiteSpace(fipsCode) Then
        fipsCode = fipsCode.Trim
        If fipsCode.Length < 4 Then retVal = fipsCode Else retVal = fipsCode.Substring(2)
      End If
    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1} ", EH.GetCallerMethod(GetCurrentMethod.DeclaringType.Name), ex.Message)
    End Try
    Return retVal
  End Function

End Class
