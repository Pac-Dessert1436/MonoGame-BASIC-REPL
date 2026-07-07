Namespace Models
    Public Enum TokenType
        Number
        [String]
        Identifier
        Keyword
        [Operator]
        Punctuation
        EndOfLine
    End Enum

    Public Class BasicToken
        Public Property Type As TokenType
        Public Property Value As String
        Public Property Line As Integer
        Public Property Column As Integer

        Public Sub New(type As TokenType, value As String, line As Integer, column As Integer)
            Me.Type = type
            Me.Value = value
            Me.Line = line
            Me.Column = column
        End Sub
    End Class
End Namespace