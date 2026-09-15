using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Writes generated documentation files, leaving a file untouched when its content has not
/// changed. The generated docs are tracked in git, so an unconditional rewrite would show up as a
/// dirty working tree after every build.
/// </summary>
internal static partial class DocFileWriter
{
    #region Public methods

    /// <summary>
    /// Builds the stamp line that lets <see cref="WriteIfChanged"/> compare a document without
    /// being confused by the generation date printed beside it.
    /// </summary>
    /// <param name="content">
    /// Document body the stamp covers, normalized to LF. Must not itself contain the stamp.
    /// </param>
    /// <returns>An HTML comment line, invisible in rendered markdown.</returns>
    public static string FormatHashStamp(string content)
    {
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
        return $"<!-- content-hash: {hash} -->"; // Keep in sync with HashStampRx.
    }

    /// <summary>
    /// Converts CRLF and lone CR to LF so generated files are byte-identical across platforms.
    /// </summary>
    /// <param name="text">Text to normalize.</param>
    /// <returns>The text with every line ending reduced to a single LF.</returns>
    public static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n").Replace("\r", "\n");

    /// <summary>
    /// Writes <paramref name="content"/> only when it differs from the file already at
    /// <paramref name="path"/>, creating the file when it is absent.
    /// </summary>
    /// <param name="path">Destination file path.</param>
    /// <param name="content">Content to write, already normalized to LF.</param>
    /// <remarks>
    /// When both sides carry a hash stamp, that stamp alone decides: it covers the document body
    /// but not the generation date, which would otherwise force a rewrite once a day. Anything
    /// else falls back to a full text comparison.
    /// </remarks>
    public static void WriteIfChanged(string path, string content)
    {
        if (File.Exists(path) && IsUnchanged(File.ReadAllText(path), content))
            return;

        File.WriteAllText(path, content);
    }

    #endregion

    #region Private methods

    private static bool IsUnchanged(string existingRaw, string content)
    {
        string existing = NormalizeLineEndings(existingRaw);

        Match existingStamp = HashStampRx().Match(existing);
        Match contentStamp = HashStampRx().Match(content);

        if (existingStamp.Success && contentStamp.Success)
            return existingStamp.Groups[1].Value == contentStamp.Groups[1].Value;

        return existing == content;
    }

    [GeneratedRegex(@"^<!-- content-hash: ([0-9A-F]{64}) -->$", RegexOptions.Multiline)]
    private static partial Regex HashStampRx();

    #endregion
}
