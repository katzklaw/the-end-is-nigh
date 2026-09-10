using System;
using System.IO;

namespace Ender;

// Lightweight, read-only mirror of BanListMod's AllowedManager — just enough
// to check moderator status from the shared BAN_DATA/ALLOWED/Allowed.txt file.
// Kept as a small standalone duplicate rather than a project reference so
// Ender stays a fully independent, drop-in plugin (doesn't need BanListMod
// installed to build or run).
//
// File format (one entry per line): FriendCode,PlayerName,Role1|Role2
internal static class AllowedFileReader
{
    private const string AllowedFilePath = "./BAN_DATA/ALLOWED/Allowed.txt";

    // Same ModCreator carve-out as AllowedManager — always treated as moderator.
    private const string ModCreatorFriendCode = "medialteam#6599";

    public static bool IsModerator(string friendCode)
    {
        if (string.IsNullOrWhiteSpace(friendCode))
            return false;

        if (friendCode.Equals(ModCreatorFriendCode, StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            if (!File.Exists(AllowedFilePath))
                return false;

            foreach (string rawLine in File.ReadAllLines(AllowedFilePath))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                string[] parts = rawLine.Split(',');
                if (parts.Length < 3)
                    continue;

                string code = parts[0].Trim();

                if (!code.Equals(friendCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                string rolesRaw = parts[2].Trim();

                foreach (string role in rolesRaw.Split('|', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (role.Trim().Equals("Moderator", StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false; // matched the entry but no Moderator role
            }
        }
        catch (Exception ex)
        {
            EnderPlugin.Log.LogError("[Ender] AllowedFileReader.IsModerator failed: " + ex);
        }

        return false;
    }
}