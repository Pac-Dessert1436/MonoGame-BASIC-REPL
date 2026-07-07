Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Content
Imports Microsoft.Xna.Framework.Graphics
Imports MonoGame_BASIC_REPL.Models

Namespace Views
    Public MustInherit Class ViewBase
        Protected _spriteBatch As SpriteBatch
        Protected _font As SpriteFont
        Protected _pixel As Texture2D
        Protected _cursorBlinkTime As Single = 0
        Protected _cursorVisible As Boolean = True

        Protected _backgroundColor As Color = Color.Black
        Protected _lineNumColor As Color = Color.DarkCyan
        Protected _textColor As Color = Color.Aquamarine
        Protected _cursorColor As Color = Color.Aqua

        Protected Const MARGIN As Integer = 10
        Protected Const LINE_HEIGHT As Integer = 25

        Public Overridable Sub Initialize(graphicsDevice As GraphicsDevice, contentManager As ContentManager)
            _spriteBatch = New SpriteBatch(graphicsDevice)
            _font = contentManager.Load(Of SpriteFont)("Font")
            _pixel = New Texture2D(graphicsDevice, 1, 1)
            _pixel.SetData({Color.White})
        End Sub

        Public Overridable Sub Update(gameTime As GameTime)
            _cursorBlinkTime += CSng(gameTime.ElapsedGameTime.TotalSeconds)
            If _cursorBlinkTime >= 0.5F Then
                _cursorVisible = Not _cursorVisible
                _cursorBlinkTime = 0
            End If
        End Sub

        Public MustOverride Sub Draw(state As ReplState, viewportHeight As Integer)

        Protected Shared Function CalculateStartIndex(totalLines As Integer, maxVisible As Integer, scrollOffset As Integer) As Integer
            Return Math.Max(0, totalLines - maxVisible - scrollOffset)
        End Function

        Protected Sub DrawCursor(x As Single, y As Integer, color As Color)
            If _cursorVisible Then
                _spriteBatch.Draw(_pixel, New Rectangle(CInt(x), y, 2, LINE_HEIGHT), color)
            End If
        End Sub

        Protected Shared Function MaxVisibleLines(viewportHeight As Integer) As Integer
            Return (viewportHeight - 2 * MARGIN) \ LINE_HEIGHT
        End Function

        Protected Sub BeginDraw()
            _spriteBatch.Begin()
            _spriteBatch.GraphicsDevice.Clear(_backgroundColor)
        End Sub

        Protected Sub EndDraw()
            _spriteBatch.End()
        End Sub
    End Class
End Namespace