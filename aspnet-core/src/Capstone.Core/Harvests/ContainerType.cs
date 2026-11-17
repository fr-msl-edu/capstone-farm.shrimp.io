using Capstone.Domain;

namespace Capstone.Harvests;

public class ContainerType : FarmEntity
{
    public const int MaxNameLength = 40;

    public string Name { get; set; }

        public decimal? NominalKg { get; set; }

        public decimal? TareKg { get; set; }

    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
