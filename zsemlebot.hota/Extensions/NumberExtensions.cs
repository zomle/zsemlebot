using System.Text;

namespace zsemlebot.hota.Extensions
{
    internal static class NumberExtensions
    {
        public static string ToHexString(this int num)
        {
            var sb = new StringBuilder(11);
            sb.Append((num & 0xff).ToString("X2")).Append(' ');
            sb.Append(((num >> 8) & 0xff).ToString("X2")).Append(' ');
            sb.Append(((num >> 16) & 0xff).ToString("X2")).Append(' ');
            sb.Append(((num >> 24) & 0xff).ToString("X2"));
            return sb.ToString();
        }

        public static string ToHexString(this short num)
        {
            var sb = new StringBuilder(5);
            sb.Append((num & 0xff).ToString("X2")).Append(' ');
            sb.Append(((num >> 8) & 0xff).ToString("X2"));
            return sb.ToString();
        }

        public static string ToHexString(this byte num)
        {
            return num.ToString("X2");
        }
    }
}
