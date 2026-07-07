Imports System.IO
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Input
Imports MonoGame_BASIC_REPL.Controllers
Imports MonoGame_BASIC_REPL.Models
Imports MonoGame_BASIC_REPL.Views

Public NotInheritable Class AppMain
    Inherits Game

    Private ReadOnly _graphics As GraphicsDeviceManager

    Private _state As ReplState
    Private _replView As ReplView
    Private _editorView As EditorView
    Private _lexer As Lexer
    Private _parser As Parser
    Private _interpreter As Interpreter
    Private _previousScrollWheel As Integer
    Private _previousKeyboardState As KeyboardState

    Public Sub New()
        _graphics = New GraphicsDeviceManager(Me)
        Content.RootDirectory = "Content"
        IsMouseVisible = True
        _graphics.PreferredBackBufferWidth = 800
        _graphics.PreferredBackBufferHeight = 600
    End Sub

    Protected Overrides Sub Initialize()
        _state = New ReplState
        _replView = New ReplView
        _editorView = New EditorView
        _lexer = New Lexer
        _parser = New Parser
        _interpreter = New Interpreter(_state)
        _previousScrollWheel = Mouse.GetState().ScrollWheelValue
        _previousKeyboardState = Keyboard.GetState()
        AddHandler Window.TextInput, AddressOf OnTextInput

        With _state.DisplayLines
            .Add(New DisplayLine With {.Text = "Welcome to MonoGame BASIC REPL!", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "To define custom functions, use syntax `DEF FN name(params)=expr`.", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "To close the application, type `END` on this REPL screen.", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "", .IsOutput = False})
            .Add(New DisplayLine With {.Text = "Available commands as follows:", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  PRINT e1[, e2..]  - Print expressions (space-separated)", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  LET var=expr      - Assign value to variable", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  IF cond THEN stmt - Conditional execution", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  GOTO line         - Jump to line number", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  GOSUB line        - Call subroutine", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  RETURN            - Return from subroutine", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  LOAD file.bas     - Load program from file", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  SAVE file.bas     - Save program to file", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  EDITOR            - Open code editor", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  RUN               - Run loaded program", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  LIST CODE         - Show loaded program code", .IsOutput = True})
            .Add(New DisplayLine With {.Text = "  LIST API          - Show full language reference", .IsOutput = True})
        End With

        MyBase.Initialize()
    End Sub

    Protected Overrides Sub LoadContent()
        _replView.Initialize(GraphicsDevice, Content)
        _editorView.Initialize(GraphicsDevice, Content)
    End Sub

    Protected Overrides Sub Update(gameTime As GameTime)
        Dim currentKeyboard As KeyboardState = Keyboard.GetState()
        Dim deltaTime As Single = CSng(gameTime.ElapsedGameTime.TotalSeconds)

        If _state.Mode = AppMode.Repl Then
            UpdateReplMode(currentKeyboard, deltaTime)
        Else
            UpdateEditorMode(currentKeyboard, deltaTime)
        End If

        Dim currentScroll As Integer = Mouse.GetState().ScrollWheelValue
        If currentScroll <> _previousScrollWheel Then
            Dim delta As Integer = (currentScroll - _previousScrollWheel) \ 120
            If _state.Mode = AppMode.Repl Then
                _state.ScrollOffset = Math.Max(0, _state.ScrollOffset + delta)
            Else
                _state.EditorScrollOffset = Math.Max(0, _state.EditorScrollOffset + delta)
            End If
            _previousScrollWheel = currentScroll
        End If

        _replView.Update(gameTime)
        _editorView.Update(gameTime)

        _previousKeyboardState = currentKeyboard
        MyBase.Update(gameTime)
    End Sub

    Protected Overrides Sub Draw(gameTime As GameTime)
        If _state.Mode = AppMode.Repl Then
            _replView.Draw(_state, _graphics.GraphicsDevice.Viewport.Height)
        Else
            _editorView.Draw(_state, _graphics.GraphicsDevice.Viewport.Height)
        End If
        MyBase.Draw(gameTime)
    End Sub

    Private Sub UpdateReplMode(currentKeyboard As KeyboardState, deltaTime As Single)
        If IsKeyRepeat(currentKeyboard, Keys.Left, deltaTime) Then
            _state.CursorColumn = Math.Max(0, _state.CursorColumn - 1)
        End If
        If IsKeyRepeat(currentKeyboard, Keys.Right, deltaTime) Then
            _state.CursorColumn = Math.Min(_state.CurrentInput.Length, _state.CursorColumn + 1)
        End If
        If IsKeyRepeat(currentKeyboard, Keys.Home, deltaTime) Then
            _state.CursorColumn = 0
        End If
        If IsKeyRepeat(currentKeyboard, Keys.End, deltaTime) Then
            _state.CursorColumn = _state.CurrentInput.Length
        End If
        If IsKeyRepeat(currentKeyboard, Keys.Delete, deltaTime) Then
            If _state.CursorColumn < _state.CurrentInput.Length Then
                _state.CurrentInput = _state.CurrentInput.Remove(_state.CursorColumn, 1)
            End If
        End If
    End Sub

    Private Sub UpdateEditorMode(currentKeyboard As KeyboardState, deltaTime As Single)
        If IsNewKeyPress(currentKeyboard, Keys.Escape) Then
            _state.Mode = AppMode.Repl
            _state.DisplayLines.Add(New DisplayLine With {.Text = $"  Returned from editor ({_state.CodeCache.DisplayName})", .IsOutput = True})
            Exit Sub
        End If

        EnsureEditorBuffer()

        If IsKeyRepeat(currentKeyboard, Keys.Left, deltaTime) Then
            If _state.EditorCursorColumn > 0 Then
                _state.EditorCursorColumn -= 1
            ElseIf _state.EditorCursorRow > 0 Then
                _state.EditorCursorRow -= 1
                _state.EditorCursorColumn = _state.CodeCache.Lines(_state.EditorCursorRow).Length
            End If
            ClampEditorScroll()
        End If

        If IsKeyRepeat(currentKeyboard, Keys.Right, deltaTime) Then
            Dim currentLineLen As Integer = _state.CodeCache.Lines(_state.EditorCursorRow).Length
            If _state.EditorCursorColumn < currentLineLen Then
                _state.EditorCursorColumn += 1
            ElseIf _state.EditorCursorRow < _state.CodeCache.Lines.Count - 1 Then
                _state.EditorCursorRow += 1
                _state.EditorCursorColumn = 0
            End If
            ClampEditorScroll()
        End If

        If IsKeyRepeat(currentKeyboard, Keys.Up, deltaTime) Then
            If _state.EditorCursorRow > 0 Then
                _state.EditorCursorRow -= 1
                _state.EditorCursorColumn = Math.Min(_state.EditorCursorColumn, _state.CodeCache.Lines(_state.EditorCursorRow).Length)
            End If
            ClampEditorScroll()
        End If

        If IsKeyRepeat(currentKeyboard, Keys.Down, deltaTime) Then
            If _state.EditorCursorRow < _state.CodeCache.Lines.Count - 1 Then
                _state.EditorCursorRow += 1
                _state.EditorCursorColumn = Math.Min(_state.EditorCursorColumn, _state.CodeCache.Lines(_state.EditorCursorRow).Length)
            End If
            ClampEditorScroll()
        End If

        If IsKeyRepeat(currentKeyboard, Keys.Home, deltaTime) Then
            _state.EditorCursorColumn = 0
        End If

        If IsKeyRepeat(currentKeyboard, Keys.End, deltaTime) Then
            _state.EditorCursorColumn = _state.CodeCache.Lines(_state.EditorCursorRow).Length
        End If

        If IsKeyRepeat(currentKeyboard, Keys.Delete, deltaTime) Then
            Dim line As String = _state.CodeCache.Lines(_state.EditorCursorRow)
            If _state.EditorCursorColumn < line.Length Then
                _state.CodeCache.Lines(_state.EditorCursorRow) = line.Remove(_state.EditorCursorColumn, 1)
            ElseIf _state.EditorCursorRow < _state.CodeCache.Lines.Count - 1 Then
                Dim nextLine As String = _state.CodeCache.Lines(_state.EditorCursorRow + 1)
                _state.CodeCache.Lines(_state.EditorCursorRow) = line & nextLine
                _state.CodeCache.Lines.RemoveAt(_state.EditorCursorRow + 1)
            End If
        End If
    End Sub

    Private Sub OnTextInput(sender As Object, e As TextInputEventArgs)
        If _state.Mode = AppMode.Repl Then
            HandleReplTextInput(e)
        Else
            HandleEditorTextInput(e)
        End If
    End Sub

    Private Sub HandleReplTextInput(e As TextInputEventArgs)
        Select Case e.Key
            Case Keys.Enter
                ProcessInput()
            Case Keys.Back
                If _state.CursorColumn > 0 Then
                    _state.CurrentInput = _state.CurrentInput.Remove(_state.CursorColumn - 1, 1)
                    _state.CursorColumn -= 1
                End If
            Case Else
                If Not Char.IsControl(e.Character) Then
                    _state.CurrentInput = _state.CurrentInput.Insert(_state.CursorColumn, e.Character.ToString())
                    _state.CursorColumn += 1
                End If
        End Select
        _state.ScrollOffset = 0
    End Sub

    Private Sub HandleEditorTextInput(e As TextInputEventArgs)
        EnsureEditorBuffer()

        Select Case e.Key
            Case Keys.Enter
                Dim line As String = _state.CodeCache.Lines(_state.EditorCursorRow)
                Dim before As String = line.Substring(0, _state.EditorCursorColumn)
                Dim after As String = line.Substring(_state.EditorCursorColumn)
                _state.CodeCache.Lines(_state.EditorCursorRow) = before
                _state.CodeCache.Lines.Insert(_state.EditorCursorRow + 1, after)
                _state.EditorCursorRow += 1
                _state.EditorCursorColumn = 0
                ClampEditorScroll()

            Case Keys.Back
                If _state.EditorCursorColumn > 0 Then
                    Dim line As String = _state.CodeCache.Lines(_state.EditorCursorRow)
                    _state.CodeCache.Lines(_state.EditorCursorRow) = line.Remove(_state.EditorCursorColumn - 1, 1)
                    _state.EditorCursorColumn -= 1
                ElseIf _state.EditorCursorRow > 0 Then
                    Dim prevLine As String = _state.CodeCache.Lines(_state.EditorCursorRow - 1)
                    _state.EditorCursorColumn = prevLine.Length
                    _state.CodeCache.Lines(_state.EditorCursorRow - 1) = prevLine & _state.CodeCache.Lines(_state.EditorCursorRow)
                    _state.CodeCache.Lines.RemoveAt(_state.EditorCursorRow)
                    _state.EditorCursorRow -= 1
                End If
                ClampEditorScroll()

            Case Keys.Tab
                Dim line As String = _state.CodeCache.Lines(_state.EditorCursorRow)
                _state.CodeCache.Lines(_state.EditorCursorRow) = line.Insert(_state.EditorCursorColumn, "    ")
                _state.EditorCursorColumn += 4

            Case Else
                If Not Char.IsControl(e.Character) Then
                    Dim line As String = _state.CodeCache.Lines(_state.EditorCursorRow)
                    _state.CodeCache.Lines(_state.EditorCursorRow) = line.Insert(_state.EditorCursorColumn, e.Character.ToString())
                    _state.EditorCursorColumn += 1
                End If
        End Select
    End Sub

    Private Sub ProcessInput()
        Dim input As String = _state.CurrentInput.Trim()
        _state.DisplayLines.Add(New DisplayLine With {
            .Text = $"{_state.CurrentLine:D3}> {_state.CurrentInput}",
            .IsOutput = False
        })

        _state.CurrentInput = ""
        _state.CursorColumn = 0

        If input.Length = 0 Then
            _state.CurrentLine += 1
            Exit Sub
        End If

        Dim lowerInput = input.ToLowerInvariant()
        Dim tempOutput = ""
        Select Case lowerInput
            Case "editor"
                EnterEditor()
            Case "list"
                tempOutput = "Please use `LIST CODE` to view editor code or `LIST API` for help."
            Case "list code"
                tempOutput = _interpreter.ExecuteListCode()
            Case "list api"
                tempOutput = Interpreter.ExecuteListApi()
            Case "run"
                tempOutput = _interpreter.ExecuteRun()
            Case "end"
                [Exit]()
        End Select
        If tempOutput.Length > 0 Then
            For Each line In tempOutput.Split(vbLf, StringSplitOptions.RemoveEmptyEntries)
                _state.DisplayLines.Add(New DisplayLine With {.Text = line, .IsOutput = True})
            Next line
            _state.CurrentLine += 1
            Exit Sub
        End If

        If lowerInput.StartsWith("load ", StringComparison.OrdinalIgnoreCase) Then
            HandleLoad(input.Substring(5).Trim())
        ElseIf lowerInput.StartsWith("save ", StringComparison.OrdinalIgnoreCase) Then
            HandleSave(input.Substring(5).Trim())
        ElseIf Not {"editor", "end", "run"}.Contains(lowerInput) Then
            ExecuteBasic(input)
        End If
        _state.CurrentLine += 1
    End Sub

    Private Sub EnterEditor()
        _state.Mode = AppMode.Editor
        EnsureEditorBuffer()
        _state.EditorCursorRow = 0
        _state.EditorCursorColumn = 0
        _state.EditorScrollOffset = 0
    End Sub

    Private Sub EnsureEditorBuffer()
        If _state.CodeCache.Lines.Count = 0 Then _state.CodeCache.Lines.Add("")
        If _state.EditorCursorRow >= _state.CodeCache.Lines.Count Then
            _state.EditorCursorRow = _state.CodeCache.Lines.Count - 1
        End If
        If _state.EditorCursorRow < 0 Then _state.EditorCursorRow = 0
    End Sub

    Private Sub HandleLoad(fileName As String)
        If fileName.Length = 0 Then
            _state.DisplayLines.Add(New DisplayLine With {.Text = "Error: no filename specified", .IsOutput = True})
            Exit Sub
        End If

        If Not fileName.EndsWith(".bas", StringComparison.OrdinalIgnoreCase) Then fileName &= ".bas"

        Try
            If Not File.Exists(fileName) Then
                _state.DisplayLines.Add(New DisplayLine With {.Text = $"Error: file '{fileName}' not found", .IsOutput = True})
                Exit Sub
            End If
            _state.CodeCache.Lines.Clear()

            For Each line In File.ReadAllLines(fileName)
                Dim trimmedLine As String = line.TrimStart()
                If trimmedLine.Length > 0 AndAlso Char.IsDigit(trimmedLine(0)) Then
                    Dim spaceIndex As Integer = trimmedLine.IndexOf(" "c)
                    If spaceIndex > 0 Then
                        _state.CodeCache.Lines.Add(trimmedLine.Substring(spaceIndex + 1))
                    Else
                        _state.CodeCache.Lines.Add("")
                    End If
                Else
                    _state.CodeCache.Lines.Add(trimmedLine)
                End If
            Next line

            _state.CodeCache.FileName = fileName
            _state.DisplayLines.Add(New DisplayLine With {.Text = $"  Loaded {fileName} ({_state.CodeCache.Lines.Count} lines)", .IsOutput = True})
        Catch ex As Exception
            _state.DisplayLines.Add(New DisplayLine With {.Text = $"Error loading file: {ex.Message}", .IsOutput = True})
        End Try
    End Sub

    Private Sub HandleSave(fileName As String)
        If fileName.Length = 0 Then fileName = _state.CodeCache.FileName

        If fileName.Length = 0 Then
            _state.DisplayLines.Add(New DisplayLine With {.Text = "Error: no filename specified (use: save filename.bas)", .IsOutput = True})
            Exit Sub
        End If

        If Not fileName.EndsWith(".bas", StringComparison.OrdinalIgnoreCase) Then fileName &= ".bas"

        Try
            Dim linesWithNumbers As New List(Of String)
            For i As Integer = 0 To _state.CodeCache.Lines.Count - 1
                Dim lineNum = (i + 1) * 10
                linesWithNumbers.Add($"{lineNum} {_state.CodeCache.Lines(i)}")
            Next i

            File.WriteAllLines(fileName, linesWithNumbers.ToArray())
            _state.CodeCache.FileName = fileName
            _state.DisplayLines.Add(New DisplayLine With {.Text = $"  Saved {fileName} ({_state.CodeCache.Lines.Count} lines)", .IsOutput = True})
        Catch ex As Exception
            _state.DisplayLines.Add(New DisplayLine With {.Text = $"Error saving file: {ex.Message}", .IsOutput = True})
        End Try
    End Sub

    Private Sub ExecuteBasic(input As String)
        Try
            Dim tokens As List(Of BasicToken) = _lexer.Tokenize(input, _state.CurrentLine)
            Dim ast As List(Of AstNode) = _parser.Parse(tokens)
            Dim output As String = _interpreter.Execute(ast)

            If output.Length > 0 Then
                For Each line In output.Split(vbLf, StringSplitOptions.RemoveEmptyEntries)
                    _state.DisplayLines.Add(New DisplayLine With {.Text = line, .IsOutput = True})
                Next line
            End If
        Catch ex As Exception
            Dim errorMsg As String = ex.Message
            If _state.Mode = AppMode.Repl Then errorMsg = errorMsg.Replace("at line", "at <repl>")
            _state.DisplayLines.Add(New DisplayLine With {.Text = $"Error: {errorMsg}", .IsOutput = True})
        End Try
    End Sub

    Private Function IsNewKeyPress(current As KeyboardState, key As Keys) As Boolean
        Return current.IsKeyDown(key) AndAlso _previousKeyboardState.IsKeyUp(key)
    End Function

    Private Function IsKeyRepeat(current As KeyboardState, key As Keys, deltaTime As Single) As Boolean
        Static keyRepeatTimers As New Dictionary(Of Keys, Single)
        If current.IsKeyUp(key) Then
            keyRepeatTimers.Remove(key)
            Return False
        End If
        If _previousKeyboardState.IsKeyUp(key) Then
            keyRepeatTimers(key) = 0
            Return True
        End If

        Dim holdTime As Single, value As Single
        If keyRepeatTimers.TryGetValue(key, value) Then
            holdTime = value + deltaTime
        Else
            holdTime = deltaTime
        End If

        keyRepeatTimers(key) = holdTime

        If holdTime >= 0.4F Then
            keyRepeatTimers(key) = holdTime - 0.05F
            Return True
        End If

        Return False
    End Function

    Private Sub ClampEditorScroll()
        Dim maxScroll = Math.Max(0, _state.CodeCache.Lines.Count - 5)
        _state.EditorScrollOffset = Math.Min(_state.EditorScrollOffset, maxScroll)
    End Sub

    Friend Shared Sub Main()
        Using app As New AppMain
            app.Run()
        End Using
    End Sub
End Class