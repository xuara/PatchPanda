using System.ComponentModel.DataAnnotations;

namespace PatchPanda.Web.Entities;

internal abstract class AbstractEntity
{
    [Key]
    internal int Id { get; set; } = 0;
}
