using WildPay.Models.Entities;

namespace WildPay.Helpers
{
    public static class ManageBadRequests
    {
        /// <summary>
        /// Verifies if the group is not null,
        /// if the user is connected
        /// and if the user is part of the group
        /// </summary>
        /// <param name="group">type of Model Group</param>
        /// <param name="userId">type string, Id of the connected user</param>
        /// <returns>A boolean</returns>
        public static string CheckGroupRequest(Group group, string userId)
        {
            if (IsGroupNull(group)) return "Le groupe que vous cherchez est inexistant.";
            else if (IsUserIdNull(userId)) return "Vous n'êtes pas connecté.";
            else if (IsUserPartOfGroup(group, userId)) return "Vous ne pouvez pas accéder à ce groupe car vous n'en faites pas partie.";
            else return string.Empty;
        }

        // returns true if the group is null
        public static bool IsGroupNull(Group group)
        {
            return group == null;
        }

        public static bool IsUserIdNull(string userId)
        {
            return userId == null;
        }

        public static bool IsUserNull(ApplicationUser user)
        {
            return user == null;
        }

        public static bool IsUserPartOfGroup(Group group, string userId)
        {
            return group.ApplicationUsers.FirstOrDefault(u => u.Id == userId) == null;
        }
    }
}
