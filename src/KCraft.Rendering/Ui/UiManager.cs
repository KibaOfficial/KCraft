// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering.Ui;

public enum GameState { MainMenu, Playing, Paused, Options, NewWorld, SelectWorld }

public sealed class UiManager : IDisposable
{
    private readonly TextRenderer _text;
    public readonly MainMenuScreen MainMenu;
    public readonly PauseMenuScreen PauseMenu;
    public readonly OptionsScreen Options;
    public readonly NewWorldScreen NewWorld;
    public readonly SelectWorldScreen SelectWorld;

    public GameState State { get; private set; } = GameState.MainMenu;
    public GameState PreviousState { get; private set; } = GameState.MainMenu;

    private Vector2 _screen;
    private bool _hasLayout;
    private float _layoutScale = UiScale.Scale;

    public event Action<string, int?>? OnNewWorldCreate;
    public event Action<string>? OnWorldSelected;

    public UiManager(string fontPath)
    {
        _text = new TextRenderer(fontPath);
        MainMenu = new MainMenuScreen(_text);
        PauseMenu = new PauseMenuScreen(_text);
        Options = new OptionsScreen(_text);
        NewWorld = new NewWorldScreen(_text);
        SelectWorld = new SelectWorldScreen(_text); // ← ZUERST initialisieren!

        // MainMenu
        MainMenu.OnSingleplayer += () => { SelectWorld.RefreshWorlds(); SetState(GameState.SelectWorld); };
        MainMenu.OnQuit += () => Environment.Exit(0);
        MainMenu.OnOptions += () => OpenOptions();

        // PauseMenu
        PauseMenu.OnResume += () => SetState(GameState.Playing);
        PauseMenu.OnQuitToTitle += () => SetState(GameState.MainMenu);
        PauseMenu.OnOptions += () => OpenOptions();

        // Options
        Options.OnBack += () => SetState(PreviousState);

        // NewWorld
        NewWorld.OnCreate += (name, seed) => OnNewWorldCreate?.Invoke(name, seed);
        NewWorld.OnBack += () => { SelectWorld.RefreshWorlds(); SetState(GameState.SelectWorld); };

        // SelectWorld
        SelectWorld.OnPlay += (name) => OnWorldSelected?.Invoke(name);
        SelectWorld.OnCreate += () => SetState(GameState.NewWorld);
        SelectWorld.OnDelete += (name) => { WorldSaveManager.Delete(name); SelectWorld.RefreshWorlds(); };
        SelectWorld.OnBack += () => SetState(GameState.MainMenu);
    }

    public void SetState(GameState state)
    {
        Console.WriteLine($"[UI] State: {state}");
        State = state;
        Relayout();
    }

    private void OpenOptions()
    {
        PreviousState = State;
        SetState(GameState.Options);
    }

    public void Layout(Vector2 screen)
    {
        _screen = screen;
        _hasLayout = true;
        _layoutScale = UiScale.Scale;
        Relayout();
    }

    private void Relayout()
    {
        if (!_hasLayout) return;

        MainMenu.Layout(_screen);
        PauseMenu.Layout(_screen);
        Options.Layout(_screen);
        NewWorld.Layout(_screen);
        SelectWorld.Layout(_screen);
    }

    public void Draw(Vector2 screen, float mx, float my)
    {
        if (!_hasLayout || _screen != screen || Math.Abs(_layoutScale - UiScale.Scale) > 0.001f)
            Layout(screen);

        if (State == GameState.MainMenu) MainMenu.Draw(screen, mx, my);
        else if (State == GameState.Paused) PauseMenu.Draw(screen, mx, my);
        else if (State == GameState.Options) Options.Draw(screen, mx, my);
        else if (State == GameState.NewWorld) NewWorld.Draw(screen, mx, my);
        else if (State == GameState.SelectWorld) SelectWorld.Draw(screen, mx, my);
    }

    public void HandleClick(float mx, float my)
    {
        if (State == GameState.MainMenu) MainMenu.HandleClick(mx, my);
        else if (State == GameState.Paused) PauseMenu.HandleClick(mx, my);
        else if (State == GameState.Options) Options.HandleClick(mx, my);
        else if (State == GameState.NewWorld) NewWorld.HandleClick(mx, my);
        else if (State == GameState.SelectWorld) SelectWorld.HandleClick(mx, my);
    }

    public void HandleMouseMove(float mx, float my)
    {
        MainMenu.HandleMouseMove(mx, my);
        PauseMenu.HandleMouseMove(mx, my);
        Options.HandleMouseMove(mx, my);
        NewWorld.HandleMouseMove(mx, my);
        SelectWorld.HandleMouseMove(mx, my);
    }

    public void Update(float deltaTime)
    {
        if (State == GameState.MainMenu) MainMenu.Update(deltaTime);
        else if (State == GameState.Paused) PauseMenu.Update(deltaTime);
        else if (State == GameState.Options) Options.Update(deltaTime);
        else if (State == GameState.NewWorld) NewWorld.Update(deltaTime);
        else if (State == GameState.SelectWorld) SelectWorld.Update(deltaTime);
    }

    public void HandleKeyDown(Keys key, bool shift)
    {
        if (State == GameState.MainMenu) MainMenu.HandleKeyDown(key, shift);
        else if (State == GameState.Paused) PauseMenu.HandleKeyDown(key, shift);
        else if (State == GameState.Options) Options.HandleKeyDown(key, shift);
        else if (State == GameState.NewWorld) NewWorld.HandleKeyDown(key, shift);
        else if (State == GameState.SelectWorld) SelectWorld.HandleKeyDown(key, shift);
    }

    public void HandleTextInput(char c)
    {
        if (State == GameState.MainMenu) MainMenu.HandleTextInput(c);
        else if (State == GameState.Paused) PauseMenu.HandleTextInput(c);
        else if (State == GameState.Options) Options.HandleTextInput(c);
        else if (State == GameState.NewWorld) NewWorld.HandleTextInput(c);
        else if (State == GameState.SelectWorld) SelectWorld.HandleTextInput(c);
    }

    public void Dispose() => _text.Dispose();
}
