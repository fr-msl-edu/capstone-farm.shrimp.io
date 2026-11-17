using Abp.Domain.Entities;
using System;

namespace Capstone.Farms;

public class FarmCodeChange : Entity<Guid>
{
    public const int MaxCodeLength = 64;
    public const int MaxNameLength = 200;

    public int TenantId { get; set; }

    public string OldCode { get; set; }

    public string NewCode { get; set; }

    public long? ChangedByUserId { get; set; }

    public string ChangedByName { get; set; }

    public DateTime ChangedAt { get; set; }
}
