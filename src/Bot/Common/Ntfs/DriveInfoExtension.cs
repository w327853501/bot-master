using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Common.Ntfs
{
    public static class DriveInfoExtension
    {
        public static IEnumerable<string> EnumerateFiles(this DriveInfo drive)
        {
            return (new MFTScanner()).EnumerateFiles(drive.Name);
        }
    }
}
