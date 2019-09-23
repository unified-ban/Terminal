using System;
using System.Collections.Generic;
using System.Text;

namespace Unifiedban.Terminal
{
    public class CacheData
    {
        // [[ Program control ]]
        public static string LoggerName = "Unifiedban-Terminal";
        public static bool FatalError = false;

        // [[ Cache ]]
        public static List<Utils.ImageHash> BannedImagesHash =
            new List<Utils.ImageHash>();

    }
}
