using Microsoft.Extensions.Configuration;
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
        public static IConfigurationRoot Configuration;
        public static DateTime StartupDateTimeUtc = DateTime.UtcNow;

        // Instance data
        public static List<Models.SysConfig> SysConfigs = new List<Models.SysConfig>();
        public static List<Models.Operator> Operators = new List<Models.Operator>();
        public static Dictionary<string, Dictionary<string, Models.Translation.Entry>> Translations = 
            new Dictionary<string, Dictionary<string, Models.Translation.Entry>>();

        // [[ Cache ]]
        public static List<Utils.ImageHash> BannedImagesHash =
            new List<Utils.ImageHash>();

    }
}
