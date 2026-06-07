using KCraft.Blocks;
using KCraft.World;

public sealed class WorldSaveData
{
  public string WorldName { get; set; } = "New World";
  public int Seed { get; set; } = 42;

  public float PlayerX { get; set; } = 8f;
  public float PlayerY { get; set; } = 80f;
  public float PlayerZ { get; set; } = -10f;

  public float CameraYaw { get; set; } = 0f;
  public float CameraPitch { get; set; } = 0f;

  public int GameMode { get; set; } = 0;

  // wichtig: speichert Tageszeit / WorldTime
  public long TotalTicks { get; set; } = 6000;

  // Inventory
  public Block[] InventorySlots { get; set; } =
    new Block[PlayerInventory.TotalSize];

  public int SelectedHotbarSlot { get; set; }

  public DateTime LastPlayed { get; set; } = DateTime.Now;
}