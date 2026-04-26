using System;
using System.Collections.Generic;
using System.Globalization;

namespace TheEvilWithinTrainer
{
    internal static class ByteHelper
    {
        internal static byte[] ParseBytes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new byte[0];
            }

            string[] parts = value.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<byte> bytes = new List<byte>(parts.Length);
            foreach (string part in parts)
            {
                bytes.Add(byte.Parse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            }

            return bytes.ToArray();
        }

        internal static int? ParseHexInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string normalized = value.Trim();
            if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(2);
            }

            return int.Parse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
    }
}

