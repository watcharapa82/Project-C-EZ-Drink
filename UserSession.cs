using System;

namespace EZTicketProject
{
    public static class UserSession
    {
        public static int CurrentUserId { get; private set; }
        public static string CurrentUserName { get; private set; }
        public static string CurrentUserPhone { get; private set; }
        public static string CurrentUserRole { get; private set; } = "Guest";

        public static string CurrentUserImagePath { get; private set; }

        public static string UserPhone => CurrentUserPhone;
        public static string UserImagePath => CurrentUserImagePath;

        public static void SetSession(int userId, string userName, string userPhone, string userRole, string imagePath)
        {
            CurrentUserId = userId;
            CurrentUserName = userName;
            CurrentUserPhone = userPhone;
            CurrentUserRole = userRole;
            CurrentUserImagePath = imagePath;
        }

        public static void ClearSession()
        {
            CurrentUserId = 0;
            CurrentUserName = string.Empty;
            CurrentUserPhone = string.Empty;
            CurrentUserRole = "Guest";
            CurrentUserImagePath = null;
        }

        public static bool IsLoggedIn => CurrentUserId > 0;
        public static bool IsAdmin => CurrentUserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}
