
using Microsoft.AspNetCore.Identity;

namespace ClientManagement.Api.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public bool MustChangePassword { get; set; } = false;
}
