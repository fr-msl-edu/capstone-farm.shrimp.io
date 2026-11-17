using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Capstone.Authorization;
using Capstone.Common;
using Capstone.Domain;
using Capstone.Harvests;
using Capstone.Production.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Capstone.Sales;

public class TallySaleInput
{
        public DateOnly? SaleDate { get; set; }

    [StringLength(Sale.MaxTextLength)]
    public string PaymentTerms { get; set; }

    public DateOnly? DueDate { get; set; }

    [Range(0, 100000000, ErrorMessage = "Price per kg cannot be negative.")]
    public decimal PricePerKg { get; set; }

    public PriceMethod PriceMethod { get; set; } = PriceMethod.Fixed;

    [Range(0, 100000000, ErrorMessage = "The base price cannot be negative.")]
    public decimal? BasePricePerKg { get; set; }

    [Range(0.01, 60, ErrorMessage = "The base ABW must be between 0.01 and 60 g.")]
    public decimal? BaseAbwG { get; set; }

    [Range(0, 1000000, ErrorMessage = "The step per gram cannot be negative.")]
    public decimal? StepPerG { get; set; }

    [Range(0.01, 60, ErrorMessage = "The pricing ABW must be between 0.01 and 60 g.")]
    public decimal? PriceAbwG { get; set; }

    [Range(1, 10000, ErrorMessage = "The base count must be at least 1 piece per kg.")]
    public decimal? BaseCountPerKg { get; set; }

    [Range(0, 1000000, ErrorMessage = "The step per piece cannot be negative.")]
    public decimal? StepPerCount { get; set; }

    [Range(1, 10000, ErrorMessage = "The count must be at least 1 piece per kg.")]
    public decimal? PriceCountPerKg { get; set; }

    public Guid? PriceTableId { get; set; }

    public PaymentInput InitialPayment { get; set; }
}

public class SaveTallySheetInput
{
    [Required]
    public SaveHarvestInput Harvest { get; set; }

    [Required]
    public TallySaleInput Sale { get; set; }
}

public class TallySheetResultDto
{
    public HarvestDto Harvest { get; set; }
    public SaleDto Sale { get; set; }
    public List<WarningDto> Warnings { get; set; } = new();
}

public class TallyPrintSaleDto
{
    public Guid SaleId { get; set; }
    public string DispatchReceiptNo { get; set; }
    public DateOnly SaleDate { get; set; }

        public Guid BuyerId { get; set; }
    public string BuyerName { get; set; }
    public string BuyerAddress { get; set; }
    public List<SaleLineDto> Lines { get; set; } = new();
    public decimal Kg { get; set; }
    public decimal Amount { get; set; }
}

public class TallyPrintDto
{
    public HarvestDto Harvest { get; set; }
    public string FarmName { get; set; }
    public string FarmAddress { get; set; }
    public string FarmContact { get; set; }
    public string BuyerName { get; set; }
    public string BuyerAddress { get; set; }

        public bool ShowsSales { get; set; }

    public List<TallyPrintSaleDto> Sales { get; set; } = new();
}

public class BuyerHarvestDefaultsDto
{
        public Guid? ContainerTypeId { get; set; }

    public WeighedBy? WeighedBy { get; set; }
    public string PaymentTerms { get; set; }

        public BuyerPriceTermsDto PriceTerms { get; set; }
}

[AbpAuthorize(PermissionNames.Harvests)]
public class TallySheetAppService : CapstoneAppServiceBase
{
    private readonly HarvestAppService _harvestService;
    private readonly SaleAppService _saleService;
    private readonly IRepository<Harvest, Guid> _harvests;
    private readonly IRepository<Sale, Guid> _sales;
    private readonly IRepository<Resources.Buyer, Guid> _buyers;
    private readonly IRepository<PriceTable, Guid> _priceTables;
    private readonly IRepository<SaleLine, Guid> _saleLines;
    private readonly IRepository<HarvestGradeLine, Guid> _gradeLines;

    public TallySheetAppService(HarvestAppService harvestService, SaleAppService saleService, IRepository<Harvest, Guid> harvests, IRepository<Sale, Guid> sales,
        IRepository<Resources.Buyer, Guid> buyers, IRepository<PriceTable, Guid> priceTables, IRepository<SaleLine, Guid> saleLines,
        IRepository<HarvestGradeLine, Guid> gradeLines)
    {
        _saleLines = saleLines;
        _gradeLines = gradeLines;
        _buyers = buyers;
        _priceTables = priceTables;
        _harvestService = harvestService;
        _saleService = saleService;
        _harvests = harvests;
        _sales = sales;
    }

        public virtual async Task<List<LookupDto>> GetBuyerOptions()
    {
        return await _buyers.GetAll().AsNoTracking().OrderBy(b => b.Name)
            .Select(b => new LookupDto { Id = b.Id, Name = b.Name, IsActive = b.IsActive }).ToListAsync();
    }

        public virtual async Task<BuyerHarvestDefaultsDto> GetBuyerDefaults(Guid buyerId)
    {
        var dto = new BuyerHarvestDefaultsDto();
        var last = await _harvests.GetAll().AsNoTracking().Include(h => h.Crates)
            .Where(h => h.BuyerId == buyerId).OrderByDescending(h => h.HarvestedAtUtc).FirstOrDefaultAsync();
        if (last != null)
        {
            dto.WeighedBy = last.WeighedBy;
            dto.ContainerTypeId = last.Crates.Where(c => c.ContainerTypeId.HasValue).GroupBy(c => c.ContainerTypeId)
                .OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
        }

        if (await IsGrantedAsync(PermissionNames.Sales))
        {
            dto.PaymentTerms = await _sales.GetAll().AsNoTracking().Where(s => s.BuyerId == buyerId && s.PaymentTerms != null)
                .OrderByDescending(s => s.SaleDate).ThenByDescending(s => s.CreationTime).Select(s => s.PaymentTerms).FirstOrDefaultAsync();
            dto.PriceTerms = await _saleService.GetBuyerPriceTerms(buyerId);
        }

        return dto;
    }

        public virtual async Task<TallyPrintDto> GetPrint(Guid harvestId)
    {
        var harvest = await _harvestService.Get(harvestId);
        var profile = await Farm.GetProfileAsync();
        var buyer = harvest.BuyerId.HasValue ? await _buyers.GetAll().AsNoTracking().FirstOrDefaultAsync(b => b.Id == harvest.BuyerId.Value) : null;
        var dto = new TallyPrintDto
        {
            Harvest = harvest, FarmName = profile.FarmName, FarmAddress = profile.FormatAddress(),
            FarmContact = string.Join(" · ", new[] { profile.ContactName, profile.ContactPhone }.Where(x => !string.IsNullOrWhiteSpace(x))),
            BuyerName = buyer?.Name, BuyerAddress = buyer?.Address, ShowsSales = await IsGrantedAsync(PermissionNames.Sales),
        };
        if (!dto.ShowsSales)
        {
            return dto;
        }

        var saleIds = await (from l in _saleLines.GetAll()
                             join g in _gradeLines.GetAll() on l.HarvestGradeLineId equals g.Id
                             join s in _sales.GetAll() on l.SaleId equals s.Id
                             where g.HarvestId == harvestId
                             select s.Id).Distinct().ToListAsync();
        foreach (var id in saleIds)
        {
            var sale = await _saleService.Get(id);
            var lines = sale.Lines.Where(l => l.HarvestId == harvestId).ToList();
            dto.Sales.Add(new TallyPrintSaleDto
            {
                SaleId = sale.Id, DispatchReceiptNo = sale.DispatchReceiptNo, SaleDate = sale.SaleDate, BuyerId = sale.BuyerId, BuyerName = sale.BuyerName, BuyerAddress = sale.BuyerAddress, Lines = lines,
                Kg = lines.Sum(l => l.Kg), Amount = lines.Sum(l => l.Amount),
            });
        }

        dto.Sales = dto.Sales.OrderBy(x => x.SaleDate).ThenBy(x => x.DispatchReceiptNo).ToList();
        return dto;
    }

        [AbpAuthorize(PermissionNames.Harvests_Create, PermissionNames.Sales, RequireAllPermissions = true)]
    public virtual async Task<TallySheetResultDto> Save(SaveTallySheetInput input)
    {
        var h = input.Harvest;
        var s = input.Sale;
        if (h.Id.HasValue)
        {
            throw new UserFriendlyException("A tally sheet records a new harvest. Edit a saved harvest and its sale on their own pages.");
        }

        if (!h.BuyerId.HasValue)
        {
            throw new UserFriendlyException("Choose the buyer.");
        }

        if ((h.GradeLines == null || h.GradeLines.Count == 0) && await Farm.GetUsesSizeGradingAsync())
        {
            throw new UserFriendlyException($"Put the harvest kg on a grade line (for example \"{SizeGrade.UngradedLabel}\") to sell it.");
        }

        if (!Enum.IsDefined(s.PriceMethod))
        {
            throw new UserFriendlyException("Choose how the price was agreed.");
        }

        if (s.PriceMethod == PriceMethod.BaseAbw && (!s.BasePricePerKg.HasValue || !s.BaseAbwG.HasValue || !s.StepPerG.HasValue))
        {
            throw new UserFriendlyException("Enter the base price, the base ABW and the step per gram.");
        }

        if (s.PriceMethod == PriceMethod.BaseAbw && !s.PriceAbwG.HasValue && !h.AbwG.HasValue && !h.AbwSampleWeightG.HasValue)
        {
            throw new UserFriendlyException("Enter the ABW sample or the ABW the price is based on.");
        }

        if (s.PriceMethod == PriceMethod.BaseCount && (!s.BasePricePerKg.HasValue || !s.BaseCountPerKg.HasValue || !s.StepPerCount.HasValue))
        {
            throw new UserFriendlyException("Enter the base price, the base count and the step per piece.");
        }

        if (s.PriceMethod == PriceMethod.Table && !s.PriceTableId.HasValue)
        {
            throw new UserFriendlyException("Choose the price table.");
        }

        var harvest = await _harvestService.Create(h);
        var tables = s.PriceMethod == PriceMethod.Table
            ? await _priceTables.GetAll().AsNoTracking().Include(t => t.Rows).Where(t => t.Id == s.PriceTableId).ToListAsync()
            : new List<PriceTable>();
        var zone = await Farm.GetTimeZoneAsync();
        var saleDate = s.SaleDate ?? FarmClock.LocalDate(harvest.HarvestedAtUtc, zone);
        var gradeLines = harvest.GradeLines.Where(g => g.UnsoldKg > 0).ToList();
        var perLine = gradeLines.Count > 1 && s.PriceMethod is PriceMethod.BaseCount or PriceMethod.Table;
        var sale = await _saleService.Create(new SaveSaleInput
        {
            BuyerId = h.BuyerId.Value, SaleDate = saleDate, PaymentTerms = s.PaymentTerms, DueDate = s.DueDate, InitialPayment = s.InitialPayment,
            Crates = harvest.Crates.Select(c => new SaleCrateInput { HarvestId = harvest.Id, CrateNo = c.CrateNo }).ToList(),
            Lines = gradeLines.Select(g => new SaleLineInput
            {
                HarvestGradeLineId = g.Id, Kg = g.UnsoldKg, PriceMethod = s.PriceMethod,
                PricePerKg = perLine ? QuoteFor(s, g, harvest, tables) : s.PricePerKg,
                BasePricePerKg = s.BasePricePerKg, BaseAbwG = s.BaseAbwG, StepPerG = s.StepPerG, PriceAbwG = s.PriceAbwG,
                BaseCountPerKg = s.BaseCountPerKg, StepPerCount = s.StepPerCount, PriceCountPerKg = perLine ? null : s.PriceCountPerKg, PriceTableId = s.PriceTableId,
            }).ToList(),
        });

        var saved = await _harvestService.Get(harvest.Id);
        return new TallySheetResultDto
        {
            Harvest = saved, Sale = sale,
            Warnings = harvest.Warnings.Concat(sale.Warnings).GroupBy(w => w.Message).Select(g => g.First()).ToList(),
        };
    }

        private static decimal QuoteFor(TallySaleInput s, GradeLineDto g, HarvestDto harvest, List<PriceTable> tables)
    {
        decimal? count = null;
        if (g.MinCountPerKg.HasValue && g.MaxCountPerKg.HasValue)
        {
            count = (g.MinCountPerKg.Value + g.MaxCountPerKg.Value) / 2m;
        }
        else if (g.MinCountPerKg.HasValue || g.MaxCountPerKg.HasValue)
        {
            count = g.MinCountPerKg ?? g.MaxCountPerKg;
        }
        else if (harvest.AbwG > 0)
        {
            count = SaleLine.CountFromAbw(harvest.AbwG.Value);
        }

        if (!count.HasValue)
        {
            return 0m;
        }

        if (s.PriceMethod == PriceMethod.BaseCount)
        {
            return Math.Max(0m, SaleLine.BaseCountPrice(s.BasePricePerKg ?? 0, s.BaseCountPerKg ?? 0, s.StepPerCount ?? 0, count.Value));
        }

        return tables.FirstOrDefault()?.Find(count.Value)?.PricePerKg ?? 0m;
    }
}
