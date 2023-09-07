using System.Text;

namespace zsemlebot.core.Extensions
{
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder(bytes.Length * 3);
            foreach (var b in bytes)
            {
                result.AppendFormat("{0:X2} ", b);
            }

            return result.ToString();
        }
    }
}
