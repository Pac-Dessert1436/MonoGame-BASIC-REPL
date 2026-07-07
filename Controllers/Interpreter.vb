Imports MonoGame_BASIC_REPL.Models

Namespace Controllers
    Public NotInheritable Class Interpreter
        Private ReadOnly _state As ReplState

        Public Sub New(replState As ReplState)
            _state = replState
        End Sub

        Private _nodeJumpTarget As Integer? = Nothing

        Public Function Execute(nodes As List(Of AstNode)) As String
            Dim output As New Text.StringBuilder
            Dim nodeIndex As Integer = 0
            _loopStack.Clear()
            _currentLineMap = Nothing
            _nodeJumpTarget = Nothing

            While nodeIndex < nodes.Count
                Dim node As AstNode = nodes(nodeIndex)
                nodeIndex += 1
                ' Let ExecuteFor/ExecuteNext know the position of the next node
                _currentNodeIndex = nodeIndex
                Dim result As String = ExecuteNode(node)
                If result IsNot Nothing Then output.Append(result)
                If _nodeJumpTarget.HasValue Then
                    nodeIndex = _nodeJumpTarget.Value
                    _nodeJumpTarget = Nothing
                End If
            End While
            _currentNodeIndex = 0
            Return output.ToString()
        End Function

        Private Function ExecuteNode(node As AstNode) As String
            If TypeOf node Is PrintNode Then
                Return ExecutePrint(CType(node, PrintNode))
            ElseIf TypeOf node Is LetNode Then
                Return ExecuteLet(CType(node, LetNode))
            ElseIf TypeOf node Is DefFnNode Then
                Return ExecuteDefFn(CType(node, DefFnNode))
            ElseIf TypeOf node Is IfThenNode Then
                Return ExecuteIfThen(CType(node, IfThenNode))
            ElseIf TypeOf node Is GotoNode Then
                Return ExecuteGoto(CType(node, GotoNode))
            ElseIf TypeOf node Is GosubNode Then
                Return ExecuteGosub(CType(node, GosubNode))
            ElseIf TypeOf node Is ReturnNode Then
                Return ExecuteReturn()
            ElseIf TypeOf node Is EndNode Then
                Return ExecuteEnd()
            ElseIf TypeOf node Is ForNode Then
                Return ExecuteFor(CType(node, ForNode))
            ElseIf TypeOf node Is NextNode Then
                Return ExecuteNext(CType(node, NextNode))
            Else
                Dim value = Evaluate(node)
                If value IsNot Nothing Then Return FormatValue(value) & vbLf
                Return Nothing
            End If
        End Function

        Private _gotoTarget As Integer? = Nothing
        Private ReadOnly _gosubStack As New Stack(Of Integer)
        Private ReadOnly _loopStack As New Stack(Of ForLoopContext)
        Private _currentLine As Integer = 0
        Private _currentLineMap As Dictionary(Of Integer, String) = Nothing
        Private _currentNodeIndex As Integer = 0

        Private Class ForLoopContext
            Public VariableName As String
            Public EndValue As Double
            Public StepValue As Double
            Public BodyLineNumber As Integer
            Public BodyNodeIndex As Integer
        End Class

        Private Function ExecuteIfThen(node As IfThenNode) As String
            Dim condition = CBool(Evaluate(node.Condition))
            If condition Then Return ExecuteNode(node.ThenStatement)
            Return Nothing
        End Function

        Private Function ExecuteGoto(node As GotoNode) As String
            _gotoTarget = node.LineNumber
            Return Nothing
        End Function

        Private Function ExecuteGosub(node As GosubNode) As String
            Dim returnLine As Integer = GetNextLineNumberFromCurrentMap(_currentLine)
            If returnLine = Integer.MaxValue Then
                Throw New InvalidOperationException("GOSUB at last line has no return point")
            End If
            _gosubStack.Push(returnLine)
            _gotoTarget = node.LineNumber
            Return Nothing
        End Function

        Private Function ExecuteReturn() As String
            If _gosubStack.Count = 0 Then Throw New InvalidOperationException("RETURN without GOSUB")
            _gotoTarget = _gosubStack.Pop()
            Return Nothing
        End Function

        Private Function ExecuteEnd() As String
            ' END keyword terminates program execution immediately
            _gotoTarget = Integer.MaxValue
            Return Nothing
        End Function

        Private Function ExecuteFor(node As ForNode) As String
            Dim startVal = CDbl(Evaluate(node.StartExpression))
            Dim endVal = CDbl(Evaluate(node.EndExpression))
            Dim stepVal = If(node.StepExpression IsNot Nothing, CDbl(Evaluate(node.StepExpression)), 1.0)

            If stepVal = 0 Then
                Throw New InvalidOperationException($"FOR step cannot be zero at line {node.Line}")
            End If

            _state.Variables(node.VariableName) = startVal

            Dim bodyLine As Integer = Integer.MaxValue
            Dim bodyNodeIndex As Integer = -1
            If _currentLineMap IsNot Nothing Then
                bodyLine = GetNextLineNumber(_currentLineMap, _currentLine)
            Else
                bodyNodeIndex = _currentNodeIndex
            End If

            _loopStack.Push(New ForLoopContext() With {
                .VariableName = node.VariableName,
                .EndValue = endVal,
                .StepValue = stepVal,
                .BodyLineNumber = bodyLine,
                .BodyNodeIndex = bodyNodeIndex
            })

            Dim conditionMet = If(stepVal > 0, startVal <= endVal, startVal >= endVal)
            If Not conditionMet Then _loopStack.Pop()

            Return Nothing
        End Function

        Private Function ExecuteNext(node As NextNode) As String
            If _loopStack.Count = 0 Then
                Throw New InvalidOperationException($"NEXT without FOR at line {node.Line}")
            End If

            Dim ctx As ForLoopContext = _loopStack.Pop()

            If node.VariableName.Length > 0 AndAlso
               Not String.Equals(node.VariableName, ctx.VariableName, StringComparison.OrdinalIgnoreCase) Then
                Throw New InvalidOperationException(
                    $"NEXT '{node.VariableName}' does not match FOR '{ctx.VariableName}' at line {node.Line}")
            End If

            Dim currentVal As Double = CDbl(_state.Variables(ctx.VariableName))
            currentVal += ctx.StepValue
            _state.Variables(ctx.VariableName) = currentVal

            Dim conditionMet As Boolean = If(ctx.StepValue > 0, currentVal <= ctx.EndValue, currentVal >= ctx.EndValue)

            If conditionMet Then
                _loopStack.Push(ctx)
                If _currentLineMap IsNot Nothing AndAlso ctx.BodyLineNumber <> Integer.MaxValue Then
                    _gotoTarget = ctx.BodyLineNumber
                ElseIf ctx.BodyNodeIndex >= 0 Then
                    _nodeJumpTarget = ctx.BodyNodeIndex
                End If
            End If

            Return Nothing
        End Function

        Private Shared Function GetNextLineNumber(lineMap As Dictionary(Of Integer, String), currentLineNum As Integer) As Integer
            Dim sortedLines = lineMap.Keys.ToList()
            sortedLines.Sort()
            Dim index = sortedLines.BinarySearch(currentLineNum)
            If index >= 0 AndAlso index < sortedLines.Count - 1 Then
                Return sortedLines(index + 1)
            End If
            Return Integer.MaxValue
        End Function

        Private Function GetNextLineNumberFromCurrentMap(currentLineNum As Integer) As Integer
            If _currentLineMap Is Nothing Then Return Integer.MaxValue
            Return GetNextLineNumber(_currentLineMap, currentLineNum)
        End Function

        Private Function ExecuteDefFn(node As DefFnNode) As String
            _state.Functions(node.FunctionName) = node
            Return Nothing
        End Function

        Public Function ExecuteListCode() As String
            Dim output As New Text.StringBuilder
            If _state.CodeCache.Lines.Count = 0 Then
                output.AppendLine("No code in editor.")
            Else
                For i As Integer = 0 To _state.CodeCache.Lines.Count - 1
                    output.AppendLine($"{(i + 1) * 10} {_state.CodeCache.Lines(i)}")
                Next i
            End If
            Return output.ToString()
        End Function

        Public Shared Function ExecuteListApi() As String
            Dim output As New Text.StringBuilder
            output.AppendLine("=== BUILT-IN FUNCTIONS ===")
            output.AppendLine("ABS(x)       - Absolute value")
            output.AppendLine("SQR(x)       - Square root")
            output.AppendLine("SIN(x)       - Sine (radians)")
            output.AppendLine("COS(x)       - Cosine (radians)")
            output.AppendLine("TAN(x)       - Tangent (radians)")
            output.AppendLine("LOG(x)       - Natural logarithm")
            output.AppendLine("EXP(x)       - Exponential")
            output.AppendLine("INT(x)       - Integer part")
            output.AppendLine("RND()        - Random number (0-1)")
            output.AppendLine("LEN(s)       - String length")
            output.AppendLine("LEFT(s,n)    - Left n characters")
            output.AppendLine("RIGHT(s,n)   - Right n characters")
            output.AppendLine("MID(s,n[,l]) - Middle substring")
            output.AppendLine("UCASE(s)     - Convert to uppercase")
            output.AppendLine("LCASE(s)     - Convert to lowercase")
            output.AppendLine("IIF(b,t,f)   - Immediate IF (ternary operator)")
            output.AppendLine("=== BASIC OPERATORS ===")
            output.AppendLine("+ - * /      - Arithmetic operators")
            output.AppendLine("MOD          - Modulus (remainder) operator")
            output.AppendLine("&            - String concatenation operator")
            output.AppendLine("= < > <= >=   - Comparison operators")
            output.AppendLine("=== LOOPS ===")
            output.AppendLine("FOR var=s TO e [STEP n] - Counted loop (default STEP is 1)")
            output.AppendLine("NEXT [var]              - End of loop body")
            output.AppendLine("=== STATEMENT SEPARATOR ===")
            output.AppendLine("stmt1 : stmt2 : ...  - Multiple statements on one line")
            output.AppendLine("=== USEFUL COMMANDS ===")
            output.AppendLine("PRINT expr1[, expr2..] - Print expressions (space-separated)")
            output.AppendLine("PRINT                  - Print a blank line")
            output.AppendLine("LET var=expr           - Assign value to variable")
            output.AppendLine("DEF FN name(args)=expr - Define a custom function")
            output.AppendLine("IF cond THEN stmt      - Conditional execution")
            output.AppendLine("GOTO line              - Jump to line number")
            output.AppendLine("GOSUB line             - Call subroutine")
            output.AppendLine("RETURN                 - Return from subroutine")
            output.AppendLine("END                    - Terminate program immediately")
            Return output.ToString()
        End Function

        Public Function ExecuteRun() As String
            Dim output As New Text.StringBuilder
            If _state.CodeCache.Lines.Count = 0 Then
                output.AppendLine("No code to run.")
                Return output.ToString()
            End If

            Dim originalVariables As New Dictionary(Of String, Object)(_state.Variables)
            Dim originalFunctions As New Dictionary(Of String, DefFnNode)(_state.Functions)

            _gotoTarget = Nothing
            _gosubStack.Clear()
            _loopStack.Clear()
            _currentLineMap = Nothing

            Try
                Dim lineMap As New Dictionary(Of Integer, String)
                For i As Integer = 0 To _state.CodeCache.Lines.Count - 1
                    Dim lineNum = (i + 1) * 10
                    Dim lineText = _state.CodeCache.Lines(i).Trim()
                    lineMap(lineNum) = lineText
                Next

                _currentLineMap = lineMap
                Dim currentLineNum As Integer = lineMap.Keys.Min()
                Const MAX_ITERATIONS As Integer = 1000000
                Dim iterations As Integer = 0

                Do While iterations < MAX_ITERATIONS AndAlso lineMap.ContainsKey(currentLineNum)
                    iterations += 1
                    _currentLine = currentLineNum
                    Dim lineText As String = lineMap(currentLineNum)
                    Dim shouldAdvance As Boolean = True

                    If Not String.IsNullOrWhiteSpace(lineText) Then
                        Dim tokens = New Lexer().Tokenize(lineText, currentLineNum)
                        Dim nodes = New Parser().Parse(tokens)
                        For Each node In nodes
                            Dim result As String = ExecuteNode(node)
                            If result IsNot Nothing Then
                                output.Append(result)
                            End If
                            If _gotoTarget.HasValue Then
                                currentLineNum = _gotoTarget.Value
                                _gotoTarget = Nothing
                                shouldAdvance = False
                                Exit For
                            End If
                        Next
                    End If

                    If shouldAdvance AndAlso Not _gotoTarget.HasValue Then
                        Dim nextLineNum = GetNextLineNumber(lineMap, currentLineNum)
                        If nextLineNum = Integer.MaxValue Then Exit Do
                        currentLineNum = nextLineNum
                    End If
                Loop

                If iterations >= MAX_ITERATIONS Then
                    Throw New InvalidOperationException("Infinite loop detected")
                End If
            Catch ex As Exception
                _state.Variables = originalVariables
                _state.Functions = originalFunctions
                Throw
            End Try

            Return output.ToString()
        End Function

        Private Function ExecutePrint(node As PrintNode) As String
            If node.Expressions.Count = 0 Then Return vbLf
            Dim parts As New List(Of String)
            For Each expr As AstNode In node.Expressions
                parts.Add(FormatValue(Evaluate(expr)))
            Next expr
            Return String.Join(" ", parts) & vbLf
        End Function

        Private Function ExecuteLet(node As LetNode) As String
            Dim value = Evaluate(node.Expression)
            _state.Variables(node.VariableName) = value
            Return Nothing
        End Function

        Private Function Evaluate(node As AstNode) As Object
            If TypeOf node Is NumberLiteralNode Then
                Return CType(node, NumberLiteralNode).Value
            ElseIf TypeOf node Is StringLiteralNode Then
                Return CType(node, StringLiteralNode).Value
            ElseIf TypeOf node Is BooleanLiteralNode Then
                Return CType(node, BooleanLiteralNode).Value
            ElseIf TypeOf node Is VariableRefNode Then
                Return EvaluateVariable(CType(node, VariableRefNode))
            ElseIf TypeOf node Is BinaryOpNode Then
                Return EvaluateBinaryOp(CType(node, BinaryOpNode))
            ElseIf TypeOf node Is UnaryOpNode Then
                Return EvaluateUnaryOp(CType(node, UnaryOpNode))
            ElseIf TypeOf node Is BuiltinFunctionNode Then
                Return EvaluateBuiltinFunction(CType(node, BuiltinFunctionNode))
            ElseIf TypeOf node Is FunctionCallNode Then
                Return EvaluateFunctionCall(CType(node, FunctionCallNode))
            Else
                Throw New InvalidOperationException($"Unknown node type: {node.GetType().Name}")
            End If
        End Function

        Private Function EvaluateBuiltinFunction(node As BuiltinFunctionNode) As Object
            Dim args = Aggregate a In node.Arguments Select Evaluate(a) Into ToArray()

            Select Case node.FunctionName
                Case "ABS"
                    Return Math.Abs(CDbl(args(0)))
                Case "SQR"
                    Return Math.Sqrt(CDbl(args(0)))
                Case "SIN"
                    Return Math.Sin(CDbl(args(0)))
                Case "COS"
                    Return Math.Cos(CDbl(args(0)))
                Case "TAN"
                    Return Math.Tan(CDbl(args(0)))
                Case "LOG"
                    Return Math.Log(CDbl(args(0)))
                Case "EXP"
                    Return Math.Exp(CDbl(args(0)))
                Case "INT"
                    Return Math.Floor(CDbl(args(0)))
                Case "RND"
                    Return New Random().NextDouble()
                Case "LEN"
                    Return CStr(args(0)).Length
                Case "LEFT"
                    Return CStr(args(0)).Substring(0, CInt(args(1)))
                Case "RIGHT"
                    Dim s As String = CStr(args(0))
                    Dim n As Integer = CInt(args(1))
                    Return s.Substring(Math.Max(0, s.Length - n), Math.Min(n, s.Length))
                Case "MID"
                    Dim s As String = CStr(args(0))
                    Dim start As Integer = CInt(args(1)) - 1
                    Dim length As Integer = If(args.Length > 2, CInt(args(2)), s.Length - start)
                    Return s.Substring(start, length)
                Case "UCASE"
                    Return CStr(args(0)).ToUpperInvariant()
                Case "LCASE"
                    Return CStr(args(0)).ToLowerInvariant()
                Case "IIF"
                    If args.Length <> 3 Then
                        Throw New InvalidOperationException("IIF requires 3 parameters: IIF(condition, trueValue, falseValue) at line " & node.Line)
                    End If
                    Dim condition As Boolean = CBool(args(0))
                    Return If(condition, args(1), args(2))
                Case Else
                    Throw New InvalidOperationException($"Unknown built-in function '{node.FunctionName}' at line {node.Line}")
            End Select
        End Function

        Private Function EvaluateFunctionCall(node As FunctionCallNode) As Object
            Dim func As DefFnNode = Nothing

            If Not _state.Functions.TryGetValue(node.FunctionName, func) Then
                Throw New InvalidOperationException($"Undefined function '{node.FunctionName}' at line {node.Line}")
            End If

            If node.Arguments.Count <> func.Parameters.Count Then
                Throw New InvalidOperationException($"Function '{node.FunctionName}' expects {func.Parameters.Count} parameters, got {node.Arguments.Count} at line {node.Line}")
            End If

            Dim originalVariables As New Dictionary(Of String, Object)()
            For i As Integer = 0 To func.Parameters.Count - 1
                Dim paramName As String = func.Parameters(i)
                Dim value As Object = Nothing
                If _state.Variables.TryGetValue(paramName, value) Then
                    originalVariables(paramName) = value
                End If
                _state.Variables(paramName) = Evaluate(node.Arguments(i))
            Next

            Try
                Return Evaluate(func.Expression)
            Finally
                For Each paramName In func.Parameters
                    Dim value As Object = Nothing
                    If originalVariables.TryGetValue(paramName, value) Then
                        _state.Variables(paramName) = value
                    Else
                        _state.Variables.Remove(paramName)
                    End If
                Next
            End Try
        End Function

        Private Function EvaluateVariable(node As VariableRefNode) As Object
            Dim value As Object = Nothing
            If _state.Variables.TryGetValue(node.Name, value) Then Return value
            Throw New InvalidOperationException($"Undefined variable '{node.Name}' at line {node.Line}")
        End Function

        Private Function EvaluateBinaryOp(node As BinaryOpNode) As Object
            Dim left = Evaluate(node.Left)
            Dim right = Evaluate(node.Right)

            Select Case node.Operator
                Case "+"
                    If TypeOf left Is String OrElse TypeOf right Is String Then
                        Return CStr(left) & CStr(right)
                    End If
                    Return CDbl(left) + CDbl(right)
                Case "-"
                    Return CDbl(left) - CDbl(right)
                Case "*"
                    Return CDbl(left) * CDbl(right)
                Case "/"
                    Dim rightVal As Double = CDbl(right)
                    If rightVal = 0 Then
                        Throw New InvalidOperationException($"Division by zero at line {node.Line}")
                    End If
                    Return CDbl(left) / rightVal
                Case "="
                    Return left.Equals(right)
                Case "<"
                    Return CDbl(left) < CDbl(right)
                Case ">"
                    Return CDbl(left) > CDbl(right)
                Case "<="
                    Return CDbl(left) <= CDbl(right)
                Case ">="
                    Return CDbl(left) >= CDbl(right)
                Case "MOD"
                    Dim leftVal As Double = CDbl(left)
                    Dim rightVal As Double = CDbl(right)
                    If rightVal = 0 Then
                        Throw New InvalidOperationException("MOD by zero at line " & node.Line)
                    End If
                    Return leftVal - (Math.Floor(leftVal / rightVal) * rightVal)
                Case "&"
                    Return CStr(left) & CStr(right)
                Case Else
                    Throw New InvalidOperationException($"Unknown operator '{node.Operator}' at line {node.Line}")
            End Select
        End Function

        Private Function EvaluateUnaryOp(node As UnaryOpNode) As Double
            Dim operand = Evaluate(node.Operand)
            Select Case node.Operator
                Case "-"
                    Return -CDbl(operand)
                Case Else
                    Throw New InvalidOperationException($"Unknown unary operator '{node.Operator}' at line {node.Line}")
            End Select
        End Function

        Private Shared Function FormatValue(value As Object) As String
            If TypeOf value Is Double Then
                Dim d As Double = CDbl(value)
                If d = Math.Floor(d) AndAlso Not Double.IsInfinity(d) Then
                    Return CInt(d).ToString()
                End If
                Return d.ToString(Globalization.CultureInfo.InvariantCulture)
            ElseIf TypeOf value Is Boolean Then
                Return If(CBool(value), "TRUE", "FALSE")
            Else
                Return CStr(value)
            End If
        End Function
    End Class
End Namespace