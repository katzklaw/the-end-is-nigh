using HarmonyLib;
using Hazel;
using InnerNet;
using System;

namespace Ender;

// Host-only /endgame and /endmeeting commands, split out into their own tiny
// standalone mod. Core logic ported from ApeMV/AmongUsRevamped
// (Patches/ChatPatches.cs, GPL-3.0).
internal static class GameStateHelper
{
    public static bool IsInGame() =>
        AmongUsClient.Instance != null &&
        AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started;

    public static bool IsMeeting() => MeetingHud.Instance != null;
}

// Handles the HOST typing /eg, /endgame, /em, or /endmeeting themselves.
// Intercepts ChatController.SendChat, which only fires for messages the
// local client is about to send.
[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
public static class EndCommandPatch
{
    public static bool Prefix(ChatController __instance)
    {
        try
        {
            string text = __instance.freeChatField.textArea.text;

            if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("/"))
                return true;

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return true;

            if (text == "/eg" || text == "/endgame")
            {
                ClearChatField(__instance);

                if (GameStateHelper.IsInGame())
                {
                    MessageWriter writer = AmongUsClient.Instance.StartEndGame();
                    writer.Write((byte)GameOverReason.ImpostorDisconnect);
                    AmongUsClient.Instance.FinishEndGame(writer);
                }

                return false;
            }

            if (text == "/em" || text == "/endmeeting")
            {
                ClearChatField(__instance);

                if (GameStateHelper.IsInGame() && GameStateHelper.IsMeeting())
                    MeetingHud.Instance.RpcClose();

                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            EnderPlugin.Log.LogError("[Ender] EndCommandPatch failed: " + ex);
            return true;
        }
    }

    private static void ClearChatField(ChatController controller)
    {
        controller.freeChatField.textArea.Clear();
        controller.freeChatField.textArea.SetText(string.Empty);
    }
}

// Handles a MODERATOR (per BAN_DATA/ALLOWED/Allowed.txt) typing /em or
// /endmeeting on their own client. Their chat send goes out as a normal RPC
// like anyone else's — this runs on the HOST, watching incoming SendChat RPCs
// from other players (same technique BanListMod's ChatSpamPatch uses), and
// if the sender is a moderator, performs the real close and swallows the
// message so it never appears as chat text.
//
// Deliberately scoped to /em and /endmeeting only — /endgame stays host-only
// unless you want that opened up too.
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public static class RemoteEndMeetingCommandPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] int callId, [HarmonyArgument(1)] MessageReader reader)
    {
        try
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return true;

            if (callId != (int)RpcCalls.SendChat)
                return true;

            // The host's own command is already handled locally by EndCommandPatch.
            if (__instance == null || __instance == PlayerControl.LocalPlayer)
                return true;

            string text;
            MessageReader peekReader = MessageReader.Get(reader);
            try
            {
                text = peekReader.ReadString();
            }
            finally
            {
                peekReader.Recycle();
            }

            if (text != "/em" && text != "/endmeeting")
                return true;

            ClientData client = AmongUsClient.Instance.GetClient(__instance.OwnerId);

            if (client == null || !AllowedFileReader.IsModerator(client.FriendCode))
                return true; // not a moderator — let it go out as normal (visible) chat

            if (GameStateHelper.IsInGame() && GameStateHelper.IsMeeting())
                MeetingHud.Instance.RpcClose();

            return false; // suppress — don't broadcast the raw command text
        }
        catch (Exception ex)
        {
            EnderPlugin.Log.LogError("[Ender] RemoteEndMeetingCommandPatch failed: " + ex);
            return true;
        }
    }
}