namespace TFELibrary.Data;

public class UserInterest
{
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
    public string UserProfileId { get; set; } = string.Empty;
    public UserProfile UserProfile { get; set; } = null!;
}
