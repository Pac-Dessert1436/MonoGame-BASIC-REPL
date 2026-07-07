Namespace Models
    Public Enum AppMode
        Repl
        Editor
    End Enum

    Public Class DisplayLine
        Public Property Text As String = ""
        Public Property IsOutput As Boolean = False
    End Class

    Public Class BasicCodeCache
        Public Property FileName As String = ""
        Public Property Lines As New List(Of String)
        Public ReadOnly Property DisplayName As String
            Get
                Return If(FileName.Length = 0, "<untitled>", FileName)
            End Get
        End Property
    End Class

    Public Class ReplState
        Public Property DisplayLines As New List(Of DisplayLine)
        Public Property CurrentInput As String = ""
        Public Property CursorColumn As Integer = 0
        Public Property Variables As New Dictionary(Of String, Object)
        Public Property Functions As New Dictionary(Of String, DefFnNode)
        Public Property CurrentLine As Integer = 1
        Public Property ScrollOffset As Integer = 0

        Public Property Mode As AppMode = AppMode.Repl
        Public Property CodeCache As New BasicCodeCache
        Public Property EditorCursorRow As Integer = 0
        Public Property EditorCursorColumn As Integer = 0
        Public Property EditorScrollOffset As Integer = 0
    End Class
End Namespace