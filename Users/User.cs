namespace ATON_APIQuest.Users;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Login { get; set; }
    public required string Password { get; set; }
    public required string Name { get; set; }
    public int Gender { get; set; } // 0 - female, 1 - male, 2 - unknown
    public DateTime? Birthday { get; set; }
    public bool Admin { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public required string CreatedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? RevokedOn { get; set; }
    public string? RevokedBy { get; set; }

    public bool IsActive => RevokedOn == null;
}
