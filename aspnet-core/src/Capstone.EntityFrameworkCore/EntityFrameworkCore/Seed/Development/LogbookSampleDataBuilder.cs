using Capstone.Authorization.Roles;
using Capstone.Domain;
using Capstone.Harvests;
using Capstone.Inventory;
using Capstone.Ponds;
using Capstone.Production;
using Capstone.ReferenceData;
using Capstone.Resources;
using Capstone.Sales;
using Capstone.EntityFrameworkCore.Seed.Farms;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Capstone.EntityFrameworkCore.Seed.Development;

public class LogbookSampleDataBuilder
{
    public const string FarmCode = "logbook-farm";
    public const string OwnerUserName = "iabi";

    private static readonly DateOnly Stocking1 = new(2026, 3, 18);
    private static readonly DateOnly Harvest1 = new(2026, 6, 13);
    private static readonly DateOnly Stocking2 = new(2026, 9, 26);

        private const long LogbookPl = 117_810;

        public const decimal HarvestKg = 2027m;
    public const decimal HarvestAbwG = 19.4m;

        private sealed record PondPlan(
        PondConstructionType Type, string Prefix, string ShortCode, decimal AreaM2,
        long Ordered1, int PerBag1, int Bags1, decimal Net1, decimal HarvestKg1, decimal HarvestAbw1,
        long Ordered2, int PerBag2, int Bags2, decimal Net2)
    {
        public long Pl1 => (long)PerBag1 * Bags1;
        public long Pl2 => (long)PerBag2 * Bags2;
        public bool IsLogbookPond => Type == PondConstructionType.Earthen;
    }

    private static readonly PondPlan[] Plans =
    {
        new(PondConstructionType.Earthen, "Earthen Pond", "EP", 1500m, 100_000, 3570, 33, 87m, HarvestKg, HarvestAbwG, 100_000, 3610, 33, 85m),
        new(PondConstructionType.Concrete, "Concrete Pond", "CP", 600m, 66_000, 3550, 22, 88m, 1235m, 18.6m, 66_000, 3580, 22, 86m),
        new(PondConstructionType.HdpeLined, "HDPE-lined Pond", "HP", 1000m, 110_000, 3600, 36, 91m, 2310m, 19.8m, 110_000, 3620, 36, 90m),
    };

    private readonly CapstoneDbContext _context;
    private int _t;
    private long _ownerId;
    private long _techId;
    private Dictionary<string, ParameterDefinition> _parameters;
    private Dictionary<string, InventoryItem> _feeds;

    public LogbookSampleDataBuilder(CapstoneDbContext context)
    {
        _context = context;
    }

    private static DateTime Local(DateOnly d, int hour, int min = 0) =>
        DateTime.SpecifyKind(d.ToDateTime(new TimeOnly(hour, min)).AddHours(-8), DateTimeKind.Utc);

    public void Create()
    {
        var farms = new FarmSeedBuilder(_context);
        var pw = SeedOptions.DevelopmentPassword;
        var farm = farms.EnsureFarm(FarmCode, "Abi Aqua Farm (test)", "Purok 5 (test data)", "General Santos City", "South Cotabato", "Tambler");
        var owner = farms.EnsureUser(farm.Id, OwnerUserName, "Iba", "Abi", StaticRoleNames.Tenants.FarmOwner, pw);
        farms.EnsureUser(farm.Id, "msam", "Mas", "Sam", StaticRoleNames.Tenants.FarmOwner, pw);
        var tech = farms.EnsureUser(farm.Id, "mkim", "Mik", "Kim", StaticRoleNames.Tenants.FarmTechnician, pw);

        if (_context.Ponds.IgnoreQueryFilters().Any(p => p.TenantId == farm.Id))
        {
            return;
        }

        _t = farm.Id;
        _ownerId = owner.Id;
        _techId = tech.Id;
        _parameters = _context.ParameterDefinitions.AsNoTracking().ToDictionary(p => p.Id);

        var profile = _context.FarmProfiles.IgnoreQueryFilters().First(p => p.Id == _t);
        profile.FarmingSystem = FarmingSystem.Intensive;
        profile.PostalCode = "9500";
        profile.ContactName = "Iba Abi";
        profile.ContactPhone = "0917 000 2222";
        profile.TotalAreaHectares = 0.35m;
        profile.WaterQualityDaily = false;
        profile.UsesSizeGrading = false;
        (profile.LotAreaCode, profile.LotPlaceCode, profile.LotFarmNumber) = (LotPlaces.GenSanArea, 24, 2);
        _context.SaveChanges();

        Seed();
    }

        public static decimal?[] LogbookFeeding(int doc)
    {
        decimal?[] Same(decimal kg) => new decimal?[] { kg, kg, kg, kg };
        if (doc == 0) return new decimal?[] { null, null, 0.5m, 0.5m };
        if (doc == 19) return new decimal?[] { 2.7m, 2.7m, 3m, 3m };
        if (doc >= 1 && doc <= 25) return Same(Page1[doc - 1]);
        if (doc >= 26 && doc <= 50) return Same(Page2[doc - 26]);
        if (doc >= 51 && doc <= 58) return Same(14m);
        if (doc >= 59 && doc <= 86) return Same(15m);
        return null;
    }

    private static readonly decimal[] Page1 = { 0.5m, 0.5m, 0.7m, 0.7m, 0.7m, 1m, 1m, 1.2m, 1.5m, 1.5m, 2m, 2m, 2.2m, 2.2m, 2.5m, 2.5m, 2.7m, 2.7m, 0m, 3m, 3m, 3.2m, 3.2m, 3.5m, 3.5m };
    private static readonly decimal[] Page2 = { 3.7m, 3.7m, 4m, 4m, 5m, 6.5m, 6.5m, 7m, 7m, 8m, 8.5m, 8.5m, 8.5m, 9m, 9m, 10m, 11m, 11m, 12m, 12m, 13m, 13m, 14m, 14m, 14m };

        private const int LastFedDoc1 = 81;

        private static readonly Dictionary<int, string> TrayRemarks1 = new()
    {
        [28] = "Feeding tray cleared", [31] = "Feeding tray consumed", [35] = "Feeding tray consumed", [40] = "Feeding tray consumed",
        [45] = "Feeding tray consumed", [46] = "Feeding tray checked", [60] = "Feeding tray cleared",
    };

        private static readonly (int Doc, decimal AbwG, string Note)[] AbwRemarks1 =
    {
        (28, 3.1m, null), (35, 4.1m, null), (43, 6.5m, null), (52, 9.1m, "First written 8.7, crossed out."), (60, 11.7m, null), (65, 14.9m, null), (72, 17.4m, null),
    };

        private static readonly (DateOnly Date, int Count, string Cause)[] Losses1 =
    {
        (new DateOnly(2026, 4, 23), 556, "2 kg taken out for a DOST test"),
        (new DateOnly(2026, 5, 11), 28, "Jumped out at the dike after a disturbance"),
        (new DateOnly(2026, 5, 12), 43, "Jumped out at the dike after a disturbance"),
        (new DateOnly(2026, 5, 13), 7, "Jumped out at the dike"),
        (new DateOnly(2026, 5, 18), 35, "Collected along the dike (cause not written)"),
        (new DateOnly(2026, 5, 31), 39, "Collected along the dike (cause not written)"),
        (new DateOnly(2026, 6, 5), 15, "Collected along the dike (cause not written)"),
    };

        private static readonly (string Code, int Sacks)[] Purchases =
    {
        ("N310", 2), ("N311", 5), ("N312", 20), ("N313", 25), ("N313", 4), ("N314", 30), ("N314", 4), ("N315", 40), ("N315", 17), ("N315", 13),
    };

        private static readonly (string Code, string Form, decimal Pack, decimal CostPhp)[] FeedCodes =
    {
        ("N310", "Crumble", 10m, 110m), ("N311", "Crumble", 10m, 105m), ("N312", "Pellet", 10m, 100m),
        ("N313", "Pellet", 20m, 92m), ("N314", "Pellet", 20m, 86m), ("N315", "Pellet", 20m, 80m),
    };

        private sealed class FeedStore
    {
        public decimal Scale = 1m;
        public readonly List<(InventoryLot Lot, decimal Left)> Lots = new();
        public int Next;
    }

        private static decimal?[] Feeding(int doc, long pl)
    {
        var kgs = LogbookFeeding(doc);
        return pl == LogbookPl || kgs == null ? kgs : kgs.Select(k => k.HasValue ? Math.Max(0.1m, Math.Round(k.Value * pl / LogbookPl, 1)) : (decimal?)null).ToArray();
    }

    private void Seed()
    {
        var ponds = new List<Pond>();
        var order = _context.PondNamePrefixes.IgnoreQueryFilters().Where(x => x.TenantId == _t).Max(x => (int?)x.SortOrder) ?? 0;
        foreach (var plan in Plans)
        {
            var prefix = _context.PondNamePrefixes.IgnoreQueryFilters().FirstOrDefault(x => x.TenantId == _t && x.ShortCode == plan.ShortCode)
                         ?? new PondNamePrefix { TenantId = _t, Name = plan.Prefix, ShortCode = plan.ShortCode, SortOrder = ++order, IsActive = true };
            if (prefix.Id == Guid.Empty)
            {
                _context.PondNamePrefixes.Add(prefix);
                _context.SaveChanges();
            }

            var earthen = plan.Type == PondConstructionType.Earthen;
            var pond = new Pond
            {
                TenantId = _t, CreatorUserId = _ownerId, CreationTime = Local(new DateOnly(2025, 11, 10), 9), PrefixId = prefix.Id, Number = 1, PondCode = PondNaming.FormatCode(plan.ShortCode, 1),
                LotNumber = ponds.Count + 1, AreaM2 = plan.AreaM2, AverageDepthM = earthen ? 1.2m : 1.5m, MaxDepthM = earthen ? 1.5m : 1.8m, Purpose = PondPurpose.GrowOut, ConstructionType = plan.Type,
                InletType = "PVC pipe with filter bag", OutletType = earthen ? "Monk with gate boards" : "Central drain with standpipe",
                AerationSystem = earthen ? "Paddlewheel x4" : "Paddlewheel x2 and an air blower with diffuser lines",
            };
            _context.Ponds.Add(pond);
            _context.SaveChanges();
            _context.PondHistories.Add(new PondHistory
            {
                TenantId = _t, CreatorUserId = _ownerId, PondId = pond.Id, ChangeDateUtc = pond.CreationTime, ChangeType = PondChangeType.Created,
                Description = $"Pond {pond.PondCode} ({PondNaming.DisplayName(plan.Prefix, 1)}) created ({plan.AreaM2:0.##} m², GrowOut). Status Available.",
            });
            ponds.Add(pond);
        }

        var hatchery = new HatcherySource
        {
            TenantId = _t, CreatorUserId = _ownerId, Name = "Gensan Shrimp Hatchery (Charoen Pokphand Foods Philippines Corporation)", ShortCode = "GSH",
            Address = "Brgy. Tinoto, Maasim, Sarangani Province", BfarAccreditationNo = "HA01-R12SAR-02092013-RNW101725", AccreditationStatus = AccreditationStatus.Accredited,
        };
        _context.HatcherySources.Add(hatchery);
        _context.SaveChanges();
        _context.HatcheryCertificates.Add(new HatcheryCertificate { TenantId = _t, CreatorUserId = _ownerId, HatcheryId = hatchery.Id, CertificateType = CertificateType.Other, CertificateNo = "BFAR accreditation HA01-R12SAR-02092013-RNW101725", IssueDate = new DateOnly(2025, 10, 17), ExpiryDate = new DateOnly(2028, 10, 17) });
        var health = new HatcheryCertificate { TenantId = _t, CreatorUserId = _ownerId, HatcheryId = hatchery.Id, CertificateType = CertificateType.Health, CertificateNo = "HC-GSH-TEST-2026", IssueDate = new DateOnly(2026, 1, 5), ExpiryDate = new DateOnly(2026, 12, 31) };
        _context.HatcheryCertificates.Add(health);

        var buyer = new Buyer { TenantId = _t, CreatorUserId = _ownerId, BuyerType = BuyerType.Individual, Name = "Tambler shrimp buyer (test)", Phone = "0921 000 3333", Address = "Tambler, General Santos City (test)" };
        _context.Buyers.Add(buyer);

        _feeds = FeedCodes.Select((f, i) => new InventoryItem
        {
            TenantId = _t, CreatorUserId = _ownerId, Category = InventoryCategory.Feed, Brand = "Uni-President", ProductName = $"Nu Ri {f.Code}", Code = f.Code, FeedType = f.Form,
            BaiRegistrationNo = $"BAI-TEST-{i + 1:0000}", Unit = "kg", ReorderLevel = f.Pack * 2, PackSize = f.Pack,
        }).ToDictionary(x => x.Code);
        _context.InventoryItems.AddRange(_feeds.Values);
        _context.SaveChanges();

        var saleNo = SeedLogbookCycle(ponds, hatchery, health, buyer);
        var lastDoc = SeedRunningCycle(ponds, hatchery, health);

        _context.NumberSequences.Add(new NumberSequence { Id = Guid.NewGuid(), TenantId = _t, SequenceType = NumberSequenceType.CycleCode, Scope = "2026-03-GSH", NextValue = 2 });
        if (lastDoc >= 0)
        {
            _context.NumberSequences.Add(new NumberSequence { Id = Guid.NewGuid(), TenantId = _t, SequenceType = NumberSequenceType.CycleCode, Scope = "2026-09-GSH", NextValue = 2 });
        }

        _context.NumberSequences.Add(new NumberSequence { Id = Guid.NewGuid(), TenantId = _t, SequenceType = NumberSequenceType.DispatchReceipt, Scope = "2026", NextValue = saleNo + 1 });
        foreach (var pond in ponds)
        {
            _context.NumberSequences.Add(new NumberSequence { Id = Guid.NewGuid(), TenantId = _t, SequenceType = NumberSequenceType.HarvestLot, Scope = $"P{pond.LotNumber}-2026", NextValue = 2 });
        }

        _context.SaveChanges();
    }

        private int SeedLogbookCycle(List<Pond> ponds, HatcherySource hatchery, HatcheryCertificate health, Buyer buyer)
    {
        var delivery = Delivery(hatchery, Stocking1, "GSH-B0318", new DateOnly(2026, 3, 5), Plans.Sum(p => p.Ordered1), 0.35m,
            Plans.Select((p, i) => (ponds[i].PondCode, p.PerBag1, p.Bags1)).ToList(), "The EP-01 count is the logbook's.");
        var cycle = new ProductionCycle { TenantId = _t, CreatorUserId = _ownerId, CycleCode = "2026-03-GSH-01", Status = CycleStatus.Active, FryDeliveryId = delivery.Id };
        _context.ProductionCycles.Add(cycle);
        _context.SaveChanges();

        var store = new FeedStore { Scale = Plans.Sum(p => p.Pl1) / (decimal)LogbookPl };
        var batches = new List<PondBatch>();
        var facts = new List<BatchFacts>();
        for (var i = 0; i < Plans.Length; i++)
        {
            var plan = Plans[i];
            var round = Preparation(ponds[i], new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 16), Stocking1, new[]
            {
                (0, plan.IsLogbookPond ? "Drained the pond after the last crop; drying started." : "Drained the pond and scrubbed the walls."),
                (7, plan.IsLogbookPond ? "Applied agricultural lime and tilled the pond bottom." : "Disinfected with calcium hypochlorite; rinsed after two days."),
                (14, "Filled with filtered seawater; aerators tested."),
            });
            batches.Add(Stock(cycle, ponds[i], round, Stocking1, plan.Pl1, (char)('A' + i), hatchery, health));
            facts.Add(new BatchFacts { PlCountStocked = plan.Pl1, PondAreaM2 = plan.AreaM2, StockingDate = Stocking1 });
        }

        for (var doc = 0; doc <= LastFedDoc1; doc++)
        {
            for (var i = 0; i < Plans.Length; i++)
            {
                facts[i].CumulativeFeedKg += FeedDay(batches[i], Stocking1, doc, Feeding(doc, Plans[i].Pl1), store, TrayRemarks1.GetValueOrDefault(doc));
            }
        }

        var ungraded = _context.SizeGrades.IgnoreQueryFilters().First(g => g.TenantId == _t && g.Label == SizeGrade.UngradedLabel);
        var closedOn = Harvest1.AddDays(7);
        var saleNo = 0;
        for (var i = 0; i < Plans.Length; i++)
        {
            var plan = Plans[i];
            var batch = batches[i];
            var f = facts[i];

            foreach (var (doc, abw, note) in AbwRemarks1)
            {
                var value = plan.IsLogbookPond ? abw : Math.Round(abw * plan.HarvestAbw1 / HarvestAbwG, 1);
                var first = doc == AbwRemarks1[0].Doc;
                f.GrowthSamples.Add(Abw(batch, Stocking1, doc, value, first ? plan.Net1 : null,
                    first ? $"Survival {plan.Net1:0}% from the survival net at DOC 7." : plan.IsLogbookPond ? note : null));
            }

            if (plan.IsLogbookPond)
            {
                foreach (var (date, count, cause) in Losses1)
                {
                    f.Mortalities.Add(Mortality(batch, date, count, cause));
                }
            }
            else
            {
                foreach (var (date, count, _) in Losses1.Where(l => l.Cause.StartsWith("Collected")))
                {
                    f.Mortalities.Add(Mortality(batch, date, (int)Math.Round(count * plan.Pl1 / (decimal)LogbookPl), "Collected at the drain (cause not written)"));
                }
            }

            f.CumulativeMortality = f.Mortalities.Sum(m => m.Count);
            WaterChange(batch, Stocking1.AddDays(43), "CW (change water)");
            _context.SaveChanges();

            var harvest = new Harvest
            {
                TenantId = _t, CreatorUserId = _techId, BatchId = batch.Id, HarvestedAtUtc = Local(Harvest1, 5 + i), HarvestType = HarvestType.Total, TotalKg = plan.HarvestKg1, AbwG = plan.HarvestAbw1,
                LotCode = HarvestLotCoder.Format(Harvest1.Year, LotPlaces.GenSanArea, 24, 2, ponds[i].LotNumber.Value, 1), BuyerId = buyer.Id, WeighedBy = WeighedBy.Buyer,
                SmoothShell = true, IntactAppendages = true, NoNecrosis = true,
                QualityNotes = plan.IsLogbookPond ? "Logbook reads \"2,827 kg total sold\" with the 8 written over another digit; 2,027 kg fits the fry count, 87% survival and 19.4 g ABW." : null,
                GradeLines = new List<HarvestGradeLine> { new() { TenantId = _t, SizeGradeId = ungraded.Id, Kg = plan.HarvestKg1 } },
            };
            _context.Harvests.Add(harvest);
            batch.Status = BatchStatus.Harvested;
            _context.SaveChanges();
            f.Harvests.Add(new HarvestFact
            {
                Date = Harvest1, HarvestedAtUtc = harvest.HarvestedAtUtc, IsFinal = true, TotalKg = plan.HarvestKg1, AbwG = plan.HarvestAbw1,
                GradeLines = new List<(decimal, decimal?)> { (plan.HarvestKg1, null) },
            });

            var price = SaleLine.BaseAbwPrice(210m, 10m, 5m, plan.HarvestAbw1);
            var sale = new Sale
            {
                Id = Guid.NewGuid(), TenantId = _t, CreatorUserId = _ownerId, BuyerId = buyer.Id, SaleDate = Harvest1, DispatchReceiptNo = $"DR-2026-{++saleNo:0000}",
                PaymentTerms = "Cash on delivery", Destination = buyer.Address,
                Lines = new List<SaleLine>
                {
                    new()
                    {
                        TenantId = _t, HarvestGradeLineId = harvest.GradeLines[0].Id, Kg = plan.HarvestKg1, PricePerKg = price, PriceMethod = PriceMethod.BaseAbw,
                        BasePricePerKg = 210m, BaseAbwG = 10m, StepPerG = 5m, PriceAbwG = plan.HarvestAbw1, ReferencePricePerKg = price,
                    },
                },
            };
            sale.Payments.Add(new SalePayment { TenantId = _t, CreatorUserId = _ownerId, PaidOn = Harvest1, Amount = sale.Lines[0].Amount, Method = "Cash" });
            _context.Sales.Add(sale);

            var clean = Log(batch, Local(Harvest1.AddDays(3), 8), LogType.PostProduction, plan.IsLogbookPond ? "Sludge pushed out of the pond bottom." : "Walls and floor pressure-washed.");
            _context.PostProductionDetails.Add(new PostProductionDetail { Id = clean.Id, TenantId = _t, ActivityType = PostProductionActivity.SludgeRemoval });
            var drying = Log(batch, Local(Harvest1.AddDays(7), 8), LogType.PostProduction, "Pond left to dry until the next crop.");
            _context.PostProductionDetails.Add(new PostProductionDetail { Id = drying.Id, TenantId = _t, ActivityType = PostProductionActivity.Drying });

            var ind = IndicatorCalculator.Compute(f, closedOn);
            batch.FinalAbwG = ind.FinalAbwG;
            batch.FinalFcr = ind.FinalFcr;
            batch.SurvivalRatePct = ind.SurvivalRatePct ?? 0m;
            batch.TotalHarvestKg = ind.TotalHarvestKg;
            batch.TotalFeedKg = ind.CumulativeFeedKg;
            batch.CultureDays = ind.CultureDays;
            batch.Status = BatchStatus.Closed;
            batch.ClosedAtUtc = Local(closedOn, 10);
            batch.ClosedByUserId = _ownerId;
        }

        cycle.Status = CycleStatus.Closed;
        cycle.Outcome = CycleOutcome.Completed;
        cycle.ClosedAtUtc = Local(closedOn, 11);
        cycle.ClosedByUserId = _ownerId;

        foreach (var (lot, left) in store.Lots.Where(l => l.Left > 0))
        {
            Move(lot, Local(closedOn, 15), StockMovementType.Adjustment, -left, null, reason: "Stock count at cycle close: left in opened sacks, written off");
        }

        _context.SaveChanges();
        return saleNo;
    }

        private int SeedRunningCycle(List<Pond> ponds, HatcherySource hatchery, HatcheryCertificate health)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8)).AddDays(-1);
        var lastDoc = Math.Min(yesterday.DayNumber - Stocking2.DayNumber, 86);
        if (lastDoc < 0)
        {
            return -1;
        }

        var delivery = Delivery(hatchery, Stocking2, "GSH-B0926", new DateOnly(2026, 9, 12), Plans.Sum(p => p.Ordered2), 0.36m,
            Plans.Select((p, i) => (ponds[i].PondCode, p.PerBag2, p.Bags2)).ToList(), null);
        var cycle = new ProductionCycle { TenantId = _t, CreatorUserId = _ownerId, CycleCode = "2026-09-GSH-01", Status = CycleStatus.Active, FryDeliveryId = delivery.Id };
        _context.ProductionCycles.Add(cycle);
        _context.SaveChanges();

        var store = new FeedStore { Scale = Plans.Sum(p => p.Pl2) / (decimal)LogbookPl };
        var batches = new List<PondBatch>();
        for (var i = 0; i < Plans.Length; i++)
        {
            var plan = Plans[i];
            var round = Preparation(ponds[i], new DateOnly(2026, 9, 8), new DateOnly(2026, 9, 24), Stocking2, new[]
            {
                (0, plan.IsLogbookPond ? "Checked the dried pond; repaired the gate boards of the monk." : "Checked the walls and the drain after the dry months."),
                (6, plan.IsLogbookPond ? "Applied agricultural lime and tilled the pond bottom." : "Disinfected with calcium hypochlorite; rinsed after two days."),
                (15, "Filled with filtered seawater; aerators tested."),
            });
            batches.Add(Stock(cycle, ponds[i], round, Stocking2, plan.Pl2, (char)('A' + i), hatchery, health));
        }

        var trays = new Dictionary<int, string> { [27] = "Feeding tray cleared", [32] = "Feeding tray consumed", [38] = "Feeding tray consumed", [44] = "Feeding tray consumed" };
        for (var doc = 0; doc <= lastDoc; doc++)
        {
            for (var i = 0; i < Plans.Length; i++)
            {
                var blankFrom = doc == lastDoc && i == 0 ? 3 : (int?)null;
                var notFed = doc == 33 && doc < lastDoc ? 4 : (int?)null;
                FeedDay(batches[i], Stocking2, doc, Feeding(doc, Plans[i].Pl2), store, trays.GetValueOrDefault(doc), blankFrom, notFed);
            }
        }

        for (var i = 0; i < Plans.Length; i++)
        {
            var plan = Plans[i];
            var batch = batches[i];
            foreach (var (doc, abw) in new[] { (28, 3.0m), (35, 4.3m), (42, 6.2m), (49, 8.6m) })
            {
                if (doc <= lastDoc)
                {
                    var first = doc == 28;
                    Abw(batch, Stocking2, doc, plan.IsLogbookPond ? abw : Math.Round(abw * plan.HarvestAbw1 / HarvestAbwG, 1), first ? plan.Net2 : null,
                        first ? $"Survival {plan.Net2:0}% from the survival net at DOC 7." : null);
                }
            }

            if (plan.IsLogbookPond && lastDoc >= 41)
            {
                Mortality(batch, Stocking2.AddDays(41), 31, "Jumped out at the dike after a disturbance");
            }

            Water(batch, Local(Stocking2.AddDays(-1), 16), LogType.PreStockingWaterQuality, i);
            for (var doc = 1; doc <= lastDoc; doc++)
            {
                var day = Stocking2.AddDays(doc);
                if (day.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday)
                {
                    Water(batch, Local(day, 5, 30 + i * 10), LogType.WaterQuality, doc + i);
                }

                if (doc >= 30 && doc % 2 == 0)
                {
                    WaterChange(batch, day, "About 20% of the water changed");
                }
            }
        }

        _context.SaveChanges();
        return lastDoc;
    }

        private FryDelivery Delivery(HatcherySource hatchery, DateOnly date, string lot, DateOnly ordered, long papers, decimal costPerPiece, List<(string Pond, int PerBag, int Bags)> counts, string note)
    {
        var counted = counts.Sum(c => (long)c.PerBag * c.Bags);
        var delivery = new FryDelivery
        {
            TenantId = _t, CreatorUserId = _ownerId, HatcheryId = hatchery.Id, HatcheryBatchNumber = lot, DeliveryDate = date,
            PlOrdered = papers, PlReceived = counted, CostPhp = papers * costPerPiece,
            PlHealthCertificateNo = $"PLHC-{date:yyyyMMdd}", TransportPermitNo = $"DTP-{date:yyyyMMdd}-GSH", SfscNo = $"SFSC-{date:yyyy}-GSH-{date:MMdd}",
            StockingFinishedAtUtc = Local(date, 12), StockingFinishedByUserId = _ownerId,
            ReceivedAtUtc = Local(date, 9), TransportMode = TransportMode.Truck, TimePackedUtc = Local(date, 6),
            Notes = "Counted at the farm: " + string.Join("; ", counts.Select(c => $"{c.Pond} {c.PerBag:N0} a bag × {c.Bags} bags = {(long)c.PerBag * c.Bags:N0}"))
                    + $". The papers say {papers:N0} (the hatchery adds extra)." + (note == null ? "" : " " + note),
        };
        _context.FryDeliveries.Add(delivery);
        _context.SaveChanges();
        _context.FryDeliveryStatusChanges.Add(new FryDeliveryStatusChange { TenantId = _t, CreatorUserId = _ownerId, DeliveryId = delivery.Id, Status = FryDeliveryStatus.Ordered, ChangedAtUtc = Local(ordered, 10), Note = "Ordered by phone" });
        _context.FryDeliveryStatusChanges.Add(new FryDeliveryStatusChange { TenantId = _t, CreatorUserId = _techId, DeliveryId = delivery.Id, Status = FryDeliveryStatus.Received, ChangedAtUtc = Local(date, 9) });
        return delivery;
    }

    private PondPreparationRound Preparation(Pond pond, DateOnly started, DateOnly prepared, DateOnly used, (int Day, string Text)[] entries)
    {
        var round = new PondPreparationRound
        {
            TenantId = _t, CreatorUserId = _techId, PondId = pond.Id, StartedAtUtc = Local(started, 8), PreparedAtUtc = Local(prepared, 15), PreparedByUserId = _techId, UsedAtUtc = Local(used, 7),
        };
        _context.PondPreparationRounds.Add(round);
        _context.SaveChanges();
        foreach (var (day, text) in entries)
        {
            _context.PondPreparationEntries.Add(new PondPreparationEntry { TenantId = _t, CreatorUserId = _techId, RoundId = round.Id, ObservedAtUtc = Local(started.AddDays(day), 9), Description = text });
        }

        return round;
    }

    private PondBatch Stock(ProductionCycle cycle, Pond pond, PondPreparationRound round, DateOnly date, long pl, char letter, HatcherySource hatchery, HatcheryCertificate health)
    {
        var band = _context.DensityBands.AsNoTracking().FirstOrDefault(b => b.FarmingSystem == FarmingSystem.Intensive && b.ConstructionType == pond.ConstructionType);
        var batch = new PondBatch
        {
            TenantId = _t, CreatorUserId = _ownerId, CycleId = cycle.Id, PondId = pond.Id, PreparationRoundId = round.Id, Letter = letter.ToString(), BatchCode = $"{cycle.CycleCode}-{letter}",
            Status = BatchStatus.Stocked, StockedAtUtc = Local(date, 11, 10 + (letter - 'A') * 15), StockingDate = date, PondAreaM2AtStocking = pond.AreaM2,
            PlCountStocked = pl, AcclimationMinutes = 45,
            FarmingSystemAtStocking = FarmingSystem.Intensive, ConstructionTypeAtStocking = pond.ConstructionType, DensityFlag = band?.Judge(pl / pond.AreaM2),
            HatcheryCertificationStatusAtStocking = hatchery.DescribeCertification(health, date),
        };
        _context.PondBatches.Add(batch);
        _context.BatchTechnicians.Add(new BatchTechnician { TenantId = _t, BatchId = batch.Id, UserId = _techId });
        _context.SaveChanges();
        return batch;
    }

    private static readonly int[] FeedingHours = { 6, 10, 14, 18 };

        private decimal FeedDay(PondBatch batch, DateOnly stocking, int doc, decimal?[] kgs, FeedStore store, string trayRemark, int? blankFrom = null, int? notFedFrom = null)
    {
        if (kgs == null)
        {
            return 0m;
        }

        var day = stocking.AddDays(doc);
        var fed = 0m;
        for (var n = 1; n <= 4; n++)
        {
            if (kgs[n - 1] == null || (blankFrom.HasValue && n >= blankFrom.Value))
            {
                continue;
            }

            var session = new FeedingSession
            {
                Id = Guid.NewGuid(), TenantId = _t, CreatorUserId = _techId, BatchId = batch.Id, FeedDate = day, Session = n, ClientRequestId = Guid.NewGuid(),
                Notes = n == 4 ? trayRemark : null,
            };
            _context.FeedingSessions.Add(session);
            if (notFedFrom.HasValue && n >= notFedFrom.Value)
            {
                session.Status = FeedingStatus.NotFed;
                session.Notes = "Heavy rain; feeding skipped";
                continue;
            }

            session.Status = FeedingStatus.Fed;
            session.FedAtTime = $"{FeedingHours[n - 1]:00}:00";
            var kg = kgs[n - 1].Value;
            fed += kg;
            while (kg > 0)
            {
                var lot = Draw(store, day);
                var take = Math.Min(kg, store.Lots[^1].Left);
                store.Lots[^1] = (lot, store.Lots[^1].Left - take);
                kg -= take;
                Move(lot, Local(day, FeedingHours[n - 1]), StockMovementType.Usage, -take, batch.Id, session.Id);
            }
        }

        return fed;
    }

        private InventoryLot Draw(FeedStore store, DateOnly needed)
    {
        if (store.Lots.Count > 0 && store.Lots[^1].Left > 0)
        {
            return store.Lots[^1].Lot;
        }

        var (code, sacks) = store.Next < Purchases.Length ? Purchases[store.Next] : ("N315", 20);
        if (store.Next < Purchases.Length)
        {
            sacks = (int)Math.Ceiling(sacks * store.Scale);
        }

        store.Next++;
        var spec = FeedCodes.First(f => f.Code == code);
        var date = needed.AddDays(-2);
        var lot = new InventoryLot
        {
            TenantId = _t, CreatorUserId = _ownerId, ItemId = _feeds[code].Id, LotNumber = $"{code}-{date:yyMMdd}", ReceivedDate = date, ExpiryDate = date.AddDays(180),
            Supplier = "Feed dealer, General Santos (test)", UnitCost = spec.CostPhp,
        };
        _context.InventoryLots.Add(lot);
        _context.SaveChanges();
        var qty = sacks * spec.Pack;
        Move(lot, Local(date, 9), StockMovementType.Receipt, qty, null, reason: $"{sacks} sacks of {spec.Pack:0} kg");
        store.Lots.Add((lot, qty));
        return lot;
    }

    private void Move(InventoryLot lot, DateTime at, StockMovementType type, decimal qty, Guid? batchId, Guid? sessionId = null, string reason = null) =>
        _context.StockMovements.Add(new StockMovement
        {
            TenantId = _t, CreatorUserId = type == StockMovementType.Usage ? _techId : _ownerId, LotId = lot.Id, MovementDateUtc = at, Type = type, Quantity = qty, UnitCost = lot.UnitCost,
            BatchId = batchId, FeedingSessionId = sessionId, Reason = reason,
        });

    private ProductionLog Log(PondBatch batch, DateTime at, LogType type, string notes = null)
    {
        var log = new ProductionLog { Id = Guid.NewGuid(), TenantId = _t, CreatorUserId = _techId, BatchId = batch.Id, PondId = batch.PondId, LogType = type, ObservedAtUtc = at, Notes = notes, ClientRequestId = Guid.NewGuid() };
        _context.ProductionLogs.Add(log);
        return log;
    }

        private GrowthSampleFact Abw(PondBatch batch, DateOnly stocking, int doc, decimal abw, decimal? survival, string note)
    {
        var log = Log(batch, Local(stocking.AddDays(doc), 8), LogType.GrowthSampling, note);
        _context.GrowthSamplingDetails.Add(new GrowthSamplingDetail { Id = log.Id, TenantId = _t, AbwG = abw, EstimatedSurvivalPct = survival });
        return new GrowthSampleFact { LogId = log.Id, Date = stocking.AddDays(doc), ObservedAtUtc = log.ObservedAtUtc, AbwG = abw, EstimatedSurvivalPct = survival };
    }

    private MortalityFact Mortality(PondBatch batch, DateOnly date, int count, string cause)
    {
        var log = Log(batch, Local(date, 7), LogType.Mortality);
        _context.MortalityDetails.Add(new MortalityDetail { Id = log.Id, TenantId = _t, Count = count, Cause = MortalityCause.Others, OtherCause = cause });
        return new MortalityFact { Date = date, ObservedAtUtc = log.ObservedAtUtc, Count = count };
    }

    private void WaterChange(PondBatch batch, DateOnly day, string note)
    {
        var log = Log(batch, Local(day, 7), LogType.PondActivity, note);
        _context.PostProductionDetails.Add(new PostProductionDetail { Id = log.Id, TenantId = _t, ActivityType = PostProductionActivity.WaterExchange });
    }

        private void Water(PondBatch batch, DateTime at, LogType type, int seed)
    {
        var log = Log(batch, at, type);
        foreach (var (code, value) in new[]
        {
            (ParameterCodes.Temperature, 28.2m + seed % 5 * 0.3m), (ParameterCodes.Salinity, 16m + seed % 4), (ParameterCodes.PH, 7.8m + seed % 3 * 0.1m), (ParameterCodes.DO, 4.6m + seed % 4 * 0.3m),
        })
        {
            var p = _parameters[code];
            _context.WaterQualityMeasurements.Add(new WaterQualityMeasurement { Id = Guid.NewGuid(), TenantId = _t, LogId = log.Id, ParameterCode = code, Value = value, UnitCode = p.UnitCode, IsOutOfRange = p.IsOutOfOptimalRange(value), Side = p.SideOf(value) });
        }
    }
}
