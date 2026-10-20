namespace ClientManagement.Api.Infrastructure.Identity;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";

    public static readonly IReadOnlyList<string> All = [SuperAdmin, Admin, Manager, Staff];

    public static class Permissions
    {
        public const string ClientsView = "clients.view";
        public const string ClientsCreate = "clients.create";
        public const string ClientsEdit = "clients.edit";
        public const string ClientsDelete = "clients.delete";

        public const string FollowUpsManage = "followups.manage";
        public const string FollowUpsDelete = "followups.delete";
        public const string NotesManage = "notes.manage";
        public const string DashboardView = "dashboard.view";
        public const string UsersManage = "users.manage";
    }

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [SuperAdmin] = [
                Permissions.ClientsView,
                Permissions.ClientsCreate,
                Permissions.ClientsEdit,
                Permissions.ClientsDelete,
                Permissions.FollowUpsManage,
                Permissions.FollowUpsDelete,
                Permissions.NotesManage,
                Permissions.DashboardView,
                Permissions.UsersManage
            ],
            [Admin] = [
                Permissions.ClientsView,
                Permissions.ClientsCreate,
                Permissions.ClientsEdit,
                Permissions.ClientsDelete,
                Permissions.FollowUpsManage,
                Permissions.FollowUpsDelete,
                Permissions.NotesManage,
                Permissions.DashboardView,
                Permissions.UsersManage
            ],
            [Manager] = [
                Permissions.ClientsView,
                Permissions.ClientsCreate,
                Permissions.ClientsEdit,
                Permissions.FollowUpsManage,
                Permissions.FollowUpsDelete,
                Permissions.NotesManage,
                Permissions.DashboardView
            ],
            [Staff] = [
                Permissions.ClientsView,
                Permissions.ClientsCreate,
                Permissions.ClientsEdit,
                Permissions.FollowUpsManage,
                Permissions.NotesManage,
                Permissions.DashboardView
            ]
        };
}

