using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace HUI.Utilities
{
    public static class StringUtilities
    {
        public static readonly Regex RemoveSymbolsRegex = new Regex("[^a-zA-Z0-9 ']");
        public static readonly char[] SpaceCharArray = new char[] { ' ' };

        public static string InvariantToString(this object obj)
        {
            if (obj is float floatValue)
                return floatValue.ToString(CultureInfo.InvariantCulture);
            else if (obj is double doubleValue)
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            else
                return obj.ToString();
        }

        public static bool TryParseInvariantFloat(string s, out float floatValue)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue);
        }

        public static bool TryParseInvariantDouble(string s, out double doubleValue)
        {
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out doubleValue);
        }
        /// <summary>
        /// Get the Levenshtein edit distance between two strings.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <returns>The edit distance between the two provided strings.</returns>
        public static int LevenshteinDistance(string s1, string s2)
        {
            if (s1.Length == 0)
                return s2.Length;
            else if (s2.Length == 0)
                return s1.Length;

            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (int i = 0; i <= s2.Length; i++)
                d[0, i] = i;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int match = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + match);
                }
            }

            return d[s1.Length, s2.Length];
        }

        /// <summary>
        /// Get the similarity value of two strings using the Jaro-Winkler similarity.
        /// </summary>
        /// <param name="s1">The first string.</param>
        /// <param name="s2">The second string.</param>
        /// <param name="scalingFactor">A number signifying how much additional value is placed on a longer common prefix.</param>
        /// <param name="maxCommonPrefix">The longest length common prefix allowed to be used during scaling.</param>
        /// <returns>A number between 0 and 1 which indicates how similar the two provided strings are, where 1 represets the two strings being the same.</returns>
        public static float JaroWinklerSimilarity(string s1, string s2, float scalingFactor = 0.1f, int maxCommonPrefix = 4)
        {
            // adapted from: https://stackoverflow.com/a/19165108
            if (string.IsNullOrEmpty(s1))
                return string.IsNullOrEmpty(s2) ? 1f : 0f;

            // get number of matched characters (m)
            float m = 0;
            int matchDistance = Convert.ToInt32(Math.Floor(Math.Max(s1.Length, s2.Length) / 2f)) - 1;
            bool[] matchedCharacters1 = new bool[s1.Length];
            bool[] matchedCharacters2 = new bool[s2.Length];
            for (int i = 0; i < s1.Length; ++i)
            {
                int left = Math.Max(0, i - matchDistance);
                int right = Math.Min(s2.Length - 1, i + matchDistance);

                for (int j = left; j <= right; ++j)
                {
                    if (s1[i] != s2[j] || matchedCharacters2[j])
                        continue;

                    matchedCharacters1[i] = true;
                    matchedCharacters2[j] = true;
                    ++m;

                    break;
                }
            }

            if (m == 0)
                return 0;

            // get number of transpositions (t)
            float t = 0;
            for (int i = 0, j = 0; i < s1.Length; ++i)
            {
                if (!matchedCharacters1[i])
                    continue;

                while (!matchedCharacters2[j])
                    ++j;

                if (s1[i] != s2[j])
                    ++t;

                ++j;
            }
            t /= 2;

            float jaro = ((m / s1.Length) + (m / s2.Length) + ((m - t) / m)) / 3;

            // get length of common prefix (l)
            int l = 0;
            for (int i = 0; i < s1.Length && i < s2.Length && i < maxCommonPrefix; ++i)
            {
                if (s1[i] == s2[i])
                    ++l;
                else
                    break;
            }

            return jaro + l * scalingFactor * (1 - jaro);
        }

        /// <summary>
        /// Escape characters in a <see cref="StringBuilder"/> according to a provided mapping.
        /// </summary>
        /// <param name="sb"><see cref="StringBuilder"/> that holds a string with characters to escape.</param>
        /// <param name="escapeChar">The character that is used as a prefix to signify an escape sequence.</param>
        /// <param name="mapping">A mapping from the <see cref="char"/> that needs to be escaped to a <see cref="char"/> that will be used to signify the original character.</param>
        /// <returns>The <see cref="StringBuilder"/> itself.</returns>
        public static StringBuilder EscapeString(this StringBuilder sb, char escapeChar, Dictionary<char, char> mapping)
        {
            // escape the escape char
            // has to be done first
            sb.Replace(escapeChar.ToString(), $"{escapeChar}{escapeChar}");

            foreach (char toEscape in mapping.Keys)
                sb.Replace(toEscape.ToString(), $"{escapeChar}{mapping[toEscape]}");

            return sb;
        }

        /// <summary>
        /// Unescape characters in a <see cref="StringBuilder"/> according to a provided mapping.
        /// </summary>
        /// <param name="sb"><see cref="StringBuilder"/> that holds an escaped string that should be decoded to get the original string.</param>
        /// <param name="escapeChar">The character that is used as a prefix to signify an escape sequence.</param>
        /// <param name="mapping">A mapping from the <see cref="char"/> that needs to be escaped to a <see cref="char"/> that will be used to signify the original character.</param>
        /// <returns>The <see cref="StringBuilder"/> itself.</returns>
        public static StringBuilder UnescapeString(this StringBuilder sb, char escapeChar, Dictionary<char, char> mapping)
        {
            for (int i = 0; i < sb.Length; ++i)
            {
                if (sb[i] == escapeChar && (i + 1) < sb.Length)
                {
                    char escapeCharCode = sb[i + 1];

                    if (mapping.ContainsValue(escapeCharCode))
                    {
                        sb.Replace($"{escapeChar}{escapeCharCode}", mapping.First(kv => kv.Value == escapeCharCode).Key.ToString(), i, 2);
                    }
                    else if (escapeCharCode == escapeChar)
                    {
                        sb.Replace($"{escapeChar}{escapeChar}", escapeChar.ToString(), i, 2);
                    }
                    else
                    {
                        sb.Remove(i, 1);
                        --i;
                    }
                }
                else if (sb[i] == escapeChar)
                {
                    sb.Remove(i, 1);
                }
            }

            return sb;
        }

        public static StringBuilder ToLower(this StringBuilder sb)
        {
            for (int i = 0; i < sb.Length; ++i)
                sb[i] = char.ToLower(sb[i]);

            return sb;
        }

        /// <summary>
        /// Sanitize user input of TextMeshPro tags.
        /// </summary>
        /// <param name="s">A <see cref="string"/> containing user input.</param>
        /// <returns>Sanitized <see cref="string"/>.</returns>
        public static string EscapeTextMeshProTags(this string s)
        {
            return new StringBuilder(s).Replace("<", "<\u200B").Replace(">", "\u200B>").ToString();
        }
    }
}
