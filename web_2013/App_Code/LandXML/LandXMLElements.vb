Option Strict On

Namespace LandXML

  Partial Public Class LandXML

    ''' <remarks>http://www.landxml.org/schema/LandXML-1.2/documentation/LandXML-1.2Doc_Feature.html</remarks>
    Private Class Feature
      Private ElementName As String = "Feature"
      Private name As String = ""
      Private code As String = ""
      Private source As String = ""

      Sub New(code As String, Optional name As String = "", Optional source As String = "")
        Me.code = code
        Me.name = name
        Me.source = source
      End Sub

      ''' <summary>
      ''' Create XML element.
      ''' </summary>
      Public Function ToXml() As XElement
        Dim retVal As New XElement(ns + ElementName)
        Try
          If Not String.IsNullOrWhiteSpace(code) Then retVal.Add(New XAttribute("code", code))
          If Not String.IsNullOrWhiteSpace(name) Then retVal.Add(New XAttribute("name", name))
          If Not String.IsNullOrWhiteSpace(source) Then retVal.Add(New XAttribute("source", source))

        Catch ex As Exception
          errorInfo &= name & " ToXml error: " & ex.ToString
        End Try
        Return retVal
      End Function

    End Class

    ''' <remarks>http://www.landxml.org/schema/LandXML-1.2/documentation/LandXML-1.2Doc_Point.html</remarks>
    Private Class Point
      Public northing As Double = Double.MinValue
      Public easting As Double = Double.MinValue
      Public zing As Double = Double.MinValue

      Public Overrides Function ToString() As String
        Dim values As New List(Of String)
        If northing <> Double.MinValue Then
          values.Add(northing.ToString("F10"))
          values.Add(easting.ToString("F10"))
          If zing <> Double.MinValue Then values.Add(zing.ToString("F10"))
        End If
        'If values.Count = 0 Then Return ""
        Return String.Join(" ", values.ToArray).Trim
      End Function
    End Class

    ''' <remarks>http://www.landxml.org/schema/LandXML-1.2/documentation/LandXML-1.2Doc_IrregularLine.html</remarks>
    Private Class IrregularLine
      Private ElementName As String = "IrregularLine"
      Public StartPoint As New Point
      Public EndPoint As New Point
      Public PntList As String = ""
      Public desc As String = ""
      Public oID As String = ""

      ''' <summary>
      ''' Create XML element.
      ''' </summary>
      Public Function ToXml() As XElement
        Dim retVal As New XElement(ns + ElementName)
        Try
          Dim first As New XElement(ns + "Start" _
            , StartPoint.ToString() _
            )
          Dim last As New XElement(ns + "End" _
            , EndPoint.ToString() _
            )
          Dim points As New XElement(ns + "PntList2D" _
            , PntList _
            )

          retVal.Add( _
             first _
            , last _
            , points _
            )
          If Not String.IsNullOrWhiteSpace(desc) Then retVal.Add(New XAttribute("desc", desc))
          If Not String.IsNullOrWhiteSpace(oID) Then retVal.Add(New XAttribute("oID", oID))

        Catch ex As Exception
          errorInfo &= ElementName & " ToXml error: " & ex.ToString
        End Try
        Return retVal
      End Function

    End Class

    ''' <remarks>http://www.landxml.org/schema/LandXML-1.2/documentation/LandXML-1.2Doc_Surface.html</remarks>
    Private Class Surface
      Private ElementName As String = "Surface"

      'TODO: update 

      'Public StartPoint As New Point
      'Public EndPoint As New Point
      'Public PntList As String = ""
      'Public desc As String = ""
      'Public oID As String = ""

      ' ''' <summary>
      ' ''' Create XML element for an irregular line.
      ' ''' </summary>
      'Public Function ToXml() As XElement
      '  Dim retVal As New XElement(ns + Name)
      '  Try
      '    Dim first As New XElement("Start" _
      '      , StartPoint.ToString() _
      '      )
      '    Dim last As New XElement("End" _
      '      , EndPoint.ToString() _
      '      )
      '    Dim points As New XElement("PntList2D" _
      '      , PntList _
      '      )

      '    retVal.add( _
      '       New XElement(first) _
      '      , New XElement(last) _
      '      , New XElement(points) _
      '      )
      '    If Not String.IsNullOrWhiteSpace(desc) Then retVal.Add(New XAttribute("desc", desc))
      '    If Not String.IsNullOrWhiteSpace(oID) Then retVal.Add(New XAttribute("oID", oID))

      '  Catch ex As Exception
      '    errorInfo &= Name & " ToXml error: " & ex.ToString
      '  End Try
      '  Return retVal
      'End Function

    End Class

  End Class

End Namespace
