// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public enum GameState { MainMenu, Playing, Paused }

public sealed class UiManager : IDisposable
{
    private readonly TextRenderer _text;
    public readonly MainMenuScreen MainMenu;
    public readonly PauseMenuScreen PauseMenu;

    public GameState State { get; private set; } = GameState.MainMenu;

    public UiManager(string fontPath)
    {
        _text    = new TextRenderer(fontPath);
        MainMenu = new MainMenuScreen(_text);
        PauseMenu = new PauseMenuScreen(_text);

        MainMenu.OnSingleplayer += () => SetState(GameState.Playing);
        MainMenu.OnQuit         += () => Environment.Exit(0);
        PauseMenu.OnResume      += () => SetState(GameState.Playing);
        PauseMenu.OnQuitToTitle += () => SetState(GameState.MainMenu);
    }

    public void SetState(GameState state) => State = state;

    public void Layout(Vector2 screen)
    {
        MainMenu.Layout(screen);
        PauseMenu.Layout(screen);
    }

    public void Draw(Vector2 screen, float mx, float my)
    {
        if (State == GameState.MainMenu)
            MainMenu.Draw(screen, mx, my);
        else if (State == GameState.Paused)
            PauseMenu.Draw(screen, mx, my);
    }

    public void HandleClick(float mx, float my)
    {
        if (State == GameState.MainMenu)
            MainMenu.HandleClick(mx, my);
        else if (State == GameState.Paused)
            PauseMenu.HandleClick(mx, my);
    }

    public void HandleMouseMove(float mx, float my)
    {
        MainMenu.HandleMouseMove(mx, my);
        PauseMenu.HandleMouseMove(mx, my);
    }

    public void Dispose() => _text.Dispose();
}