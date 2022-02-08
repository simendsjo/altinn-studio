using System;
using System.IO;
using System.Linq;

namespace LocalTest.Helpers
{
    /// <summary>
    /// Extensions to facilitate sanitization of string values
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Sanitize the input as a file name.
        /// Replaces Path.DirectorySeparatorPath with '_' and other invalid characters (according to Path.GetInvalidFileNameChars()) as '-'.
        /// </summary>
        /// <param name="input">The input variable to be sanitized</param>
        /// <param name="extension">Appends ".{extension}" to the result if non-null</param>
        /// <param name="throwExceptionOnInvalidCharacters">Throw exception instead of replacing invalid characters</param>
        /// <returns></returns>
        public static string AsFileName(this string input, string extension = null, bool throwExceptionOnInvalidCharacters = true)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            // DirectorySeparator is invalid in a filename, but many places want to use '_'
            // rather than the default '-'
            if (!throwExceptionOnInvalidCharacters)
            {
                input = input.Replace(Path.DirectorySeparatorChar, '_');
            }

            char[] illegalFileNameCharacters = Path.GetInvalidFileNameChars();
            if (throwExceptionOnInvalidCharacters)
            {
                if (illegalFileNameCharacters.Any(ic => input.Any(i => ic == i)))
                {
                    throw new ArgumentOutOfRangeException(nameof(input));
                }

                if (input == "..")
                {
                    throw new ArgumentOutOfRangeException(nameof(input));
                }

                return input;
            }

            if (input == "..")
            {
               return "-";
            }

            var sanitized = illegalFileNameCharacters.Aggregate(input, (current, c) => current.Replace(c, '-'));
            return extension != null
                ? $"{sanitized}.{extension}"
                : sanitized;
        }
    }
}
