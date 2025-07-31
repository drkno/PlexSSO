using PlexSSO.Model.Types;

namespace PlexSSO.Extensions
{
    public static class AccessTierExtensions
    {
        public static bool HasLessAccessThan(this AccessTier currentAccess, AccessTier? accessTier)
        {
            if (accessTier == null || accessTier == AccessTier.Failure)
            {
                return false;
            }
            if (currentAccess == AccessTier.Failure)
            {
                return false;
            }
            return currentAccess > accessTier;
        }

        public static bool HasMoreAccessThan(this AccessTier currentAccess, AccessTier? accessTier)
        {
            if (accessTier == null || accessTier == AccessTier.Failure)
            {
                return false;
            }
            if (currentAccess == AccessTier.Failure)
            {
                return true;
            }
            return currentAccess < accessTier;
        }
    }
}
