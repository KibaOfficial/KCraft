// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using KCraft.Blocks;

namespace KCraft.World;

public sealed class PlayerInventory
{
  public const int HotbarSize = 9;
  public const int InventorySize = 27; // 3 rows of 9 slots
  public const int TotalSize = HotbarSize + InventorySize;

  // Index 0-8 = Hotbar, 9-35 = Inventory
  private readonly Block[] _slots = new Block[TotalSize];

  public Block[] Hotbar => _slots[..HotbarSize];
  public Block[] Inventory => _slots[HotbarSize..];

  public Block GetSlot(int index)
  {
    if (index < 0 || index >= TotalSize) throw new ArgumentOutOfRangeException(nameof(index));
    return _slots[index];
  }
  public void SetSlot(int index, Block block)
  {
    if (index < 0 || index >= TotalSize) throw new ArgumentOutOfRangeException(nameof(index));
    _slots[index] = block;
  }

  public Block GetHotbar(int slot)
  {
    if (slot < 0 || slot >= HotbarSize) throw new ArgumentOutOfRangeException(nameof(slot));
    return _slots[slot];
  }
  public void SetHotbar(int slot, Block block)
  {
    if (slot < 0 || slot >= HotbarSize) throw new ArgumentOutOfRangeException(nameof(slot));
    _slots[slot] = block;
  }

  public Block GetInventory(int slot)
  {
    if (slot < 0 || slot >= InventorySize) throw new ArgumentOutOfRangeException(nameof(slot));
    return _slots[HotbarSize + slot];
  }
  public void SetInventory(int slot, Block block)
  {
    if (slot < 0 || slot >= InventorySize) throw new ArgumentOutOfRangeException(nameof(slot));
    _slots[HotbarSize + slot] = block;
  }

  // find first free slot in inventory
  public int FindFreeInventorySlot()
  {
    for (int i = 0; i < InventorySize; i++)
    {
      if (_slots[HotbarSize + i] == Block.Air) return i;
    }
    return -1; // no free slot
  }

  // fill default hotbar
  public void SetDefaultHotbar()
  {
    _slots[0] = Block.Grass;
    _slots[1] = Block.Dirt;
    _slots[2] = Block.Stone;
    _slots[3] = Block.OakLog;
    _slots[4] = Block.OakPlanks;
    _slots[5] = Block.Sand;
    _slots[6] = Block.Glass;
    _slots[7] = Block.OakStairs;
    _slots[8] = Block.StoneStairs;
  }

  public Block[] GetRawSlots()
  {
    return (Block[])_slots.Clone();
  }

  public void LoadRawSlots(Block[] slots)
  {
    Array.Copy(
        slots,
        _slots,
        Math.Min(slots.Length, _slots.Length));
  }

  public int SelectedHotbarSlot { get; set; } = 0;
}