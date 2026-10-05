using PharmaCore.Domain.Common;

namespace PharmaCore.Domain.Entities.Identity;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
