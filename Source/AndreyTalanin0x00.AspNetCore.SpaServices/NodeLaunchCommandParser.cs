using System;

namespace AndreyTalanin0x00.AspNetCore.SpaServices;

internal static class NodeLaunchCommandParser
{
    private static readonly char[] s_commandSeparators = [' ', '\t'];
    private static readonly StringSplitOptions s_stringSplitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    public static void ParseSpaProxyLaunchCommand(string command, out string packageManagerCommand, out string script, out string[] scriptParameters)
    {
        // Sample Node.js package manager command:
        // npm start -- paramA=valueA paramB=valueB (command tokens)
        // 0   1     2  3             4             (command tokens' indexes)

        string[] commandTokens = command.Split(s_commandSeparators, s_stringSplitOptions);

        if (commandTokens.Length >= 2)
            (packageManagerCommand, script) = (commandTokens[0], commandTokens[1]);
        else
            throw new ArgumentException("The command does not represent a valid package manager command (not enough tokens for the \"npm start\" pattern).", nameof(command));

        scriptParameters = [];
        if (commandTokens.Length >= 3)
        {
            if (commandTokens[2] != "--")
                throw new ArgumentException("The command does not represent a valid package manager command (parameter separator is not present).", nameof(command));

            if (commandTokens.Length >= 4)
                scriptParameters = commandTokens[3..];
        }
    }
}
