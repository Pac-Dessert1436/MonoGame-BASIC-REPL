Imports Microsoft.Xna.Framework
Imports MonoGame_BASIC_REPL.Models

Namespace Views
    Public Class EditorView
        Inherits ViewBase

        Private _headerColor As Color = Color.Cyan
        Private _statusColor As Color = Color.Gray
        Private Const LINE_NUM_WIDTH As Single = 55

        Public Overrides Sub Draw(state As ReplState, viewportHeight As Integer)
            BeginDraw()

            Dim maxVisible As Integer = MaxVisibleLines(viewportHeight) - 2
            If maxVisible < 1 Then maxVisible = 1

            Dim y As Integer = MARGIN
            _spriteBatch.DrawString(_font, $" EDITOR - {state.CodeCache.DisplayName} | Arrows to move cursor; ESC to return to REPL", New Vector2(MARGIN, y), _headerColor)
            y += LINE_HEIGHT + 4

            _spriteBatch.Draw(_pixel, New Rectangle(MARGIN, y, _spriteBatch.GraphicsDevice.Viewport.Width - 2 * MARGIN, 1), Color.DarkGray)
            y += 4

            Dim totalLines = Math.Max(state.CodeCache.Lines.Count, 1)
            Dim startIndex = CalculateStartIndex(totalLines, maxVisible, state.EditorScrollOffset)
            Dim lineNumDigits = Math.Max(totalLines.ToString().Length, 4)

            For i As Integer = startIndex To state.CodeCache.Lines.Count - 1
                If (y + LINE_HEIGHT) > (viewportHeight - MARGIN - LINE_HEIGHT) Then Exit For

                Dim lineNum As String = ((i + 1) * 10).ToString().PadLeft(lineNumDigits)
                _spriteBatch.DrawString(_font, lineNum, New Vector2(MARGIN, y), _lineNumColor)

                Dim lineText As String = state.CodeCache.Lines(i)
                Dim textX As Single = MARGIN + LINE_NUM_WIDTH
                _spriteBatch.DrawString(_font, lineText, New Vector2(textX, y), _textColor)

                If i = state.EditorCursorRow Then
                    Dim beforeCursor = lineText.Substring(0, Math.Min(state.EditorCursorColumn, lineText.Length))
                    Dim cursorX = textX + _font.MeasureString(beforeCursor).X
                    DrawCursor(cursorX, y, _cursorColor)
                End If

                y += LINE_HEIGHT
            Next i

            If state.CodeCache.Lines.Count = 0 Then
                _spriteBatch.DrawString(_font, "  (empty)", New Vector2(MARGIN + LINE_NUM_WIDTH, y), _statusColor)
                DrawCursor(MARGIN + LINE_NUM_WIDTH, y, _cursorColor)
            End If

            y = viewportHeight - MARGIN - LINE_HEIGHT + 4
            _spriteBatch.Draw(_pixel, New Rectangle(MARGIN, y - 4, _spriteBatch.GraphicsDevice.Viewport.Width - 2 * MARGIN, 1), Color.DarkGray)
            _spriteBatch.DrawString(_font, $"  Row {state.EditorCursorRow + 1}, Col {state.EditorCursorColumn + 1}  |  Lines: {state.CodeCache.Lines.Count}", New Vector2(MARGIN, y), _statusColor)

            EndDraw()
        End Sub
    End Class
End Namespace