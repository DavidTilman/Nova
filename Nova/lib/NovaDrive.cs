using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.lib;
internal class NovaDrive
{
    public static bool DriveConnected => DriveInfo.GetDrives().Any(d => d.Name == "N:\\" && d.DriveType == DriveType.Removable && d.VolumeLabel == "NOVA");
}
