using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pORM.Tests.Models;

[Table("sample_table")]
public class TestEntity
{
    [Key]
    public int Id { get; set; }
    
    public string? Name { get; set; }
    
    [NotMapped]
    public string Ignored { get; set; } = string.Empty;
}

public class TestEntityProjection
{
    public string? DisplayName { get; set; }
}

[Table("related_entities")]
public class RelatedEntity
{
    [Key]
    public int Id { get; set; }

    public string? Description { get; set; }
}

public class JoinProjection
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
