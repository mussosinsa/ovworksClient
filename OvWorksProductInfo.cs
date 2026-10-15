namespace csharpOvWorksClient_1._0._0
{
    internal static class OvWorksProductInfo
    {
        public const string ProductName = "OV-Works 2.4";
        public const string DetailVersion = "2.4.5.9";
        public const string ComponentName = "OV-Works Client 4.5.2-1.w11";

        public static bool DemoSecurityBootstrap
        {
            get
            {
                bool enabled;
                return bool.TryParse(
                    System.Configuration.ConfigurationManager.AppSettings["DemoSecurityBootstrap"],
                    out enabled) && enabled;
            }
        }
    }
}
