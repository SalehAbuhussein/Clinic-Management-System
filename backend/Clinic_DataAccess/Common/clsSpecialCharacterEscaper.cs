namespace Clinic_DataAccess.Common
{
    /// <summary>
    /// Builds the patterns the search filters hand to LIKE.
    ///
    /// A parameter stops the text becoming SQL, but not becoming a wildcard:
    /// %, _ and [ are operators inside the pattern itself, so a user typing %
    /// into a name box matched every row. Wrapping each one in brackets makes
    /// T-SQL read it as the character it is, which needs no ESCAPE clause.
    /// </summary>
    public class clsSpecialCharacterEscaper
    {
        /// <summary>
        /// Rows holding the text anywhere. A leading % rules out an index seek,
        /// which is the price of searching mid-string.
        /// </summary>
        /// <param name="value">What the user typed.</param>
        /// <returns>The pattern to send as the parameter.</returns>
        public static string Contains(string value) => $"%{_Escape(value.Trim())}%";

        /// <summary>
        /// Rows beginning with the text, which an index can still seek.
        /// </summary>
        /// <param name="value">What the user typed.</param>
        /// <returns>The pattern to send as the parameter.</returns>
        public static string StartsWith(string value) => $"{_Escape(value.Trim())}%";

        /// <summary>
        /// Turns every wildcard into its literal self.
        ///
        /// The bracket goes first: doing it later would re-escape the brackets
        /// the other two replacements add. A lone ] is already literal to
        /// T-SQL, so it is left alone.
        /// </summary>
        /// <param name="value">Text that may hold wildcards.</param>
        /// <returns>The same text, safe to splice into a pattern.</returns>
        private static string _Escape(string value) => value
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }
}
