Namespace Models
    Public MustInherit Class AstNode
        Public Property Line As Integer
        Public Property Column As Integer
    End Class

    Public Class NumberLiteralNode
        Inherits AstNode
        Public Property Value As Double
    End Class

    Public Class StringLiteralNode
        Inherits AstNode
        Public Property Value As String
    End Class

    Public Class VariableRefNode
        Inherits AstNode
        Public Property Name As String
    End Class

    Public Class BinaryOpNode
        Inherits AstNode
        Public Property Left As AstNode
        Public Property [Operator] As String
        Public Property Right As AstNode
    End Class

    Public Class UnaryOpNode
        Inherits AstNode
        Public Property [Operator] As String
        Public Property Operand As AstNode
    End Class

    Public Class PrintNode
        Inherits AstNode
        Public Property Expressions As List(Of AstNode)
    End Class

    Public Class LetNode
        Inherits AstNode
        Public Property VariableName As String
        Public Property Expression As AstNode
    End Class

    Public Class DefFnNode
        Inherits AstNode
        Public Property FunctionName As String
        Public Property Parameters As List(Of String)
        Public Property Expression As AstNode
    End Class

    Public Class FunctionCallNode
        Inherits AstNode
        Public Property FunctionName As String
        Public Property Arguments As List(Of AstNode)
    End Class

    Public Class BuiltinFunctionNode
        Inherits AstNode
        Public Property FunctionName As String
        Public Property Arguments As List(Of AstNode)
    End Class

    Public Class BooleanLiteralNode
        Inherits AstNode
        Public Property Value As Boolean
    End Class

    Public Class IfThenNode
        Inherits AstNode
        Public Property Condition As AstNode
        Public Property ThenStatement As AstNode
    End Class

    Public Class GotoNode
        Inherits AstNode
        Public Property LineNumber As Integer
    End Class

    Public Class GosubNode
        Inherits AstNode
        Public Property LineNumber As Integer
    End Class

    Public Class ReturnNode
        Inherits AstNode
    End Class

    Public Class EndNode
        Inherits AstNode
    End Class

    Public Class ForNode
        Inherits AstNode
        Public Property VariableName As String
        Public Property StartExpression As AstNode
        Public Property EndExpression As AstNode
        Public Property StepExpression As AstNode
    End Class

    Public Class NextNode
        Inherits AstNode
        Public Property VariableName As String
    End Class
End Namespace