// Copyright (c) 2026 KibaOfficial
// All rights reserved.

using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace KCraft.Core;

public sealed class DiscordRpc : IDisposable
{
  private const string ClientId = "1512487740489596938";
  private NamedPipeClientStream? _pipe;
  private bool _connected = false;
  private long _startTime;
  private int _nonce = 1;

  public bool IsConnected => _connected;

  public void Connect()
  {
    try
    {
      for (int i = 0; i < 10; i++)
      {
        try
        {
          _pipe = new NamedPipeClientStream(".", $"discord-ipc-{i}",
              PipeDirection.InOut, PipeOptions.Asynchronous);
          _pipe.Connect(1000);
          break;
        }
        catch
        {
          _pipe?.Dispose();
          _pipe = null;
        }
      }

      if (_pipe == null || !_pipe.IsConnected) return;

      // Handshake
      var handshake = JsonSerializer.Serialize(new
      {
        v = 1,
        client_id = ClientId
      });
      WriteFrame(0, handshake); // OpCode 0 = Handshake

      // Response lesen
      ReadFrame();

      _connected = true;
      _startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

      Console.WriteLine("[Discord RPC] Connected!");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Discord RPC] Failed to connect: {ex.Message}");
      _connected = false;
    }
  }

  public void SetActivity(string state, string details)
  {
    if (!_connected || _pipe == null) return;

    try
    {
      var payload = new
      {
        cmd = "SET_ACTIVITY",
        args = new
        {
          pid = Environment.ProcessId,
          activity = new
          {
            state,
            details,
            timestamps = new
            {
              start = _startTime
            },
            // assets = new
            // {
            //   large_image = "kcraft_logo",
            //   large_text = "KCraft",
            // }
          }
        },
        nonce = (_nonce++).ToString()
      };

      var json = JsonSerializer.Serialize(payload);
      WriteFrame(1, json); // OpCode 1 = Frame
      ReadFrame(); // ACK lesen
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Discord RPC] SetActivity failed: {ex.Message}");
      _connected = false;
    }
  }

  private void WriteFrame(int opCode, string json)
  {
    var data = Encoding.UTF8.GetBytes(json);
    var frame = new byte[8 + data.Length];

    // OpCode (4 bytes LE)
    BitConverter.TryWriteBytes(frame.AsSpan(0, 4), opCode);
    // Length (4 bytes LE)
    BitConverter.TryWriteBytes(frame.AsSpan(4, 4), data.Length);
    // Payload
    data.CopyTo(frame, 8);

    _pipe!.Write(frame);
    _pipe.Flush();
  }

  private string ReadFrame()
  {
    var header = new byte[8];
    _pipe!.ReadExactly(header);

    int length = BitConverter.ToInt32(header, 4);
    var data = new byte[length];
    _pipe.ReadExactly(data);

    return Encoding.UTF8.GetString(data);
  }

  public void Dispose()
  {
    _connected = false;
    _pipe?.Dispose();
  }
}