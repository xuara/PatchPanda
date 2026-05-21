namespace PatchPanda.Web.Entities;

internal class AppSetting
{
    [Key]
    [MaxLength(64)]
    internal required string Key { get; init; }

    [MaxLength(64)]
    internal required string Value { get; set; }
}
