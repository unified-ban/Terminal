namespace Unifiedban.Terminal
{
    public class UserPrivileges
    {
        public bool CanManageChat { get; set; }
        public bool CanPostMessages { get; set; }
        public bool CanEditMessages { get; set; }
        public bool CanDeleteMessages { get; set; }
        public bool CanManageVoiceChats { get; set; }
        public bool CanRestrictMembers { get; set; }
        public bool CanPromoteMembers { get; set; }
        public bool CanChangeInfo { get; set; }
        public bool CanInviteUsers { get; set; }
        public bool CanPinMessages { get; set; }
    }
}