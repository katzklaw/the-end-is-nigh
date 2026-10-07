using HarmonyLib;
using System;
using UnityEngine;

namespace Ender;

// Vanilla only lets you open the chat panel during the lobby or a meeting.
// If a round gets stuck (freeze, no meeting available, etc.) there's
// normally no way in to type /endgame or /endmeeting. This adds a hidden
// hotkey that forces the chat panel open regardless of game state.
//
// Technique verified against TownofHost-Enhanced (0xDrMoe/TownofHost-Enhanced,
// GPL-3.0), Patches/ControlPatch.cs — HudManager.Instance.Chat.SetVisible(true)
// on a ControllerManager.Update postfix.
[HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.Update))]
public static class ForceChatOpenPatch
{
    // Shift + C + Enter, held together. Deliberately an obscure chord so it
    // can't trigger by accident during normal play/typing.
    private static readonly KeyCode[] Chord = { KeyCode.Return, KeyCode.C, KeyCode.LeftShift };

    public static void Postfix()
    {
        try
        {
            if (!AnyJustPressedAndAllHeld(Chord))
                return;

            if (HudManager.Instance == null || HudManager.Instance.Chat == null)
                return;

            HudManager.Instance.Chat.SetVisible(true);
        }
        catch (Exception ex)
        {
            EnderPlugin.Log.LogError("[Ender] ForceChatOpenPatch failed: " + ex);
        }
    }

    // True the frame at least one of the chord keys is newly pressed while
    // every key in the chord is currently held down.
    private static bool AnyJustPressedAndAllHeld(KeyCode[] keys)
    {
        bool anyJustPressed = false;

        foreach (KeyCode key in keys)
        {
            if (Input.GetKeyDown(key))
                anyJustPressed = true;

            if (!Input.GetKey(key))
                return false;
        }

        return anyJustPressed;
    }
}