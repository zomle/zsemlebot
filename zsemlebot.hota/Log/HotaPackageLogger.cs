using System;
using System.Text;
using zsemlebot.core.Log;
using zsemlebot.hota.Extensions;

namespace zsemlebot.hota.Log
{
    public class HotaPackageLogger : TextLogger
    {
        public static readonly HotaPackageLogger Null = new HotaPackageLogger();

        public HotaPackageLogger(DateTime timestamp)
            : base($"hota_packages_{timestamp:yyyyMMdd_HHmmss}.txt", Encoding.ASCII)
        {
        }

        private HotaPackageLogger()
            : base()
        {
        }

        public void LogPackage(bool isIncoming, DataPackage package)
        {
            var packageString = CreatePackageString(package);

            if (isIncoming)
            {
                WriteLine($" < {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                WriteLine($" > {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            }

            WriteLine(packageString);
            WriteLine();
        }

        private static string CreatePackageString(DataPackage package)
        {
            if (package.SkipLogging)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Type: 0x{package.Type.ToHexString()}; Length: {package.ReadShort(0)} bytes; Content hidden.");
                sb.AppendLine();
                return sb.ToString();
            }
            else
            {
                var hexa = new StringBuilder();
                var ascii = new StringBuilder();

                var sb = new StringBuilder();
                sb.AppendLine($"Type: 0x{package.Type.ToHexString()}; Length: {package.ReadShort(0)} bytes");
                sb.AppendLine();

                int startIndex = 0;
                for (int i = 0; i < package.Content.Length; i++)
                {
                    var b = package.Content[i];
                    hexa.AppendFormat("{0:X2} ", b);
                    ascii.Append(b < 0x20 ? '.' : (char)b);

                    if ((i + 1) % 8 == 0)
                    {
                        hexa.Append(' ');
                        ascii.Append(' ');
                    }

                    if (i > 0 && (i + 1) % 16 == 0)
                    {
                        sb.AppendFormat("{0,5:X5}  {1,-52}{2,-17}", startIndex, hexa.ToString(), ascii.ToString());
                        sb.AppendLine();

                        startIndex += 16;

                        hexa.Clear();
                        ascii.Clear();
                    }
                }

                if (hexa.Length > 0)
                {
                    sb.AppendFormat("{0,5:X5}  {1,-52}{2,-17}", startIndex, hexa.ToString(), ascii.ToString());
                    sb.AppendLine();
                }

                return sb.ToString();
            }
        }
    }
}
