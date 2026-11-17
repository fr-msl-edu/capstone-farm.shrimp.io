using Capstone.Domain;
using Capstone.Production;
using Capstone.Resources;
using System;
using System.Collections.Generic;

namespace Capstone.Harvests;

public enum HarvestType { Partial = 1, Total = 2 }

public enum WeighedBy { Farm = 1, Buyer = 2 }

public enum DisposalReason { StaffOrHomeUse = 1, GivenAway = 2, SpoiledOrLost = 3, Other = 4 }

public class Harvest : FarmEntity
{
    public const int MaxNotesLength = 1000;

    public Guid BatchId { get; set; }
    public PondBatch Batch { get; set; }
    public DateTime HarvestedAtUtc { get; set; }
    public HarvestType HarvestType { get; set; }

        public string LotCode { get; set; }

        public Guid? BuyerId { get; set; }
    public Buyer Buyer { get; set; }

        public WeighedBy? WeighedBy { get; set; }

    public decimal TotalKg { get; set; }
    public int? Boxes { get; set; }

        public decimal? AbwG { get; set; }

        public decimal? AbwSampleWeightG { get; set; }

        public int? AbwSamplePieces { get; set; }

        public long? FinalCount { get; set; }

        public long? DeadAtHarvest { get; set; }

    public bool? SmoothShell { get; set; }
    public bool? IntactAppendages { get; set; }
    public bool? NoNecrosis { get; set; }
    public string QualityNotes { get; set; }

    public List<HarvestGradeLine> GradeLines { get; set; } = new();
    public List<HarvestDisposal> Disposals { get; set; } = new();

        public List<HarvestCrate> Crates { get; set; } = new();

        public static decimal AbwFromSample(decimal weightG, int pieces) =>
        Math.Round(weightG / pieces, 2, MidpointRounding.AwayFromZero);
}

public class HarvestCrate : FarmChildEntity
{
    public const int MaxCrates = 1000;

    public Guid HarvestId { get; set; }

        public int CrateNo { get; set; }

        public decimal Kg { get; set; }

        public System.Guid? SizeGradeId { get; set; }

        public System.Guid? ContainerTypeId { get; set; }

        public string ContainerName { get; set; }

        public decimal? TareKg { get; set; }

        public decimal? GrossKg { get; set; }

        public System.Guid? SaleId { get; set; }
}

public class HarvestGradeLine : FarmChildEntity
{
    public Guid HarvestId { get; set; }
    public Guid SizeGradeId { get; set; }
    public SizeGrade SizeGrade { get; set; }
    public decimal Kg { get; set; }
}

public class HarvestDisposal : FarmChildEntity
{
    public const int MaxNoteLength = 300;

    public Guid HarvestId { get; set; }
    public decimal Kg { get; set; }
    public DisposalReason Reason { get; set; }
    public string Note { get; set; }
}
