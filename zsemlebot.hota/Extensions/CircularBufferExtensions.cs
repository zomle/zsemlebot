using System;
using zsemlebot.networklib;

namespace zsemlebot.hota.Extensions
{
    internal static class CircularBufferExtensions
    {
        internal static bool TryReadPackage(this CircularByteBuffer buffer, out DataPackage? package)
        {
            bool lengthRead = buffer.TryPeekShort(out var nextPackageLength);
            if (!lengthRead)
            {
                package = null;
                return false;
            }

            if (!buffer.TryRead(nextPackageLength, out var packageContent))
            {
                package = null;
                return false;
            }

            package = new DataPackage(packageContent);
            return true;
        }
    }
}
