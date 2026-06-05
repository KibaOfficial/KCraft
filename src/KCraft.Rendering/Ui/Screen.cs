// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace KCraft.Rendering.Ui;

public abstract class Screen
{
  protected readonly TextRenderer Text;
  protected readonly List<Button> Buttons = [];
  protected readonly List<TextInput> TextInputs = [];

  protected Screen(TextRenderer text)
  {
    Text = text;
  }

  public abstract void Layout(Vector2 screen);
  public abstract void Draw(Vector2 screen, float mouseX, float mouseY);

  public virtual void Update(float deltaTime)
  {
    foreach (var input in TextInputs)
      input.Update(deltaTime);
  }

  public virtual void HandleClick(float mx, float my)
  {
    foreach (var input in TextInputs)
      input.HandleClick(mx, my);
    foreach (var btn in Buttons)
      if (btn.OnMouseClick(mx, my))
        btn.Click();
  }
  public virtual void HandleKeyDown(Keys key, bool shift)
  {
    foreach (var input in TextInputs)
      input.HandleKeyDown(key, shift);
  }

  public virtual void HandleTextInput(char c)
  {
    foreach (var input in TextInputs)
      input.HandleTextInput(c);
  }

  public virtual void HandleMouseMove(float mx, float my) { }
  public virtual void HandleMouseUp(float mx, float my) { }
}