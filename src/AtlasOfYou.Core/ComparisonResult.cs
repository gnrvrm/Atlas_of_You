namespace AtlasOfYou.Core;

public sealed class ComparisonResult
{
    public string CountryIso { get; init; } = string.Empty;

    public string CountryName { get; init; } = string.Empty;

    public string Sex { get; init; } = string.Empty;

    public int Age { get; init; }

    public double Bmi { get; init; }

    public string BmiBandLabel { get; init; } = string.Empty;

    public double? BmiReferencePercentile { get; init; }

    public string? BmiAgeGroup { get; init; }

    public int? BmiReferenceYear { get; init; }

    public double? CountryReferenceBmi { get; init; }

    public double? WorldReferenceBmi { get; init; }

    public BmiReference? BmiCountryReference { get; init; }

    public BmiReference? BmiWorldReference { get; init; }

    public double CountryMeanHeightCm { get; init; }

    public double WorldMeanHeightCm { get; init; }

    public double CountryHeightDifferenceCm { get; init; }

    public double WorldHeightDifferenceCm { get; init; }

    public double? CountryEstimatedWeightKg { get; init; }

    public double? WorldEstimatedWeightKg { get; init; }

    public double? CountryWeightDifferenceKg { get; init; }

    public double? WorldWeightDifferenceKg { get; init; }

    public double HeightPercentileCountry { get; init; }

    public double HeightPercentileWorld { get; init; }

    public string HeightReferenceLabel { get; init; } = string.Empty;

    public int? HeightReferenceBirthYear { get; init; }

    public int? HeightReferenceYear { get; init; }

    public double? Cohort1900MeanHeightCm { get; init; }

    public double? CohortDeltaVs1900Cm { get; init; }

    public TraitComparison? EyeColor { get; init; }

    public TraitComparison? HairColor { get; init; }

    public TraitComparison? HairEyeCombination { get; init; }

    public TraitComparison? HandPreference { get; init; }

    public TraitComparison? BloodGroup { get; init; }

    public IReadOnlyList<string> Notes { get; init; } = [];
}

public sealed class TraitComparison
{
    public string Trait { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public double Share { get; init; }

    public string Confidence { get; init; } = string.Empty;

    public string SourceId { get; init; } = string.Empty;

    public string Color { get; init; } = string.Empty;

    public string Scope { get; init; } = string.Empty;

    public string ScopeLabel { get; init; } = string.Empty;

    public string RarityLabel { get; init; } = string.Empty;

    public int? OneIn { get; init; }

    public string? Note { get; init; }
}
