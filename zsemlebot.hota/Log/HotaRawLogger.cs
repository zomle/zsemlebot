using System;
using System.Reflection.Metadata;
using System.Text;
using zsemlebot.core.Log;
using zsemlebot.hota.Extensions;
using zsemlebot.hota.Messages;

namespace zsemlebot.hota.Log
{
    public class HotaRawLogger : Logger
    {
        public static readonly HotaRawLogger Null = new HotaRawLogger();

        public HotaRawLogger(DateTime timestamp)
            : base($"hota_raw_{timestamp:yyyyMMdd_HHmmss}.bin")
        {
        }

        private HotaRawLogger()
            : base()
        {
        }
    }

    public class HotaPackageLogger : Logger
    {
        public static readonly HotaPackageLogger Null = new HotaPackageLogger();

        public HotaPackageLogger(DateTime timestamp)
            : base($"hota_packages_{timestamp:yyyyMMdd_HHmmss}.txt")
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
                WriteLine($" >  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
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

    public class HotaEventLogger : Logger
    {
        public static readonly HotaEventLogger Null = new HotaEventLogger();

        public HotaEventLogger(DateTime timestamp)
             : base($"hota_events_{timestamp:yyyyMMdd_HHmmss}.txt")
        {
        }

        private HotaEventLogger()
            : base()
        {
        }

        internal void LogEvent(short type, string summary, string? details = null)
        {
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} Type: {type.ToHexString()} ({summary,-14}); {details ?? string.Empty}");
        }
    }
}
