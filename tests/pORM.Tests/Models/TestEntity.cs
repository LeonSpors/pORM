using System.ComponentModel.DataAnnotations.Schema;

namespace pORM.Tests.Models;

[Table("TestTable")]
public class TestEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }

    public string Name { get; set; }
}