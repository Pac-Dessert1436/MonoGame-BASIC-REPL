Imports Microsoft.Xna.Framework
Imports MonoGame_BASIC_REPL.Models

Namespace Views
    Public Class ReplView
        Inherits ViewBase

        Private _promptColor As Color = Color.Cyan
        Private _outputColor As Color = Color.White

        Public Overrides Sub Draw(state As ReplState, viewportHeight As Integer)
            BeginDraw()

            Dim maxVisible = MaxVisibleLines(viewportHeight)
            Dim totalLines = state.DisplayLines.Count + 1
            Dim startIndex = CalculateStartIndex(totalLines, maxVisible, state.ScrollOffset)
            Dim y As Integer = MARGIN

            For i As Integer = startIndex To state.DisplayLines.Count - 1
                Dim line As DisplayLine = state.DisplayLines(i)
                If line.IsOutput Then
                    _spriteBatch.DrawString(_font, "  " & line.Text, New Vector2(MARGIN, y), _outputColor)
                Else
                    _spriteBatch.DrawString(_font, line.Text, New Vector2(MARGIN, y), _promptColor)
                End If
                y += LINE_HEIGHT
            Next

            Dim prompt As String = $"{state.CurrentLine:D3}> "
            _spriteBatch.DrawString(_font, prompt, New Vector2(MARGIN, y), _lineNumColor)

            Dim promptWidth As Single = _font.MeasureString(prompt).X
            Dim inputX As Single = MARGIN + promptWidth

            Dim beforeCursor As String = state.CurrentInput.Substring(0, state.CursorColumn)
            Dim afterCursor As String = state.CurrentInput.Substring(state.CursorColumn)

            _spriteBatch.DrawString(_font, beforeCursor, New Vector2(inputX, y), _textColor)
            Dim afterX As Single = inputX + _font.MeasureString(beforeCursor).X
            _spriteBatch.DrawString(_font, afterCursor, New Vector2(afterX, y), _textColor)

            DrawCursor(inputX + _font.MeasureString(beforeCursor).X, y, _cursorColor)
            EndDraw()
        End Sub
    End Class
End Namespace