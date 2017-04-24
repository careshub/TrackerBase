Option Strict On

#Region "Imports"

Imports Microsoft.VisualBasic
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient
Imports System.Data.SqlTypes
Imports System.Diagnostics
Imports System.IO
Imports System.Net
Imports System.Net.Mail
Imports System.Reflection
Imports System.Reflection.MethodBase
Imports System.Text
Imports System.Transactions
Imports System.Web
Imports System.Web.HttpContext
Imports System.Xml

Imports CV = CommonVariables
Imports MDL = TerLoc.Model

#End Region

Public Class TransactionUtils
  ''' <summary>
  ''' Creates a TransactionScope without defaults that could cause problems.
  ''' </summary>
  ''' <remarks>https://blogs.msdn.microsoft.com/dbrowne/2010/06/03/using-new-transactionscope-considered-harmful/</remarks>
  Public Shared Function CreateTransactionScope() As TransactionScope
    Dim transactionOptions = New TransactionOptions()
    transactionOptions.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted
    transactionOptions.Timeout = TransactionManager.MaximumTimeout
    Return New TransactionScope(TransactionScopeOption.Required, transactionOptions)
  End Function
End Class

Public Class Utf8StringWriter
  Inherits StringWriter
  Public Overrides ReadOnly Property Encoding() As Encoding
    Get
      Return Encoding.UTF8
    End Get
  End Property
End Class

''' <summary>
''' Store "global" functions. NOTE: EVERY function shall have xml comments with at least a descriptive summary
''' </summary> 
''' <remarks></remarks>
Public Class CommonFunctions
  Private Shared dataSchema As String = GetDataSchemaName()
  Private Shared dataConn As String = SiteSpecific.BaseDatabaseConnString
  Private Shared aspDataConnStr As String = GetNetDatabaseConnString()
  Private Shared aspNetDb As String = GetAspNetDatabaseName()
  Private Shared aspNetConn As String = GetNetDatabaseConnString()

#Region "Utilities"

  Public Shared Function XDocToStringWithDeclaration(doc As XDocument) As String
    If doc Is Nothing Then
      Throw New ArgumentNullException("doc")
    End If
    Dim builder As New StringBuilder()
    Using writer As TextWriter = New Utf8StringWriter()
      Using XmlWriter As XmlWriter = XmlWriter.Create(writer)
        doc.Save(writer)
      End Using
      Return writer.ToString
    End Using
    Return builder.ToString()
  End Function

  ''' <summary>
  ''' Convert sql date to javascript milliseconds.
  ''' </summary>
  Public Shared Function DateAsMillis(Value As Date) As Long
    Dim EPOCH As Date = New Date(1970, 1, 1, 0, 0, 0, 0)
    Dim span As TimeSpan = Value.Subtract(EPOCH)
    Return CLng(span.TotalMilliseconds)
  End Function

  ''' <summary>
  ''' Convert javascript ticks to .NET time with utc offset.
  ''' </summary>
  Public Shared Function JSTicksToDotNetWithUtc(ByVal jsTicks As Long) As DateTime
    Dim retVal As DateTime = Nothing
    Try : retVal = DateAdd("s", jsTicks / 1000, "1/1/1970").Add(Date.Now - Date.UtcNow)
    Catch : End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Add utc offset to a date.
  ''' </summary>
  Public Shared Function UtcOffset(ByVal inDate As DateTime) As DateTime
    Dim retVal As DateTime = Nothing
    Try : retVal = inDate.Add(Date.UtcNow - Date.Now)
    Catch : End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Trim a string to a maximum length.
  ''' </summary>
  ''' <returns>Original or trimmed string</returns>
  Public Shared Function TrimString(ByVal text As String, ByVal maxLen As Integer) As String
    Dim retVal = text
    Try : If Not String.IsNullOrEmpty(text) Then retVal = If(text.Length > maxLen, text.Substring(0, maxLen), text)
    Catch : End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Deserialize a json string into the given type.
  ''' </summary>
  Public Shared Function DeserializeJson(Of T)(ByVal objectData As String) As T
    Dim retVal As T = Nothing
    Try
      Dim serializr As New System.Web.Script.Serialization.JavaScriptSerializer()
      retVal = CType(serializr.Deserialize(objectData, GetType(T)), T)
    Catch ex As Exception
      Throw
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Serialize a datatable.
  ''' </summary> 
  Public Shared Function SerializeDataTable(ByVal dt As DataTable, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Dim serializr As New System.Web.Script.Serialization.JavaScriptSerializer()
    Dim rows As New List(Of Dictionary(Of String, Object))()
    Dim row As Dictionary(Of String, Object) = Nothing
    Try
      For Each dr As DataRow In dt.Rows
        row = New Dictionary(Of String, Object)()
        For Each dc As DataColumn In dt.Columns
          row.Add(dc.ColumnName.Trim(), dr(dc))
        Next
        rows.Add(row)
      Next
      retVal = serializr.Serialize(rows)
    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Finds a Control recursively. Note finds the first match and exists.
  ''' </summary>
  ''' <returns>matching control</returns>
  Public Shared Function FindControlRecursive(ByVal Root As Control, ByVal Id As String) As Control
    If Root.ID = Id Then Return Root
    Dim FoundCtl As Control
    For Each Ctl As Control In Root.Controls
      FoundCtl = FindControlRecursive(Ctl, Id)
      If FoundCtl IsNot Nothing Then Return FoundCtl
    Next

    Return Nothing
  End Function

  ''' <summary>
  ''' Sets a property of an object.
  ''' </summary>
  ''' <param name="obj">Object whose property will be set</param>
  ''' <param name="objType">Type of the object whose property will be set</param>
  ''' <param name="propName">Case-sensitive name of the property to be set</param>
  ''' <param name="propVal">Value for the object's property</param>
  Public Shared Function SetObjectValue(ByRef obj As Object, ByVal objType As Type, ByVal propName As String, _
                                        ByVal propVal As Object, ByRef callInfo As String) As Boolean
    Dim retVal As Boolean = True
    callInfo = ""
    Dim localInfo As String = ""
    Try
      If String.IsNullOrWhiteSpace(NullSafeString(propName, "")) Then Throw New ArgumentException("Property name is blank.")
      localInfo = " (name: " & propName.ToString & ") "
      If propVal Is Nothing OrElse IsDBNull(propVal) Then Throw New ArgumentException("Property value is nothing.")
      localInfo = String.Format(" (name: {0}, val: {1}) ", propName.ToString, propVal.ToString)
      Dim propertyInfo As PropertyInfo = objType.GetProperty(propName)
      If propertyInfo Is Nothing Then Throw New ArgumentException("Property info is not found.")
      propertyInfo.SetValue(obj, Convert.ChangeType(propVal, propertyInfo.PropertyType), Nothing)
    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier, ex)
      retVal = False
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Create a default "from" address for emailing based on the current site.
  ''' </summary>
  Public Shared Function GetSiteEmail() As String
    Dim retVal As String = "Terrace"
    Try
      retVal = CV.siteEmailBase & GetSiteName()
    Catch ex As Exception
      Throw
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Validate an email address.
  ''' </summary>
  Public Shared Function IsValidEmail(ByVal email As String) As Boolean
    Dim retVal As Boolean = True
    Try : Dim mailAddr As New MailAddress(email)
    Catch : retVal = False : End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Email function with no defaults. Optional cc, bcc, attachments.
  ''' </summary>
  ''' <param name="toAddrs">Comma separated string of addresses</param>
  ''' <param name="fromAddr">Single address</param>
  ''' <param name="subject"></param>
  ''' <param name="body"></param>
  ''' <param name="callInfo">In/Out information passing</param>
  ''' <param name="cc">Comma separated string of addresses</param>
  ''' <param name="bcc">Comma separated string of addresses</param>
  ''' <param name="attachments">List of attachment strings</param>
  Public Shared Sub SendEmail(toAddrs As String, fromAddr As String, subject As String, body As String, ByRef callInfo As String, _
        Optional cc As String = "", Optional bcc As String = "", Optional attachments As List(Of String) = Nothing, Optional isHtml As Boolean = True)
    Try
      Dim message As New MailMessage()
      message.From = New MailAddress(fromAddr)
      message.Subject = subject.Replace(Environment.NewLine, "  ")
      message.IsBodyHtml = isHtml
      message.Body = body
      message.To.Add(toAddrs)
      If Not String.IsNullOrWhiteSpace(cc) Then
        message.CC.Add(cc)
      End If
      If Not String.IsNullOrWhiteSpace(bcc) Then
        message.Bcc.Add(bcc)
      End If
      If attachments IsNot Nothing Then
        For Each attach As String In attachments
          message.Attachments.Add(New Attachment(attach))
        Next
      End If
      Dim client As New SmtpClient()
      client.Host = "massmail.missouri.edu"
      client.Port = 587
      client.EnableSsl = True
      client.Send(message)
    Catch ex As Exception
      callInfo = ShowError(MethodIdentifier(), ex)
    End Try
  End Sub

  ''' <summary>
  ''' Kevin's debug email function.
  ''' </summary>
  Public Shared Sub SendOzzy(subject As String, body As String, ByRef callInfo As String)
    Try
      Dim message As New MailMessage()
      message.From = New MailAddress(CV.ozzyEmail)
      message.Subject = subject.Replace(Environment.NewLine, "  ")
      message.IsBodyHtml = True
      message.Body = body
      message.To.Add(CV.ozzyEmail)
      Dim client As New SmtpClient()
      client.Host = "massmail.missouri.edu"
      client.Port = 587
      client.EnableSsl = True
      client.Send(message)
    Catch ex As Exception
      callInfo = ShowError(MethodIdentifier(), ex)
    End Try
  End Sub

  ''' <summary>
  ''' Returns new GUID.
  ''' </summary>
  Public Shared Function GenerateGUID() As Guid
    Return System.Guid.NewGuid
  End Function

#Region "Null safe conversions"

  ''' <summary>Converts an object to a bit integer if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <param name="returnIfEmpty">Default value if <paramref name="arg"/> is <c>DBNull</c>, <c>Nothing</c>, or <c>String.Empty</c>.</param>
  ''' <returns>Converted <paramref name="arg"/> or <paramref name="returnIfEmpty"/>.</returns>
  ''' <remarks>From http://www.codeproject.com/Articles/8748/NullSafe-Functions-Ensuring-Safe-Variables</remarks>
  Public Shared Function NullSafeBit(ByVal arg As Object, Optional ByVal returnIfEmpty As Integer = 0) As Integer
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then Return CInt(arg)
    Catch : End Try
    Return returnIfEmpty
  End Function

  ''' <summary>Converts an object to a string if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <param name="returnIfEmpty">Default value if <paramref name="arg"/> is <c>DBNull</c>, <c>Nothing</c>, or <c>String.Empty</c>.</param>
  ''' <param name="doTrim">Flag for trimming the return value.</param>
  ''' <returns>Converted <paramref name="arg"/> or <paramref name="returnIfEmpty"/>.</returns>
  ''' <remarks>From http://www.codeproject.com/Articles/8748/NullSafe-Functions-Ensuring-Safe-Variables</remarks>
  Public Shared Function NullSafeString(ByVal arg As Object, Optional ByVal returnIfEmpty As String = "", Optional doTrim As Boolean = True) As String
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then Return If(doTrim, CStr(arg).Trim, CStr(arg)).ToString
    Catch : End Try
    Return returnIfEmpty
  End Function

  ''' <summary>Converts an object to a long if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <param name="returnIfEmpty">Default value if <paramref name="arg"/> is <c>DBNull</c>, <c>Nothing</c>, or <c>String.Empty</c>.</param>
  ''' <returns>Converted <paramref name="arg"/> or <paramref name="returnIfEmpty"/>.</returns>
  ''' <remarks>From http://www.codeproject.com/Articles/8748/NullSafe-Functions-Ensuring-Safe-Variables</remarks>
  Public Shared Function NullSafeLong(ByVal arg As Object, Optional ByVal returnIfEmpty As Long = Long.MinValue) As Long
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then Return CLng(arg)
    Catch : End Try
    Return returnIfEmpty
  End Function

  ''' <summary>Converts an object to a integer if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <param name="returnIfEmpty">Default value if <paramref name="arg"/> is <c>DBNull</c>, <c>Nothing</c>, or <c>String.Empty</c>.</param>
  ''' <returns>Converted <paramref name="arg"/> or <paramref name="returnIfEmpty"/>.</returns>
  ''' <remarks>From http://www.codeproject.com/Articles/8748/NullSafe-Functions-Ensuring-Safe-Variables</remarks>
  Public Shared Function NullSafeInteger(ByVal arg As Object, Optional ByVal returnIfEmpty As Integer = Integer.MinValue) As Integer
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then Return CInt(arg)
    Catch : End Try
    Return returnIfEmpty
  End Function

  ''' <summary>Converts an object to a short if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <param name="returnIfEmpty">Default value if <paramref name="arg"/> is <c>DBNull</c>, <c>Nothing</c>, or <c>String.Empty</c>.</param>
  ''' <returns>Converted <paramref name="arg"/> or <paramref name="returnIfEmpty"/>.</returns>
  ''' <remarks>From http://www.codeproject.com/Articles/8748/NullSafe-Functions-Ensuring-Safe-Variables</remarks>
  Public Shared Function NullSafeShort(ByVal arg As Object, Optional ByVal returnIfEmpty As Short = Short.MinValue) As Short
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then Return CShort(arg)
    Catch : End Try
    Return returnIfEmpty
  End Function

  ''' <summary>Converts an object to a double if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <param name="returnIfEmpty">Default value if <paramref name="arg"/> is <c>DBNull</c>, <c>Nothing</c>, or <c>String.Empty</c>.</param>
  ''' <returns>Converted <paramref name="arg"/> or <paramref name="returnIfEmpty"/>.</returns>
  ''' <remarks>From http://www.codeproject.com/Articles/8748/NullSafe-Functions-Ensuring-Safe-Variables</remarks>
  Public Shared Function NullSafeDouble(ByVal arg As Object, Optional ByVal returnIfEmpty As Double = Double.MinValue) As Double
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then Return CDbl(arg)
    Catch : End Try
    Return returnIfEmpty
  End Function

  ''' <summary>Converts an object to a decimal if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <param name="returnIfEmpty">Default value if <paramref name="arg"/> is <c>DBNull</c>, <c>Nothing</c>, or <c>String.Empty</c>.</param>
  ''' <returns>Converted <paramref name="arg"/> or <paramref name="returnIfEmpty"/>.</returns>
  ''' <remarks>From http://www.codeproject.com/Articles/8748/NullSafe-Functions-Ensuring-Safe-Variables</remarks>
  Public Shared Function NullSafeDecimal(ByVal arg As Object, Optional ByVal returnIfEmpty As Decimal = Decimal.MinValue) As Decimal
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then Return CDec(arg)
    Catch : End Try
    Return returnIfEmpty
  End Function

  ''' <summary>Converts an object to a single if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <param name="returnIfEmpty">Default value if <paramref name="arg"/> is <c>DBNull</c>, <c>Nothing</c>, or <c>String.Empty</c>.</param>
  ''' <returns>Converted <paramref name="arg"/> or <paramref name="returnIfEmpty"/>.</returns>
  ''' <remarks>From http://www.codeproject.com/Articles/8748/NullSafe-Functions-Ensuring-Safe-Variables</remarks>
  Public Shared Function NullSafeSingle(ByVal arg As Object, Optional ByVal returnIfEmpty As Single = Single.MinValue) As Single
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then Return CSng(arg)
    Catch : End Try
    Return returnIfEmpty
  End Function

  ''' <summary>Converts an object to a boolean if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns><c>True</c> or <c>False</c>.</returns>
  ''' <remarks>From http://www.codeproject.com/Articles/8748/NullSafe-Functions-Ensuring-Safe-Variables</remarks>
  Public Shared Function NullSafeBoolean(ByVal arg As Object) As Boolean
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then Return CBool(arg)
    Catch : End Try
    Return False
  End Function

  ''' <summary>Converts an object to a datetime if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or DateTime.MinValue.</returns> 
  Public Shared Function NullSafeDateTime(ByVal arg As Object) As DateTime
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then
        Dim tmpVal As DateTime
        If DateTime.TryParse(arg.ToString, tmpVal) Then Return tmpVal
      End If
    Catch : End Try
    Return DateTime.MinValue
  End Function

  ''' <summary>Converts an object to a Guid if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or Guid.Empty.</returns> 
  Public Shared Function NullSafeGuid(ByVal arg As Object) As Guid
    Try : If Not ((arg Is DBNull.Value) OrElse (arg Is Nothing) OrElse (arg Is String.Empty)) Then
        Dim tmpVal As Guid
        If Guid.TryParse(arg.ToString, tmpVal) Then Return tmpVal
      End If
    Catch : End Try
    Return Guid.Empty
  End Function

#End Region

#End Region

#Region "Site specific"

  ''' <summary>
  ''' Testing. TODO: delete?
  ''' </summary> 
  Public Shared Function GetSitePrefixTest() As String
    Dim Request As HttpRequest = HttpContext.Current.Request
    Dim RequestUrl As Uri = Request.Url
    'Dim port As String = if(RequestUrl.IsDefaultPort, String.Empty, String.Format(":{0}", Request.Url.Port)).ToString
    'Return String.Format("{0}://{1}{2}{3}/orderdownloads", RequestUrl.Scheme, RequestUrl.Host, port, Request.ApplicationPath)

    Dim uc As New System.Web.UI.Page()
    Return "dev"
  End Function

  ''' <summary>
  ''' Returns subdomain for site based on url host name.
  ''' </summary> 
  Public Shared Function GetSiteSubdomain() As String
    Return SiteSpecific.SiteSubdomain
  End Function

  ''' <summary>
  ''' Returns type (dev, demo, or www) for site based on url host name.
  ''' </summary> 
  Public Shared Function GetSiteType() As String
    Return SiteSpecific.SiteType
  End Function

  ''' <summary>
  ''' Returns domain of site based on url host name.
  ''' </summary> 
  Public Shared Function GetSiteName() As String
    Return SiteSpecific.SiteName
  End Function

  ''' <summary>
  ''' Returns name of schema for database holding data.
  ''' </summary> 
  Public Shared Function GetDataSchemaName() As String
    Return SiteSpecific.ProjectSchemaName
  End Function

  ''' <summary>
  ''' Returns name of database holding data.
  ''' </summary> 
  Public Shared Function GetDataDatabaseName() As String
    Return SiteSpecific.BaseDatabaseName
  End Function

  ''' <summary>
  ''' Returns name of database holding asp.net data.
  ''' </summary> 
  Public Shared Function GetAspNetDatabaseName() As String
    Return SiteSpecific.AspNetDatabaseName
  End Function

  ''' <summary>
  ''' Returns connection string to database holding data.
  ''' </summary> 
  Public Shared Function GetBaseDatabaseConnString() As String
    Return SiteSpecific.BaseDatabaseConnString
  End Function

  ''' <summary>
  ''' Returns connection string to database holding asp.net data.
  ''' </summary> 
  Public Shared Function GetNetDatabaseConnString() As String
    Return SiteSpecific.AspNetDatabaseConnString
  End Function

  ''' <summary>
  ''' Returns name of folder that contains site project folders.
  ''' </summary> 
  Public Shared Function GetProjectFolderBase() As String
    Return SiteSpecific.ProjectFolderBase
  End Function

  ''' <summary>
  ''' Return a formatted title for the page.
  ''' </summary>
  ''' <param name="pageName">Name of calling page to be included in the title</param>
  Public Shared Function GetPageTitle(Optional ByVal pageName As String = "") As String
    Dim retVal As String = "Terrace"
    Try
      Dim prefx As String = GetSiteType()
      Select Case prefx
        Case "demo"
          retVal = "TerraceDemo"
        Case "dev"
          retVal = "TerraceDev"
      End Select
      If Not String.IsNullOrWhiteSpace(pageName) Then retVal &= " " & pageName
    Catch ex As Exception
      ShowError(MethodIdentifier(), ex)
    End Try
    Return retVal
  End Function

#End Region

#Region "SQL"

  ''' <summary>
  ''' INCOMPLETE -- do not use.
  ''' </summary> 
  Public Shared Function InsertParameter(ByVal name As String, ByVal sqlType As SqlDbType, ByVal value As Object, ByVal valType As System.Type, ByRef callInfo As String) As Object
    Dim retVal As Object = Nothing
    Try

    Catch ex As Exception
      callInfo &= String.Format(" {0} error: {1}  ", MethodIdentifier(), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Retrieves a data table based on a connection string and command text with parameters.
  ''' </summary>
  ''' <param name="dataConn">SQL data connection string</param>
  ''' <param name="dataCmd">SQL select command text</param>
  Public Shared Function GetDataTable(ByVal dataConn As String, ByVal dataCmd As String, _
                                      ByVal parm As SqlParameter, ByRef callInfo As String) As DataTable
    Dim retVal As New DataTable
    Try
      Using da As New SqlDataAdapter(dataCmd, dataConn)
        da.SelectCommand.Parameters.Add(parm)
        da.Fill(retVal)
      End Using
    Catch ex As Exception
      callInfo &= String.Format(" {0} error: {1}  ", MethodIdentifier(), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Retrieves a data table based on a connection string and command text with parameters.
  ''' </summary>
  ''' <param name="dataConn">SQL data connection string</param>
  ''' <param name="dataCmd">SQL select command text</param> 
  Public Shared Function GetDataTable(ByVal dataConn As String, ByVal dataCmd As String, _
                                      ByVal parms() As SqlParameter, ByRef callInfo As String) As DataTable
    Dim retVal As New DataTable
    Try
      Using da As New SqlDataAdapter(dataCmd, dataConn)
        da.SelectCommand.Parameters.AddRange(parms)
        da.Fill(retVal)
      End Using
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", MethodIdentifier(), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Retrieves a data table based on a connection string and command text.
  ''' </summary>
  ''' <param name="dataConn">SQL data connection string</param>
  ''' <param name="dataCmd">SQL select command text</param> 
  Public Shared Function GetDataTable(ByVal dataConn As String, ByVal dataCmd As String, ByRef callInfo As String) As DataTable
    Dim retVal As DataTable = New DataTable
    Try
      Using da As New SqlDataAdapter(dataCmd, dataConn)
        da.Fill(retVal)
      End Using
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", MethodIdentifier(), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Retrieves a data table based on a connection and command text.
  ''' </summary>
  ''' <param name="dataConn">SQL data connection</param>
  ''' <param name="dataCmd">SQL select command text</param>
  Public Shared Function GetDataTable(ByVal dataConn As SqlConnection, ByVal dataCmd As String, ByRef callInfo As String) As DataTable
    Dim retVal As DataTable = New DataTable
    Try
      Using da As New SqlDataAdapter(dataCmd, dataConn)
        da.Fill(retVal)
      End Using
    Catch ex As Exception
      callInfo &= String.Format("  {0} error: {1}  ", MethodIdentifier(), ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Executes nonquery command.
  ''' </summary>
  ''' <returns>ExecuteNonQuery integer (number of rows affected)</returns>
  Public Shared Function ExecuteSqlNonQuery(ByVal conn As SqlConnection, ByVal sqlStr As String, _
                                            ByRef callInfo As String) As Integer
    Dim rowCount As Integer = -1
    Try
      Using cmd As SqlCommand = conn.CreateCommand()
        cmd.CommandText = sqlStr
        If conn.State = ConnectionState.Closed Then conn.Open()
        rowCount = cmd.ExecuteNonQuery()
      End Using
    Catch ex As Exception
      callInfo = MethodIdentifier() & " error: " & ex.Message
    End Try
    Return rowCount
  End Function

  ''' <summary>
  ''' Executes nonquery command with parameters.
  ''' </summary>
  ''' <returns>ExecuteNonQuery integer (number of rows affected)</returns>
  Public Shared Function ExecuteSqlNonQuery(ByVal conn As SqlConnection, ByVal sqlStr As String, _
                                            ByVal parms() As SqlParameter, ByRef callInfo As String) As Integer
    Dim rowCount As Integer = -1
    Try
      Using cmd As SqlCommand = conn.CreateCommand()
        cmd.CommandText = sqlStr
        cmd.Parameters.AddRange(parms)
        If conn.State = ConnectionState.Closed Then conn.Open()
        rowCount = cmd.ExecuteNonQuery()
      End Using
    Catch ex As Exception
      callInfo = MethodIdentifier() & " error: " & ex.Message
    End Try
    Return rowCount
  End Function

  ''' <summary>
  ''' Executes scalar command.
  ''' </summary>
  ''' <returns>ExecuteScalar object (first column of first row of result set)</returns>
  Public Shared Function ExecuteSqlScalar(ByVal conn As SqlConnection, ByVal sqlStr As String, _
                                          ByRef callInfo As String) As Object
    Dim retVal As Object = Nothing
    Try
      Using cmd As SqlCommand = conn.CreateCommand()
        cmd.CommandText = sqlStr
        If conn.State = ConnectionState.Closed Then conn.Open()
        retVal = cmd.ExecuteScalar()
      End Using
    Catch ex As Exception
      callInfo = MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Executes scalar command with parameters.
  ''' </summary>
  ''' <returns>ExecuteScalar object (first column of first row of result set)</returns>
  Public Shared Function ExecuteSqlScalar(ByVal conn As SqlConnection, ByVal sqlStr As String, _
                                            ByVal parms() As SqlParameter, ByRef callInfo As String) As Object
    Dim retVal As Object = Nothing
    Try
      Using cmd As SqlCommand = conn.CreateCommand()
        cmd.CommandText = sqlStr
        cmd.Parameters.AddRange(parms)
        If conn.State = ConnectionState.Closed Then conn.Open()
        retVal = cmd.ExecuteScalar()
      End Using
    Catch ex As Exception
      callInfo = MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Returns an updated table with names set based on ids in the table
  ''' </summary> 
  Public Shared Function UpdateNames(ByVal tbl As DataTable, ByRef callInfo As String) As DataTable
    Dim retVal As DataTable = Nothing

    retVal = tbl.Copy
    With retVal.Columns
      .Add(New DataColumn("Creator", GetType(String)))
      .Add(New DataColumn("Editor", GetType(String)))
    End With

    Return retVal 'Don't update until more than one user can be on a project, but need columns for other functions

    'Dim cmdText As String = ""
    'Dim localInfo As String = ""
    'Try

    '  localInfo = ""
    '  Dim usrs As MDL.UserList = MDL.UserHelper.Fetch(localInfo)
    '  If localInfo.ToLower.Contains("error") Then callInfo &= Space(3) & localInfo & Space(3)

    '  Dim selUsrs As IEnumerable(Of MDL.User)

    '  For Each dr As DataRow In retVal.Rows
    '    With dr
    '      selUsrs = usrs.Users.Where(Function(usr) usr.UserId.ToString = NullSafeString(.Item("CreatorGuid"), ""))
    '      If selUsrs.Count > 0 Then
    '        .Item("Creator") = String.Join(" ", selUsrs(0).FirstName, selUsrs(0).LastName)
    '      End If

    '      selUsrs = usrs.Users.Where(Function(usr) usr.UserId.ToString = NullSafeString(.Item("EditorGuid"), ""))
    '      If selUsrs.Count > 0 Then
    '        .Item("Creator") = String.Join(" ", selUsrs(0).FirstName, selUsrs(0).LastName)
    '      End If

    '    End With
    '  Next

    'Catch ex As Exception
    '  callInfo &= String.Format("  {0} error: {1}  ", MethodIdentifier(), ex.ToString)
    'End Try
    'Return retVal
  End Function

#Region "Null safe conversions to sql"

  ''' <summary>Converts an object to a SqlString if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or SqlString.Null.</returns>
  Public Shared Function NullSafeSqlString(ByVal arg As String) As SqlString
    Try : If Not String.IsNullOrWhiteSpace(arg) Then Return New SqlString(arg)
    Catch : End Try
    Return SqlString.Null
  End Function

  ''' <summary>Converts an object to a SqlInt64 if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or SqlInt64.Null.</returns>
  Public Shared Function NullSafeSqlLong(ByVal arg As Long, Optional ByVal nullValue As Long = Long.MinValue) As SqlInt64
    Try : If Not (arg = nullValue) Then Return New SqlInt64(arg)
    Catch : End Try
    Return SqlInt64.Null
  End Function

  ''' <summary>Converts an object to a SqlInt32 if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or SqlInt32.Null.</returns>
  Public Shared Function NullSafeSqlInteger(ByVal arg As Integer, Optional ByVal nullValue As Integer = Integer.MinValue) As SqlInt32
    Try : If Not (arg = nullValue) Then Return New SqlInt32(arg)
    Catch : End Try
    Return SqlInt32.Null
  End Function

  ''' <summary>Converts an object to a SqlInt16 if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or SqlInt16.Null.</returns>
  Public Shared Function NullSafeSqlShort(ByVal arg As Short, Optional ByVal nullValue As Short = Short.MinValue) As SqlInt16
    Try : If Not (arg = nullValue) Then Return New SqlInt16(arg)
    Catch : End Try
    Return SqlInt16.Null
  End Function

  ''' <summary>Converts an object to a SqlDouble if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or SqlDouble.Null.</returns>
  Public Shared Function NullSafeSqlDouble(ByVal arg As Double, Optional ByVal nullValue As Double = Double.MinValue) As SqlDouble
    Try : If Not (arg = nullValue) Then Return New SqlDouble(arg)
    Catch : End Try
    Return SqlDouble.Null
  End Function

  ''' <summary>Converts an object to a SqlDecimal if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or SqlDecimal.Null.</returns>
  Public Shared Function NullSafeSqlDecimal(ByVal arg As Decimal, Optional ByVal nullValue As Decimal = Decimal.MinValue) As SqlDecimal
    Try : If Not (arg = nullValue) Then Return New SqlDecimal(arg)
    Catch : End Try
    Return SqlDecimal.Null
  End Function

  ''' <summary>Converts an object to a SqlSingle if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or SqlSingle.Null.</returns>
  Public Shared Function NullSafeSqlSingle(ByVal arg As Single, Optional ByVal nullValue As Single = Single.MinValue) As SqlSingle
    Try : If Not (arg = nullValue) Then Return New SqlSingle(arg)
    Catch : End Try
    Return SqlSingle.Null
  End Function

  ''' <summary>Converts an object to a SqlBoolean if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <param name="requiresValue">If false, returns SqlBoolean.Null as default. If true, returns SqlBoolean.False as default.</param>
  ''' <returns>Converted <paramref name="arg"/> or nullValue.</returns>
  Public Shared Function NullSafeSqlBoolean(ByVal arg As Boolean, Optional ByVal requiresValue As Boolean = False) As SqlBoolean
    Try : Return New SqlBoolean(arg)
    Catch : End Try
    If requiresValue Then
      Return SqlBoolean.False
    Else
      Return SqlBoolean.Null
    End If
  End Function

  ''' <summary>Converts an object to a SqlDateTime if possible.</summary>
  ''' <param name="arg">Object to be converted.</param>
  ''' <returns>Converted <paramref name="arg"/> or SqlDateTime.Null.</returns>
  Public Shared Function NullSafeSqlDateTime(ByVal arg As DateTime, Optional ByVal nullValue As System.Nullable(Of DateTime) = Nothing) As SqlDateTime
    Try : If Not (arg = nullValue) Then Return New SqlDateTime(arg)
    Catch : End Try
    Return SqlDateTime.Null
  End Function

#End Region

#End Region

#Region "ArcGIS Requests"

  Public Shared Function GetStringFromUrl(url As String) As String
    Try
      Dim strResult As String = Nothing
      Dim objResponse As Net.WebResponse = Nothing
      Dim objRequest As Net.WebRequest = Net.HttpWebRequest.Create(url)
      objResponse = objRequest.GetResponse()
      Using sr As New StreamReader(objResponse.GetResponseStream())
        strResult = sr.ReadToEnd()
        sr.Close()
      End Using
      Return strResult
    Catch ex As Exception
      SendEmail(CV.ozzyEmail, GetSiteEmail, "Get String From URL", "Error for URL: " & url, Nothing)
      Return ""
    End Try
  End Function

#End Region

  ''' <summary>
  ''' Add a system message to a project.
  ''' </summary>
  Public Shared Sub InsertProjectSystemMessage(ByVal projectId As Long, ByVal msgDesc As String, ByRef callInfo As String)
    Try
      Dim cmdText As String = String.Format(<a>
            INSERT INTO {0}.[ProjectSystemMessages]
            ([ProjectId]
            ,[MessageId]
            )
            SELECT
            @objId
            ,(SELECT ObjectId FROM {0}.[SystemMessages] WHERE Description = @desc)
            </a>.Value, dataSchema)
      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand

          cmd.CommandText = cmdText
          cmd.Parameters.Add("@objId", SqlDbType.BigInt).Value = projectId
          cmd.Parameters.Add("@desc", SqlDbType.NChar, 10).Value = msgDesc

          If conn.State = ConnectionState.Closed Then conn.Open()
          cmd.ExecuteNonQuery()
        End Using
      End Using
    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier, ex)
    End Try
  End Sub

  ''' <summary>
  ''' Delete a project's system message records.
  ''' </summary>
  Public Shared Sub DeleteProjectSystemMessage(ByVal projectId As Long, ByVal msgDesc As String, ByRef callInfo As String)
    Try
      Dim cmdText As String = String.Format(<a>
            DELETE FROM {0}.[ProjectSystemMessages]
            FROM {0}.[ProjectSystemMessages] AS PSM
            INNER JOIN {0}.[SystemMessages] AS SM ON PSM.MessageId = SM.ObjectId
            WHERE [ProjectId]= @objId AND [Description] = @desc</a>.Value, dataSchema)
      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand

          cmd.CommandText = cmdText
          cmd.Parameters.Add("@objId", SqlDbType.BigInt).Value = projectId
          cmd.Parameters.Add("@desc", SqlDbType.NChar, 10).Value = msgDesc

          If conn.State = ConnectionState.Closed Then conn.Open()
          cmd.ExecuteNonQuery()
        End Using
      End Using
    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier, ex)
    End Try
  End Sub

  ''' <summary>
  ''' Gets table of system messages for a project.
  ''' </summary>
  Public Shared Function GetProjectMessages(ByVal projectId As Long, ByRef callInfo As String) As DataTable
    Dim retVal As DataTable = Nothing
    Dim localInfo As String = ""
    Try
      Dim cmdText As String = String.Format(<a>
            SELECT *
            FROM {0}.[SystemMessages] as SM
            INNER JOIN {0}.[ProjectSystemMessages] as PSM ON SM.ObjectId = PSM.MessageId
            WHERE [ProjectId]= @projectId</a>.Value, dataSchema)

      Dim parm As New SqlParameter("@projectId", SqlDbType.BigInt)
      parm.Value = projectId

      retVal = GetDataTable(dataConn, cmdText, parm, localInfo)
      If localInfo.Contains("error") Then callInfo &= Environment.NewLine & localInfo

    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier, ex)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Change project ownership.
  ''' </summary>
  Public Shared Sub UpdatePermissionRolesCreator(ByVal projectId As Long, _
                                             ByVal newOwnerGuid As Guid, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try
      Dim cmdText As String = String.Format(<a>
            UPDATE {0}.[PermissionRoles] SET [CreatorGuid] = @newOwnerId WHERE [ProjectOid] = @projectId
                                          </a>.Value, dataSchema)

      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = cmdText

          cmd.Parameters.Add("@newOwnerId", SqlDbType.UniqueIdentifier).Value = newOwnerGuid
          cmd.Parameters.Add("@projectId", SqlDbType.BigInt).Value = projectId

          If conn.State = ConnectionState.Closed Then conn.Open()
          cmd.ExecuteNonQuery()
        End Using
      End Using
    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier, ex)
    End Try
  End Sub

  ''' <summary>
  ''' Gets owner id for a project id.
  ''' </summary>
  Public Shared Function GetPermissionRolesCreator(ByVal projectId As Long, ByRef callInfo As String) As Guid
    Dim retVal As Guid = Guid.Empty
    Try
      Using conn As New SqlConnection(GetBaseDatabaseConnString)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = String.Format("select [CreatorGuid] FROM {0}.[PermissionRoles] WHERE [ProjectOid]= @projectId", dataSchema)
          Dim prm As New SqlParameter("@projectId", projectId)
          cmd.Parameters.Add(prm)

          If conn.State = ConnectionState.Closed Then conn.Open()
          Using readr As SqlDataReader = cmd.ExecuteReader
            While readr.Read
              retVal = NullSafeGuid(readr("CreatorGuid"))
            End While
          End Using
        End Using
      End Using

    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1}", MethodIdentifier, ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Change project ownership.
  ''' </summary>
  Public Shared Sub UpdateProjectOwner(ByVal projectId As Long, _
                                           ByVal newOwnerGuid As Guid, ByRef callInfo As String)
    Try
      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = <a>
  UPDATE TerLoc.Project SET OwnerGuid = @newOwnerGuid WHERE ObjectID = @projectId
                                    </a>.Value

          cmd.Parameters.Add("@newOwnerGuid", SqlDbType.UniqueIdentifier).Value = newOwnerGuid
          cmd.Parameters.Add("@projectId", SqlDbType.BigInt).Value = projectId

          If conn.State = ConnectionState.Closed Then conn.Open()
          cmd.ExecuteNonQuery()
        End Using
      End Using
    Catch ex As Exception
      callInfo &= ShowError(MethodIdentifier, ex)
    End Try
  End Sub

  ''' <summary>
  ''' Gets owner id for a project id.
  ''' </summary>
  Public Shared Function GetProjectOwner(ByVal projectId As Long, ByRef callInfo As String) As Guid
    Dim retVal As Guid = Guid.Empty
    Try
      Using conn As New SqlConnection(GetBaseDatabaseConnString)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = String.Format("select [OwnerGuid] FROM {0}.[Project] WHERE [ObjectID]= @projectId", dataSchema)

          cmd.Parameters.Add("@projectId", SqlDbType.BigInt).Value = projectId

          If conn.State = ConnectionState.Closed Then conn.Open()
          Using readr As SqlDataReader = cmd.ExecuteReader
            While readr.Read
              retVal = NullSafeGuid(readr("OwnerGuid"))
            End While
          End Using
        End Using
      End Using

    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1}", MethodIdentifier, ex.Message)
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Formats data for db insert based on system.type.
  ''' </summary> 
  ''' <returns>Formatted object ready for insertion</returns>
  Public Shared Function FormatInsertValue(ByVal inVal As Object, ByVal atype As Object) As Object
    Dim retVal As Object = Nothing
    Try
      Select Case atype.ToString
        Case GetType(Integer).ToString, GetType(Decimal).ToString '  format numeric
          If IsNumeric(inVal) Then retVal = inVal Else retVal = System.DBNull.Value
        Case GetType(Boolean).ToString '  format true/false
          Dim thisBool As Boolean
          If Boolean.TryParse(inVal.ToString, thisBool) Then
            If thisBool = True Then retVal = 1 Else retVal = 0
          Else
            Dim trueValues() As String = CV.BooleanTrueValues
            If Array.IndexOf(trueValues, inVal.ToString) > -1 Then
              retVal = 1
            Else : retVal = 0
            End If
          End If
        Case GetType(String).ToString, GetType(Char).ToString, GetType(Date).ToString '  format string
          If inVal Is Nothing Then
            retVal = "NULL"
          ElseIf inVal.ToString.Trim.Length > 0 Then
            inVal = inVal.ToString.Replace("'", "''")
            retVal = "'" & inVal.ToString & "'"
          Else : retVal = "NULL"
          End If
        Case Else
          retVal = System.DBNull.Value
      End Select
    Catch ex As Exception
      retVal = ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Returns name for project.
  ''' </summary>
  Public Shared Function GetProjectNameByProjectId(ByVal projectId As Long, _
                                               ByRef callInfo As String) As String
    Dim retVal As String = ""
    Try
      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = "SELECT Name FROM " & dataSchema & ".Project WHERE ObjectId=@projectId "
          Dim prm As New SqlParameter("@projectId", projectId)
          cmd.Parameters.Add(prm)

          If conn.State = ConnectionState.Closed Then conn.Open()
          Dim readr As SqlDataReader = cmd.ExecuteReader
          While readr.Read
            retVal = NullSafeString(readr("Name").ToString, "Project name not found.")
          End While
        End Using
      End Using
    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Returns Folder for project.
  ''' </summary>
  Public Shared Function GetProjectFolderByProjectId(ByVal projectID As Long, _
                                               ByRef callInfo As String) As String
    Dim retVal As String = ""
    Try
      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = "SELECT Folder FROM " & dataSchema & ".Project WHERE ObjectId=@projectId "
          Dim prm As New SqlParameter("@projectId", projectID)
          cmd.Parameters.Add(prm)

          If conn.State = ConnectionState.Closed Then conn.Open()
          Dim readr As SqlDataReader = cmd.ExecuteReader
          While readr.Read
            retVal = readr(0).ToString
          End While
        End Using
      End Using

    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Updates ProjectDatum record with a given ObjectId.
  ''' </summary>
  ''' <returns>-1 or Number of rows affected</returns>
  Public Shared Function UpdateProjectDatumByDatumId(ByVal datumId As Long, ByVal usrId As Guid, _
                          ByVal notes As String, ByRef callInfo As String) As Integer
    Dim retVal As Integer = -1
    Try
      Dim cmdText As String = String.Format(<a>
          UPDATE {0}.ProjectDatum
          SET Edited = @Edited, EditorGuid = @EditorGuid, Notes = @Notes
          WHERE ObjectId = @ObjectId
                              </a>.Value, dataSchema)

      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = cmdText
          With cmd.Parameters
            .Add("@ObjectId", SqlDbType.BigInt).Value = datumId
            .Add("@Edited", SqlDbType.DateTime).Value = Now
            .Add("@EditorGuid", SqlDbType.UniqueIdentifier).Value = usrId
            .Add("@Notes", SqlDbType.NVarChar).Value = NullSafeSqlString(notes)
          End With

          If conn.State = ConnectionState.Closed Then conn.Open()
          retVal = cmd.ExecuteNonQuery()
        End Using
      End Using

    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Zips files into new target file.
  ''' </summary>
  Public Shared Function ZipDirs(ByVal dirNamesToZip As List(Of String), ByVal zipFileName As String, _
                               ByRef callInfo As String) As Boolean
    Dim localInfo As String = ""
    Dim retVal As Boolean = True
    Try
      Using zip1 As Ionic.Zip.ZipFile = New Ionic.Zip.ZipFile
        Try
          For Each fileName As String In dirNamesToZip
            zip1.AddDirectory(fileName, Path.GetFileNameWithoutExtension(fileName))
          Next
          zip1.Save(zipFileName)
        Catch invOpEx As InvalidOperationException
          If invOpEx.Message.Contains("Collection was modified; enumeration operation may not execute") Then
            'Should be due to skipping path in extraction (change of FileName property, so ignore.
            'File seems to extract anyway.
            'retVal = True
          Else
            callInfo &= MethodIdentifier() & " error: " & invOpEx.Message
          End If
        Catch ex As Exception
          callInfo &= MethodIdentifier() & " error: " & ex.Message
        End Try
      End Using

      retVal = True 'Made it here, should be good
    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Zips files into new target file.
  ''' </summary>
  Public Shared Function Zip(ByVal fileNamesToZip As List(Of String), ByVal zipFileName As String, _
                               ByRef callInfo As String) As Boolean
    Dim localInfo As String = ""
    Dim retVal As Boolean = False
    Try
      Using zip1 As Ionic.Zip.ZipFile = New Ionic.Zip.ZipFile
        Try
          For Each fileName As String In fileNamesToZip
            zip1.AddFile(fileName, "")
          Next
          zip1.Save(zipFileName)
        Catch invOpEx As InvalidOperationException
          If invOpEx.Message.Contains("Collection was modified; enumeration operation may not execute") Then
            'Should be due to skipping path in extraction (change of FileName property, so ignore.
            'File seems to extract anyway.
            'retVal = True
          Else
            callInfo &= MethodIdentifier() & " error: " & invOpEx.Message
          End If
        Catch ex As Exception
          callInfo &= MethodIdentifier() & " error: " & ex.Message
        End Try
      End Using

      Dim di As New DirectoryInfo(zipFileName)
      If di.Exists Then
        retVal = True
      Else
        retVal = False
      End If

      'retVal = True 'Made it here, should be good
    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  'TODO: ????
  ''' <summary>
  ''' Unzips file to target directory -- DO NOT USE.
  ''' </summary>
  Public Shared Function Unzip(ByVal ZipToUnpack As String, ByVal TargetDir As String, _
                               ByRef callInfo As String) As Boolean
    Dim localInfo As String = ""
    Dim retVal As Boolean = False
    Try
      Using zip1 As Ionic.Zip.ZipFile = Ionic.Zip.ZipFile.Read(Path.Combine(TargetDir, ZipToUnpack))
        Try
          For Each zipE As Ionic.Zip.ZipEntry In zip1
            zipE.FileName = System.IO.Path.GetFileName(zipE.FileName)
            zipE.Extract(TargetDir, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently)
          Next
        Catch invOpEx As InvalidOperationException
          If invOpEx.Message.Contains("Collection was modified; enumeration operation may not execute") Then
            'Should be due to skipping path in extraction (change of FileName property, so ignore.
            'File seems to extract anyway.
            'retVal = True
          Else
            callInfo &= MethodIdentifier() & " invOpEx error: " & invOpEx.Message
          End If
        Catch ex As Exception
          callInfo &= MethodIdentifier() & " ex error: " & ex.Message
        End Try
      End Using

      retVal = True 'Made it here, should be good
    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Move uploaded files into proper folders.
  ''' </summary>
  Public Shared Function MoveUploadFilesIntoProjectFolders(ByVal projectFolderName As String, ByVal uploadedFileName As String, _
                         ByRef callInfo As String) As String
    Dim xmlFileFullName As String = ""
    Dim xmlFileNameOnly As String = ""

    Try
      Dim archiveDir As String = Path.Combine(projectFolderName, CV.ProjectArchiveFolder)
      Dim userDir As String = Path.Combine(projectFolderName, CV.ProjectUserFolder)
      Dim supportDir As String = Path.Combine(projectFolderName, CV.ProjectSupportFolder)

      Dim uploadedPathAndFileName As String = Path.Combine(projectFolderName, uploadedFileName)

      ' See if a valid mmp xml file is present
      Dim dirInfo As New IO.DirectoryInfo(projectFolderName & "\")
      Dim strFiles As IO.FileInfo() = dirInfo.GetFiles("*.xml")
      Dim xpathDoc As XPath.XPathDocument
      Dim xmlNav As XPath.XPathNavigator
      Dim xmlNI As XPath.XPathNodeIterator
      For Each strfi As IO.FileInfo In strFiles
        xpathDoc = New XPath.XPathDocument(strfi.FullName)
        xmlNav = xpathDoc.CreateNavigator()
        Dim xmlpath As String = "/MmpPlan/PlanInfo/Source"
        xmlNI = xmlNav.Select(xmlpath)
        If xmlNI.Count > 0 Then '   xml is valid
          xmlFileFullName = strfi.Name
          xmlFileNameOnly = strfi.Name.Split("."c)(0) ' Can use this to find files with same name as plan file
        End If
      Next

      ' Make new archive folder
      'Dim ArchiveDateN As String = Replace(Replace(Replace(Date.Now, "/", "_"), ":", ""), " ", "_")
      Dim ArchiveDateN As String = Now.ToString("u").Replace(":"c, "_"c) ' e.g. 2011-06-10 15_23_42Z
      Dim newArchiveFolder As String = Path.Combine(archiveDir, "Archive__" & ArchiveDateN).Replace(" ", "__") ' e.g. Archive__2011-06-10__15_23_42Z
      If Not System.IO.Directory.Exists(newArchiveFolder) Then MkDir(newArchiveFolder)
      System.IO.File.Copy(uploadedPathAndFileName, Path.Combine(newArchiveFolder, uploadedFileName)) ' archive the uploaded file
      If uploadedFileName.EndsWith(".zip") Then
        If System.IO.File.Exists(uploadedPathAndFileName) Then System.IO.File.Delete(uploadedPathAndFileName) ' cleanup
      End If

      ' Move files to correct folder
      Dim currFile As String
      strFiles = dirInfo.GetFiles("*.*")
      For Each fi As IO.FileInfo In strFiles
        callInfo &= Environment.NewLine & fi.Name
        'If fi.Name <> uploadedFileName Then
        currFile = Path.Combine(projectFolderName, fi.Name)
        callInfo &= Environment.NewLine & currFile

        Select Case True
          Case fi.Name.EndsWith(".gdb"), fi.Name.EndsWith(".r2.xml")
            'Put rusle files in support folder
            callInfo &= Environment.NewLine & "rusle"
            Try
              callInfo &= Environment.NewLine & Path.Combine(supportDir, fi.Name)
              File.Move(currFile, Path.Combine(supportDir, fi.Name))
              File.Delete(currFile)
            Catch ex As Exception
              callInfo &= Environment.NewLine & fi.Name & " error: " & ex.Message.ToString & Environment.NewLine
            End Try
          Case fi.Name.EndsWith(".xsd"), fi.Name.EndsWith(".consplan.xml")
            'Delete files not needed
            callInfo &= Environment.NewLine & "2"
            Try
              File.Delete(currFile)
            Catch ex As Exception
              callInfo &= Environment.NewLine & fi.Name & " error: " & ex.Message.ToString & Environment.NewLine
            End Try
          Case fi.Name.EndsWith(".xml")
            'Put xml files in support folder
            callInfo &= Environment.NewLine & "support"
            Try
              callInfo &= Environment.NewLine & Path.Combine(supportDir, fi.Name)
              File.Move(currFile, Path.Combine(supportDir, fi.Name))
              File.Delete(currFile)
            Catch ex As Exception
              callInfo &= Environment.NewLine & fi.Name & " error: " & ex.Message.ToString & Environment.NewLine
            End Try
          Case Else
            'Put all other files in user folder
            callInfo &= Environment.NewLine & "user"
            Try
              callInfo &= Environment.NewLine & Path.Combine(userDir, fi.Name)
              File.Move(currFile, Path.Combine(userDir, fi.Name))
              File.Delete(currFile)
            Catch ex As Exception
              callInfo &= Environment.NewLine & fi.Name & " error: " & ex.Message.ToString & Environment.NewLine
            End Try
        End Select
        'End If
      Next

    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return xmlFileFullName
  End Function

#Region "Folders and files"

  ''' <summary>
  ''' Creates base folders for project.
  ''' </summary>
  Public Shared Function CreateBaseProjectFolders(ByVal projectFolderName As String, ByRef callInfo As String) As Boolean
    Dim retVal As Boolean = True
    Try
      Dim dirNames() As String = CV.BaseProjectFolders
      Dim currDir As String
      For indx As Integer = 0 To UBound(dirNames)
        currDir = projectFolderName & "\" & dirNames(indx)
        If Not System.IO.Directory.Exists(currDir) Then MkDir(currDir)
      Next

      For indx As Integer = 0 To UBound(dirNames)
        currDir = projectFolderName & "\" & dirNames(indx)
        If Not System.IO.Directory.Exists(currDir) Then retVal = False
      Next
    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Deletes project files from path.
  ''' </summary>
  Public Shared Function DeleteProjectFiles(ByVal projectFolder As String, ByRef callInfo As String) As String
    Dim retVal As String = ""
    Try
      Dim allFiles() As String
      Dim dirNames() As String = CV.BaseProjectFolders
      Dim currFolder As String
      For k As Integer = 0 To dirNames.Length - 1
        currFolder = Path.Combine(projectFolder, dirNames(k))
        If System.IO.Directory.Exists(currFolder) Then
          allFiles = System.IO.Directory.GetFiles(currFolder, "*", IO.SearchOption.AllDirectories)
          For Each fyle As String In allFiles
            If Not fyle.Contains(".svn") Then
              System.IO.File.Delete(fyle)
            End If
          Next
        End If
      Next
    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Deletes project folder.
  ''' </summary>
  Public Shared Sub DeleteProjectFolders(ByVal projectFolder As String, Optional ByRef callInfo As String = "")
    Try
      Dim dirNames() As String = CV.BaseProjectFolders
      Dim path As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) & projectFolder
      System.IO.Directory.Delete(path, True)
    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
  End Sub

#End Region

  ''' <summary>
  ''' Returns number of rows deleted from PermissionRoles table based on project id and creator id.
  ''' </summary>
  Public Shared Function DeleteProjectRolePermissionByProjectIdByUserId(ByVal projectId As Long, ByVal usrId As Guid, _
                                                                        ByRef callInfo As String) As Integer

    Dim retVal As Integer = -1
    Try
      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = String.Format(<a>
                  DELETE FROM {0}.PermissionRoles 
                  WHERE ProjectOid = @projectId AND CreatorGuid = @usrId 
                                          </a>.Value, dataSchema)
          With cmd.Parameters
            .Add("@projectId", SqlDbType.BigInt).Value = projectId
            .Add("@usrId", SqlDbType.UniqueIdentifier).Value = usrId
          End With

          If conn.State = ConnectionState.Closed Then conn.Open()
          retVal = cmd.ExecuteNonQuery()
        End Using
      End Using

    Catch ex As Exception
      callInfo &= MethodIdentifier() & " error: " & ex.Message
    End Try
    Return retVal
  End Function

  ''' <summary>
  ''' Returns array list of IDs from a given table matching Project ID
  ''' </summary> 
  ''' <remarks>An entry in the return array will be -1 if record ID is not an integer</remarks>
  Public Shared Function GetAllObjectIdsByProjectIdAndTableName(ByVal projectId As Long, ByVal tableName As String, _
                                               ByRef callInfo As String) As ArrayList
    callInfo = MethodIdentifier()
    Dim retVal As New ArrayList
    Try
      Using conn As New SqlConnection(dataConn)
        Using cmd As SqlCommand = conn.CreateCommand()
          cmd.CommandText = _
              "SELECT " & tableName & ".ObjectID " & _
              " FROM " & dataSchema & "." & tableName & " INNER JOIN " & _
                  dataSchema & ".ProjectDatum ON " & dataSchema & "." & tableName & ".ObjectID=" & dataSchema & ".ProjectDatum.ObjectID " & _
              " INNER JOIN " & _
                  dataSchema & ".Project ON " & dataSchema & ".Project.ObjectID=" & dataSchema & ".ProjectDatum.ProjectID " & _
              " WHERE Project.ObjectID=" & projectId & ""
          If conn.State = ConnectionState.Closed Then conn.Open()

          Dim readr As SqlDataReader = cmd.ExecuteReader
          While readr.Read
            Try
              retVal.Add(CInt(readr(0)))
            Catch ex As Exception 'error if cant convert to integer
              retVal.Add(-1)
            End Try
          End While
        End Using
      End Using

      callInfo &= ": " & "Okay"
    Catch ex As Exception
      callInfo &= " error: " & ex.Message
    End Try
    Return retVal
  End Function

#Region "Info/Messages/Errors"

  Public Shared Function FormatMethodIdentifier(ByVal declarer As String, ByVal method As String) As String
    'Used for error message attributes (title)
    Try
      'Return String.Format("{1}", declarer, method) 'just give user the method
      Return String.Format("{0}.{1}", declarer, method)
    Catch ex As Exception
      Return "Format Method Identifier failed"
    End Try
  End Function

  ''' <summary>
  ''' Formats a message with title and body labels (circumvents javascript alert window's shortcoming of non-alterable title text)
  ''' </summary>
  Public Shared Function FormatMessageForJSAlert(ByVal title As String, ByVal message As String, _
                                                 Optional ByVal includeNewLines As Boolean = True) As String
    Return If(includeNewLines, Environment.NewLine, "").ToString & _
            If(title.Length > 0, title, "<No title>").ToString & Environment.NewLine & Environment.NewLine & _
            If(message.Length > 0, message, "<No message>").ToString & _
            If(includeNewLines, Environment.NewLine, "").ToString
  End Function

  ''' <summary>
  ''' Returns formatted identifier of project and calling method
  ''' </summary>
  Private Shared Function MethodIdentifier() As String
    'Used for error message attributes (title)
    Try
      Return FormatMethodIdentifier(System.Reflection.MethodBase.GetCurrentMethod.DeclaringType.Name, New System.Diagnostics.StackFrame(1).GetMethod().Name)
    Catch ex As Exception
      Return "CommonFunctions MethodIdentifier didn't work"
    End Try
  End Function

  Private Shared Function ShowError(callingMethod As String, inEx As Exception) As String
    Try
      Return String.Format("{0} error: {1}", callingMethod, FormatErrorMessage(inEx))
    Catch ex As Exception
      Return "CommonFunctions ShowError error: " + ex.Message
    End Try
    'hard code return to avoid endless loop
  End Function

  Private Shared Function FormatErrorMessage(inEx As Exception) As String
    'Used for error message attributes
    Try
      Dim CurrentStack As New System.Diagnostics.StackTrace(inEx, True)
      Dim fln As Integer = CurrentStack.GetFrame(CurrentStack.GetFrames().Length - 1).GetFileLineNumber()
      Dim lnNum As String = If(fln <> 0, " (line " & fln.ToString() & ") ", "")
      ' vb doesn't report line number in release mode
      'use pipe to try and avoid asyncpostback locking from when an error occurs
      Return lnNum & Convert.ToString(inEx.Message) & "|"
    Catch ex As Exception
      Return "CommonFunctions FormatErrorMessage error: " + ex.Message
    End Try
    'hard code return to avoid endless loop
  End Function

#End Region

End Class
