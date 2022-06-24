using System;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;

namespace PullLogger.Events;

/**
 * straight up work from MidoriKami:
 * https://github.com/MidoriKami/NoTankYou/blob/master/NoTankYou/System/EventManager.cs
 * https://xivlogs.github.io/nari/types/director.html
 * https://discord.com/channels/581875019861328007/653504487352303619/984859948003500032
 */
public sealed unsafe class EndViaDirectorUpdateHook : IDisposable
{
    [Signature("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC ?? 48 8B D9 49 8B F8 41 0F B7 08",
        DetourName = nameof(DutyEventFunction))]
    private readonly Hook<DutyEventDelegate>? _dutyEventHook = null;

    public EventHandler<EndEventArgs>? EndEvent;

    public EndViaDirectorUpdateHook()
    {
        SignatureHelper.Initialise(this);
        _dutyEventHook?.Enable();
    }

    public bool Available { get; private set; }

    public void Dispose()
    {
        _dutyEventHook?.Dispose();
    }

    private byte DutyEventFunction(void* a1, void* a2, ushort* a3)
    {
        try
        {
            var category = *a3;
            var type = *(uint*)(a3 + 4);

            // DirectorUpdate Category
            if (category == 0x6D)
            {
                // PluginLog.Debug("DirectorUpdate: " + type.ToString("X2"));
                switch (type)
                {
                    case 0x40000001: // initialized
                    case 0x40000006: // barrier down (after a wipe)
                    case 0x40000010: // fade-in (happens after a wipe, or when logging in into an instance)
                        Available = true;
                        break;
                    case 0x40000005: // party wipe
                        EndEvent?.Invoke(this, new EndEventArgs(false));
                        break;
                    case 0x40000003: // duty completed
                        EndEvent?.Invoke(this, new EndEventArgs(true));
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Failed to get duty event");
        }

        return _dutyEventHook!.Original(a1, a2, a3);
    }

    public void ResetAvailability() => Available = false;

    private delegate byte DutyEventDelegate(void* a1, void* a2, ushort* a3);
}