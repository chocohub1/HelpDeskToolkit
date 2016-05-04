using System;
using System.DirectoryServices;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace de.janhendrikpeters.helpdesk.library
{
    public static class ExtensionMethods
    {
        public static double ToMegabyte(this long x)
        {
            return x / 1024 / 1024;
        }

        public static DateTime GetLinkerTime(this Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                stream.Read(buffer, 0, 2048);

            var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        public static string GetParentOrgUnit(this DirectoryEntry directoryEntry, string parentSearchPattern)
        {
            string orgUnit = directoryEntry.Parent.Name;
            if (!Regex.IsMatch(directoryEntry.Parent.Name, parentSearchPattern, RegexOptions.IgnoreCase))
            {
                orgUnit = GetParentOrgUnit(directoryEntry.Parent, parentSearchPattern);
            }
            return orgUnit;
        }
    }
}
