Imports System.Text
Imports MonoGame_BASIC_REPL.Models

Namespace Controllers
    Public NotInheritable Class Lexer
        Private ReadOnly _keywords As New HashSet(Of String) From {
            "PRINT", "LET", "IF", "THEN", "ELSE", "END", "FOR", "TO", "STEP", "NEXT",
            "GOTO", "GOSUB", "DEF", "FN", "TRUE", "FALSE", "MOD", "RETURN"
        }
        Private ReadOnly _operators As New HashSet(Of Char) From {"+"c, "-"c, "*"c, "/"c, "="c, "<"c, ">"c, "&"c}
        Private ReadOnly _punctuation As New HashSet(Of Char) From {","c, ";"c, "("c, ")"c, ":"c}

        Public Function Tokenize(input As String, line As Integer) As List(Of BasicToken)
            Dim tokens As New List(Of BasicToken)
            Dim position As Integer = 0
            Dim length As Integer = input.Length

            While position < length
                Dim c As Char = input(position)

                If Char.IsWhiteSpace(c) Then
                    position += 1
                ElseIf Char.IsDigit(c) OrElse (c = "."c AndAlso position + 1 < length AndAlso Char.IsDigit(input(position + 1))) Then
                    Dim start As Integer = position
                    Dim token As BasicToken = TokenizeNumber(input, start, line)
                    tokens.Add(token)
                    position = start
                ElseIf c = """"c Then
                    Dim start As Integer = position
                    Dim token As BasicToken = TokenizeString(input, start, line)
                    tokens.Add(token)
                    position = start
                ElseIf Char.IsLetter(c) OrElse c = "_"c Then
                    Dim start As Integer = position
                    Dim token As BasicToken = TokenizeIdentifierOrKeyword(input, start, line)
                    tokens.Add(token)
                    position = start
                ElseIf _operators.Contains(c) Then
                    Dim op As String = c.ToString()
                    If (c = "<"c OrElse c = ">"c) AndAlso position + 1 < length AndAlso input(position + 1) = "="c Then
                        op = c & "="
                        position += 2
                    Else
                        position += 1
                    End If
                    tokens.Add(New BasicToken(TokenType.Operator, op, line, position - op.Length + 1))
                ElseIf _punctuation.Contains(c) Then
                    tokens.Add(New BasicToken(TokenType.Punctuation, c.ToString(), line, position + 1))
                    position += 1
                Else
                    Throw New InvalidOperationException($"Unexpected character '{c}' at line {line}, column {position + 1}")
                End If
            End While

            tokens.Add(New BasicToken(TokenType.EndOfLine, "", line, position + 1))
            Return tokens
        End Function

        Private Shared Function TokenizeNumber(ByRef input As String, ByRef position As Integer, line As Integer) As BasicToken
            Dim startColumn As Integer = position + 1
            Dim valBuilder As New StringBuilder
            Dim hasDecimal As Boolean = False

            Do While position < input.Length
                Dim c As Char = input(position)
                If c = "."c AndAlso Not hasDecimal Then
                    hasDecimal = True
                    valBuilder.Append(c)
                    position += 1
                ElseIf Char.IsDigit(c) Then
                    valBuilder.Append(c)
                    position += 1
                Else
                    Exit Do
                End If
            Loop

            Return New BasicToken(TokenType.Number, valBuilder.ToString(), line, startColumn)
        End Function

        Private Shared Function TokenizeString(ByRef input As String, ByRef position As Integer, line As Integer) As BasicToken
            Dim startCol As Integer = position + 1
            Dim valBuilder As New StringBuilder

            position += 1
            Do While position < input.Length
                Dim c As Char = input(position)
                If c = """"c Then
                    position += 1
                    Exit Do
                End If
                valBuilder.Append(c)
                position += 1
            Loop

            Return New BasicToken(TokenType.String, valBuilder.ToString(), line, startCol)
        End Function

        Private Function TokenizeIdentifierOrKeyword(ByRef input As String, ByRef position As Integer, line As Integer) As BasicToken
            Dim startCol As Integer = position + 1
            Dim valBuilder As New StringBuilder

            Do While position < input.Length
                Dim c As Char = input(position)
                If Char.IsLetterOrDigit(c) OrElse c = "_"c Then
                    valBuilder.Append(c)
                    position += 1
                Else
                    Exit Do
                End If
            Loop

            Dim value = valBuilder.ToString()
            Dim token = If(
                _keywords.Contains(value.ToUpperInvariant()),
                TokenType.Keyword,
                TokenType.Identifier
            )
            Return New BasicToken(token, value, line, startCol)
        End Function
    End Class
End Namespace