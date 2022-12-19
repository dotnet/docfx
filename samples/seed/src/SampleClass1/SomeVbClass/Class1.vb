''' <summary>
''' This is summary from vb class...
''' </summary>
Public Class Class1
    Inherits BaseClass1

    Private _value As Integer

    ''' <summary>
    ''' This is a *Value* type
    ''' </summary>
    Public ValueClass As Class1

    <Obsolete("This member is obsolete.", True)>
    Public Shadows ReadOnly Property Keyword As CounterSampleCalculator
        Get
            Throw New ArgumentNullException()
        End Get
    End Property

    ''' <summary>
    ''' What is **Sub**?
    ''' </summary>
    Public Overrides Function WithDeclarationKeyword(keyword As CounterSampleCalculator) As DateTime
        Return DateTime.Now
    End Function

    ''' <summary>
    ''' This is a *Function*
    ''' </summary>
    ''' <param name="name">Name as the **String**
    ''' value</param>
    ''' <returns>**Returns**
    ''' Ahooo</returns>
    Public Function Value(ByVal name As String) As Integer
        Return _value * 2
    End Function
End Class

''' <summary>
''' This is the BaseClass
''' </summary>
Public MustInherit Class BaseClass1
    Public MustOverride Function WithDeclarationKeyword(keyword As CounterSampleCalculator) As DateTime
End Class
