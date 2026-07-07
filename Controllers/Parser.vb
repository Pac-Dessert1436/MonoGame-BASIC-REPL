Imports System.Globalization
Imports MonoGame_BASIC_REPL.Models

Namespace Controllers
    Public NotInheritable Class Parser
        Private _tokens As List(Of BasicToken)
        Private _position As Integer

        Public Function Parse(tokens As List(Of BasicToken)) As List(Of AstNode)
            _tokens = tokens
            _position = 0
            Dim statements As New List(Of AstNode)

            Do Until IsAtEnd()
                Dim stmt As AstNode = ParseStatement()
                If stmt IsNot Nothing Then statements.Add(stmt)
                ' If the statement ended with a colon, consume it and parse the next statement
                If Check(TokenType.Punctuation) AndAlso Peek().Value = ":" Then
                    Advance()
                End If
            Loop
            Return statements
        End Function

        Private Function ParseStatement() As AstNode
            While Check(TokenType.Punctuation) AndAlso Peek().Value = ":"
                Advance()
            End While
            If MatchKeyword("PRINT") Then
                Return ParsePrint()
            ElseIf MatchKeyword("LET") Then
                Return ParseLet()
            ElseIf MatchKeyword("DEF") Then
                Return ParseDefFn()
            ElseIf MatchKeyword("IF") Then
                Return ParseIfThen()
            ElseIf MatchKeyword("GOTO") Then
                Return ParseGoto()
            ElseIf MatchKeyword("GOSUB") Then
                Return ParseGosub()
            ElseIf MatchKeyword("RETURN") Then
                Return ParseReturn()
            ElseIf MatchKeyword("END") Then
                Return ParseEnd()
            ElseIf MatchKeyword("FOR") Then
                Return ParseFor()
            ElseIf MatchKeyword("NEXT") Then
                Return ParseNext()
            ElseIf Check(TokenType.Keyword) Then
                Dim kw As BasicToken = Advance()
                Throw New InvalidOperationException($"Unsupported keyword '{kw.Value}' at line {kw.Line}")
            ElseIf IsEndOfStatement() Then
                Return Nothing
            Else
                Dim expr As AstNode = ParseExpression()
                EnsureEndOfStatement("Expected end of line or statement separator")
                Return expr
            End If
        End Function

        Private Function ParseIfThen() As IfThenNode
            Dim ifKeyword As BasicToken = Previous()
            Dim condition As AstNode = ParseExpression()
            Consume(TokenType.Keyword, "Expected 'THEN' after IF condition")
            Dim thenStatement As AstNode = ParseStatement()
            EnsureEndOfStatement("Expected end of line after IF...THEN")

            Return New IfThenNode With {
                .Condition = condition,
                .ThenStatement = thenStatement,
                .Line = ifKeyword.Line,
                .Column = ifKeyword.Column
            }
        End Function

        Private Function ParseGoto() As GotoNode
            Dim keyword As BasicToken = Previous()
            Dim lineNumToken As BasicToken = Consume(TokenType.Number, "Expected line number after GOTO")
            EnsureEndOfStatement("Expected end of line after GOTO")

            Return New GotoNode With {
                .LineNumber = Integer.Parse(lineNumToken.Value),
                .Line = keyword.Line,
                .Column = keyword.Column
            }
        End Function

        Private Function ParseGosub() As GosubNode
            Dim keyword As BasicToken = Previous()
            Dim lineNumToken As BasicToken = Consume(TokenType.Number, "Expected line number after GOSUB")
            EnsureEndOfStatement("Expected end of line after GOSUB")

            Return New GosubNode With {
                .LineNumber = Integer.Parse(lineNumToken.Value),
                .Line = keyword.Line,
                .Column = keyword.Column
            }
        End Function

        Private Function ParseReturn() As ReturnNode
            Dim keyword As BasicToken = Previous()
            EnsureEndOfStatement("Expected end of line after RETURN")

            Return New ReturnNode With {
                .Line = keyword.Line,
                .Column = keyword.Column
            }
        End Function

        Private Function ParseEnd() As EndNode
            Dim keyword As BasicToken = Previous()
            EnsureEndOfStatement("Expected end of line after END")

            Return New EndNode With {
                .Line = keyword.Line,
                .Column = keyword.Column
            }
        End Function

        Private Function ParseFor() As ForNode
            Dim forKeyword As BasicToken = Previous()
            Dim nameToken = Consume(TokenType.Identifier, "Expected variable name after FOR")
            ConsumeOperator("=", "Expected '=' after FOR variable")
            Dim startExpr As AstNode = ParseExpression()
            Consume(TokenType.Keyword, "Expected 'TO' after start value")
            Dim endExpr As AstNode = ParseExpression()
            Dim stepExpr As AstNode = Nothing
            If MatchKeyword("STEP") Then
                stepExpr = ParseExpression()
            End If
            EnsureEndOfStatement("Expected end of line after FOR...TO...STEP")

            Return New ForNode() With {
                .VariableName = nameToken.Value.ToUpperInvariant(),
                .StartExpression = startExpr,
                .EndExpression = endExpr,
                .StepExpression = stepExpr,
                .Line = forKeyword.Line,
                .Column = forKeyword.Column
            }
        End Function

        Private Function ParseNext() As NextNode
            Dim nextKeyword As BasicToken = Previous()
            Dim variableName As String = ""
            If Check(TokenType.Identifier) Then
                Dim nameToken As BasicToken = Advance()
                variableName = nameToken.Value.ToUpperInvariant()
            End If
            EnsureEndOfStatement("Expected end of line after NEXT")

            Return New NextNode() With {
                .VariableName = variableName,
                .Line = nextKeyword.Line,
                .Column = nextKeyword.Column
            }
        End Function

        Private Function ParseDefFn() As DefFnNode
            Dim defKeyword As BasicToken = Previous()
            If Not MatchKeyword("FN") Then Throw New InvalidOperationException(
                $"Expected 'FN' after 'DEF' at line {defKeyword.Line}")
            Dim nameToken = Consume(TokenType.Identifier, "Expected function name after DEF FN")
            ConsumePunctuation("(", "Expected '(' after function name")

            Dim parameters As New List(Of String)
            If Not Check(TokenType.Punctuation) OrElse Peek().Value <> ")" Then
                Do
                    Dim paramToken = Consume(TokenType.Identifier, "Expected parameter name")
                    parameters.Add(paramToken.Value.ToUpperInvariant())
                Loop While Check(TokenType.Punctuation) AndAlso Peek().Value = "," AndAlso Advance() IsNot Nothing
            End If

            ConsumePunctuation(")", "Expected ')' after parameters")
            ConsumeOperator("=", "Expected '=' in DEF FN")
            Dim expr As AstNode = ParseExpression()
            EnsureEndOfStatement("Expected end of line after DEF FN")

            Return New DefFnNode() With {
                .FunctionName = nameToken.Value.ToUpperInvariant(),
                .Parameters = parameters,
                .Expression = expr,
                .Line = defKeyword.Line,
                .Column = defKeyword.Column
            }
        End Function

        Private Function ParsePrint() As PrintNode
            Dim keyword = Previous()
            Dim expressions As New List(Of AstNode)
            If Not IsEndOfStatement() Then
                expressions.Add(ParseExpression())
                While Check(TokenType.Punctuation) AndAlso Peek().Value = ","
                    Advance()
                    expressions.Add(ParseExpression())
                End While
            End If
            EnsureEndOfStatement("Expected end of line after PRINT")

            Return New PrintNode With {
                .Expressions = expressions,
                .Line = keyword.Line,
                .Column = keyword.Column
            }
        End Function

        Private Function ParseLet() As LetNode
            Dim keyword As BasicToken = Previous()
            Dim nameToken = Consume(TokenType.Identifier, "Expected variable name after LET")
            ConsumeOperator("=", "Expected '=' in LET assignment")
            Dim expr As AstNode = ParseExpression()
            EnsureEndOfStatement("Expected end of line after LET")

            Return New LetNode With {
                .VariableName = nameToken.Value.ToUpperInvariant(),
                .Expression = expr,
                .Line = keyword.Line,
                .Column = keyword.Column
            }
        End Function

        Private Function ParseExpression() As AstNode
            Return ParseComparison()
        End Function

        Private Function ParseComparison() As AstNode
            Dim expr As AstNode = ParseAdditive()

            Do While CheckOperator("=") OrElse CheckOperator("<") OrElse CheckOperator(">") OrElse CheckOperator("<=") OrElse CheckOperator(">=")
                Dim op As BasicToken = Advance()
                Dim right As AstNode = ParseAdditive()
                expr = New BinaryOpNode() With {
                    .Left = expr,
                    .Operator = op.Value,
                    .Right = right,
                    .Line = op.Line,
                    .Column = op.Column
                }
            Loop

            Return expr
        End Function

        Private Function ParseAdditive() As AstNode
            Dim expr As AstNode = ParseMultiplicative()

            While CheckOperator("+") OrElse CheckOperator("-") OrElse CheckOperator("&")
                Dim op As BasicToken = Advance()
                Dim right As AstNode = ParseMultiplicative()
                expr = New BinaryOpNode() With {
                    .Left = expr,
                    .Operator = op.Value,
                    .Right = right,
                    .Line = op.Line,
                    .Column = op.Column
                }
            End While

            Return expr
        End Function

        Private Function ParseMultiplicative() As AstNode
            Dim expr As AstNode = ParseUnary()

            Do
                Dim opToken As BasicToken
                If CheckOperator("*") OrElse CheckOperator("/") Then
                    opToken = Advance()
                ElseIf MatchKeyword("MOD") Then
                    opToken = Previous()
                    opToken = New BasicToken(TokenType.Operator, "MOD", opToken.Line, opToken.Column)
                Else
                    Exit Do
                End If

                Dim right As AstNode = ParseUnary()
                expr = New BinaryOpNode() With {
                    .Left = expr,
                    .Operator = opToken.Value,
                    .Right = right,
                    .Line = opToken.Line,
                    .Column = opToken.Column
                }
            Loop

            Return expr
        End Function

        Private Function ParseUnary() As AstNode
            If CheckOperator("-") Then
                Dim op As BasicToken = Advance()
                Dim operand As AstNode = ParseUnary()
                Return New UnaryOpNode() With {
                    .Operator = "-",
                    .Operand = operand,
                    .Line = op.Line,
                    .Column = op.Column
                }
            End If

            Return ParsePrimary()
        End Function

        Private Function ParsePrimary() As AstNode
            Static builtinFunctions As New HashSet(Of String) From {
                "ABS", "SQR", "SIN", "COS", "TAN", "LOG", "EXP", "INT", "RND", "LEN",
                "LEFT", "RIGHT", "MID", "UCASE", "LCASE", "IIF"
            }

            If Check(TokenType.Number) Then
                Dim token As BasicToken = Advance()
                Return New NumberLiteralNode() With {
                    .Value = Double.Parse(token.Value, CultureInfo.InvariantCulture),
                    .Line = token.Line,
                    .Column = token.Column
                }
            End If

            If Check(TokenType.String) Then
                Dim token As BasicToken = Advance()
                Return New StringLiteralNode With {
                    .Value = token.Value,
                    .Line = token.Line,
                    .Column = token.Column
                }
            End If

            If MatchKeyword("TRUE") Then
                Dim token As BasicToken = Previous()
                Return New BooleanLiteralNode With {
                    .Value = True,
                    .Line = token.Line,
                    .Column = token.Column
                }
            End If

            If MatchKeyword("FALSE") Then
                Dim token As BasicToken = Previous()
                Return New BooleanLiteralNode With {
                    .Value = False,
                    .Line = token.Line,
                    .Column = token.Column
                }
            End If

            If Check(TokenType.Identifier) Then
                Dim token As BasicToken = Advance()
                Dim name As String = token.Value.ToUpperInvariant()
                If Check(TokenType.Punctuation) AndAlso Peek().Value = "(" Then
                    Advance()
                    Dim arguments As New List(Of AstNode)()
                    If Not Check(TokenType.Punctuation) OrElse Peek().Value <> ")" Then
                        Do
                            arguments.Add(ParseExpression())
                        Loop While Check(TokenType.Punctuation) AndAlso Peek().Value = "," AndAlso Advance() IsNot Nothing
                    End If
                    ConsumePunctuation(")", "Expected ')' after arguments")

                    If builtinFunctions.Contains(name) Then
                        Return New BuiltinFunctionNode With {
                            .FunctionName = name,
                            .Arguments = arguments,
                            .Line = token.Line,
                            .Column = token.Column
                        }
                    Else
                        Return New FunctionCallNode With {
                            .FunctionName = name,
                            .Arguments = arguments,
                            .Line = token.Line,
                            .Column = token.Column
                        }
                    End If
                End If
                Return New VariableRefNode With {
                    .Name = name,
                    .Line = token.Line,
                    .Column = token.Column
                }
            End If

            If Check(TokenType.Punctuation) AndAlso Peek().Value = "(" Then
                Advance()
                Dim expr As AstNode = ParseExpression()
                ConsumePunctuation(")", "Expected ')' after expression")
                Return expr
            End If

            If IsAtEnd() Then
                Throw New InvalidOperationException($"Unexpected end of input at line {Peek().Line}")
            End If

            Throw New InvalidOperationException($"Unexpected token '{Peek().Value}' at line {Peek().Line}, column {Peek().Column}")
        End Function

        Private Function Peek() As BasicToken
            If _position >= _tokens.Count Then Return _tokens(_tokens.Count - 1)
            Return _tokens(_position)
        End Function

        Private Function Previous() As BasicToken
            Return _tokens(_position - 1)
        End Function

        Private Function IsAtEnd() As Boolean
            Return _position >= _tokens.Count OrElse _tokens(_position).Type = TokenType.EndOfLine
        End Function

        Private Function IsEndOfStatement() As Boolean
            If IsAtEnd() Then Return True
            Return _tokens(_position).Type = TokenType.Punctuation AndAlso _tokens(_position).Value = ":"
        End Function

        Private Function Check(type As TokenType) As Boolean
            If IsAtEnd() Then Return False
            Return Peek().Type = type
        End Function

        Private Function CheckOperator(op As String) As Boolean
            If IsAtEnd() Then Return False
            Return Peek().Type = TokenType.Operator AndAlso Peek().Value = op
        End Function

        Private Function MatchKeyword(keyword As String) As Boolean
            If Check(TokenType.Keyword) AndAlso String.Equals(Peek().Value, keyword, StringComparison.OrdinalIgnoreCase) Then
                Advance()
                Return True
            End If
            Return False
        End Function

        Private Function Advance() As BasicToken
            If Not IsAtEnd() Then _position += 1
            Return Previous()
        End Function

        Private Function Consume(type As TokenType, message As String) As BasicToken
            If Check(type) Then Return Advance()
            Throw New InvalidOperationException($"{message} at line {Peek().Line}, column {Peek().Column}")
        End Function

        Private Function ConsumeOperator(op As String, message As String) As BasicToken
            If CheckOperator(op) Then Return Advance()
            Throw New InvalidOperationException($"{message} at line {Peek().Line}, column {Peek().Column}")
        End Function

        Private Function ConsumePunctuation(punc As String, message As String) As BasicToken
            If Check(TokenType.Punctuation) AndAlso Peek().Value = punc Then Return Advance()
            Throw New InvalidOperationException($"{message} at line {Peek().Line}, column {Peek().Column}")
        End Function

        Private Sub EnsureEndOfStatement(message As String)
            If Not IsEndOfStatement() Then
                Throw New InvalidOperationException($"{message} at line {Peek().Line}, column {Peek().Column}")
            End If
        End Sub
    End Class
End Namespace