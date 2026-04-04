using System;
using System.Collections.Generic;

namespace Crysis2RemasteredTrainer
{
    internal static class PatternScanner
    {
        internal static int Find(byte[] data, string pattern)
        {
            PatternToken[] tokens = ParsePattern(pattern);
            if (tokens.Length == 0 || data.Length < tokens.Length)
            {
                return -1;
            }

            int last = data.Length - tokens.Length;
            for (int i = 0; i <= last; i++)
            {
                bool matched = true;
                for (int j = 0; j < tokens.Length; j++)
                {
                    PatternToken token = tokens[j];
                    if (!token.IsWildcard && data[i + j] != token.Value)
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched)
                {
                    return i;
                }
            }

            return -1;
        }

        private static PatternToken[] ParsePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return new PatternToken[0];
            }

            string[] parts = pattern.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<PatternToken> tokens = new List<PatternToken>(parts.Length);
            foreach (string part in parts)
            {
                if (part == "?" || part == "??")
                {
                    tokens.Add(new PatternToken(true, 0));
                }
                else
                {
                    tokens.Add(new PatternToken(false, Convert.ToByte(part, 16)));
                }
            }

            return tokens.ToArray();
        }

        private struct PatternToken
        {
            internal PatternToken(bool isWildcard, byte value)
            {
                IsWildcard = isWildcard;
                Value = value;
            }

            internal bool IsWildcard;
            internal byte Value;
        }
    }
}
