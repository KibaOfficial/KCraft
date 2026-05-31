// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using OpenTK.Mathematics;

namespace KCraft.Rendering.Ui;

public abstract class Screen
{
  protected readonly TextRenderer Text;
  protected readonly List<Button> Buttons = [];

  protected Screen(TextRenderer text)
  {
    Text = text;
  }

  public abstract void Layout(Vector2 screen);
  public abstract void Draw(Vector2 screen, float mouseX, float mouseY);

  public void HandleClick(float mx, float my)
  {
    foreach (var btn in Buttons)
      if (btn.OnMouseClick(mx, my))
        btn.Click();
  }

  public virtual void HandleMouseMove(float mx, float my) { }
}