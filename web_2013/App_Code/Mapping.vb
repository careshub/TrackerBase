Option Explicit On
Option Strict On

#Region "Imports"
Imports Microsoft.VisualBasic
Imports System.Data
Imports System.Data.SqlClient
Imports System.Runtime.Serialization
Imports System.IO
Imports System.Reflection
Imports System.Runtime.Serialization.Json
Imports System.Web.Script.Serialization
Imports CF = CommonFunctions
Imports CV = CommonVariables
Imports GISTools
Imports GTA = GIS.GISToolsAddl 
Imports TerLoc.Model
Imports GGeom = GeoAPI.Geometries
Imports NGeom = NetTopologySuite.Geometries

Imports System
Imports System.Collections.Generic 
Imports NetTopologySuite.Geometries
Imports NetTopologySuite.IO

#End Region

Public Class Mapping

#Region "Module declarations"
  Private Shared dataSchema As String = CF.GetDataSchemaName
  Private Shared dataConn As String = CF.GetBaseDatabaseConnString
  Private Shared aspNetDb As String = CF.GetAspNetDatabaseName
  Private Shared aspNetConn As String = CF.GetNetDatabaseConnString

  Private Shared ReadOnly coordsPrecision As Integer = CV.CoordinatePrecision
  Private Shared ReadOnly sqMtrsPerAcre As Double = CV.AcresToSquareMetersMultiplier

#End Region

#Region "Record copying"

  Public Shared Sub CopyFieldRecords(ByVal projectId As Long, ByVal origFeatureId As Integer, ByVal newFeatureId As Integer, ByRef callInfo As String)
    Dim localInfo As String = ""
    Try

    Catch ex As Exception
      callInfo &= String.Format("{0} error: {1} ({2})", MethodIdentifier(), ex.Message, ex.InnerException.Message)
    End Try
  End Sub

#End Region

  Private Shared Function MethodIdentifier() As String
    'Used for error message attributes (title)
    Try
      Return CF.FormatMethodIdentifier(System.Reflection.MethodBase.GetCurrentMethod.DeclaringType.Name, New System.Diagnostics.StackFrame(1).GetMethod().Name)
    Catch ex As Exception
      Return "Mapping MethodIdentifier didn't work"
    End Try
  End Function

End Class
