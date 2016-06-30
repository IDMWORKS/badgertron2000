namespace BadgerApi
{
    public class CachingSettings
    {
        public bool CacheDynamicBadges { get; set; } = true;
        public string CacheDirectory { get; set; } = "images/cached-badges";
    }
}