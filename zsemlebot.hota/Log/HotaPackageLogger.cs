﻿using System;
using System.Text;
using zsemlebot.core.Extensions;
using zsemlebot.core.Log;

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

		public void LogPartialPackage(DataPackage package, int startIndex, int length)
		{
			WriteLine($" P {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
			var packageString = CreatePartialPackageString(package, startIndex, length);
			WriteLine(packageString);
			WriteLine();
		}

        public void LogPackage(bool isIncoming, DataPackage package, bool isHandled)
        {
            if (isIncoming)
            {
                WriteLine($" < {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            }
            else
            {
                WriteLine($" > {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            }

            var packageString = CreatePackageString(package, isHandled);
            WriteLine(packageString);
            WriteLine();
        }

		public static string CreatePartialPackageString(DataPackage package, int dataStartIndex, int length)
		{
			var hexa = new StringBuilder(2000);
			var ascii = new StringBuilder(2000);

			var sb = new StringBuilder();
			sb.AppendLine($"Type: 0x{package.Type.ToHexString()}; Length: {length} bytes; partial");
			sb.AppendLine();
			var dataEndIndex = dataStartIndex + length;

			var partialPackage = package.Content[dataStartIndex..dataEndIndex];
			sb.AppendLine(partialPackage.ToHexString());
			sb.AppendLine();
			int startIndex = 0;
			for (int i = 0; i < partialPackage.Length; i++)
			{
				var b = partialPackage[i];
				hexa.AppendFormat("{0:X2} ", b);
				var c = (char)b;
				ascii.Append(char.IsControl(c) ? '.' : c);

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

        public static string CreatePackageString(DataPackage package, bool isHandled)
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
                var hexa = new StringBuilder(2000);
                var ascii = new StringBuilder(2000);

                var sb = new StringBuilder();
                sb.AppendLine($"Type: 0x{package.Type.ToHexString()}; Length: {package.ReadShort(0)} bytes; {(isHandled ? "handled" : "not handled")}");
                sb.AppendLine();
                sb.AppendLine(package.Content.ToHexString());
                sb.AppendLine();
                int startIndex = 0;
                for (int i = 0; i < package.Content.Length; i++)
                {
                    var b = package.Content[i];
                    hexa.AppendFormat("{0:X2} ", b);
                    var c = (char)b;
                    ascii.Append(char.IsControl(c) ? '.' : c);

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
