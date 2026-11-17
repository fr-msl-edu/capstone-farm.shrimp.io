using Capstone.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Capstone.Resources;

public enum FryDeliveryStatus { Ordered = 1, InTransit = 2, Received = 3, Rejected = 4, Cancelled = 5 }

public static class FryDeliveryFlow
{
    public static IReadOnlyList<FryDeliveryStatus> Next(FryDeliveryStatus current) => current switch
    {
        FryDeliveryStatus.Ordered => new[] { FryDeliveryStatus.InTransit, FryDeliveryStatus.Received, FryDeliveryStatus.Cancelled },
        FryDeliveryStatus.InTransit => new[] { FryDeliveryStatus.Received, FryDeliveryStatus.Rejected },
        FryDeliveryStatus.Received => new[] { FryDeliveryStatus.Rejected },
        _ => Array.Empty<FryDeliveryStatus>(),
    };

    public static bool IsEnded(FryDeliveryStatus status) => status is FryDeliveryStatus.Rejected or FryDeliveryStatus.Cancelled;
}

public class FryDelivery : FarmEntity
{
    public const int MaxBatchNumberLength = 100;
    public const int MaxNotesLength = 500;
    public const long MaxPl = 10_000_000;
    public const int MaxPermitLength = 100;

    public Guid HatcheryId { get; set; }
    public HatcherySource Hatchery { get; set; }

    public string HatcheryBatchNumber { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public int? PlAgeDays { get; set; }

    public long? PlReceived { get; set; }

    public DateTime? ReceivedAtUtc { get; set; }

    public long? PlOrdered { get; set; }

    public decimal? CostPhp { get; set; }
    public string Notes { get; set; }

    public string PlHealthCertificateNo { get; set; }

    public string TransportPermitNo { get; set; }

    public Production.TransportMode? TransportMode { get; set; }

    public DateTime? TimePackedUtc { get; set; }

    public string SfscNo { get; set; }

    public DateTime? StockingFinishedAtUtc { get; set; }

    public long? StockingFinishedByUserId { get; set; }

    public bool IsStockingFinished => StockingFinishedAtUtc.HasValue;

    [Timestamp]
    public byte[] RowVersion { get; set; }
}

public class FryDeliveryStatusChange : FarmEntity
{
    public const int MaxNoteLength = 500;

    public Guid DeliveryId { get; set; }
    public FryDeliveryStatus Status { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public string Note { get; set; }
}

public enum ShortfallReason { DeadOnArrival = 1, Discarded = 2, Returned = 3, CountDifference = 4 }

public class FryDeliveryShortfall : FarmEntity
{
    public const int MaxNoteLength = 500;

    public Guid DeliveryId { get; set; }
    public ShortfallReason Reason { get; set; }
    public long Count { get; set; }
    public string Note { get; set; }
}
