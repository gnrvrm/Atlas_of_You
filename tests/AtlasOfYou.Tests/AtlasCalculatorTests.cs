using AtlasOfYou.Core;

namespace AtlasOfYou.Tests;

public sealed class AtlasCalculatorTests
{
    [Fact]
    public void CalculateBmi_UsesMetricUnits()
    {
        var bmi = AtlasCalculator.CalculateBmi(180, 80);

        Assert.Equal(24.7, Math.Round(bmi, 1));
    }

    [Fact]
    public void EstimateWeightKg_ConvertsReferenceBmiWithHeight()
    {
        var weight = AtlasCalculator.EstimateWeightKg(174, 26);

        Assert.Equal(78.7, Math.Round(weight, 1));
    }

    [Theory]
    [InlineData("18-19", 18, true)]
    [InlineData("18-19", 20, false)]
    [InlineData("85+", 91, true)]
    [InlineData("85plus", 91, true)]
    public void AgeGroupContains_HandlesRangesAndOpenEndedGroups(string group, int age, bool expected)
    {
        Assert.Equal(expected, AtlasCalculator.AgeGroupContains(group, age));
    }

    [Fact]
    public void Analyze_ReturnsHeightAndBmiComparisons()
    {
        var dataset = BuildDataset();
        var input = new ProfileInput
        {
            CountryIso = "TUR",
            Sex = "male",
            BirthYear = 1990,
            HeightCm = 180,
            WeightKg = 80,
            EyeColor = "green",
            HairColor = "red",
            HandPreference = "left",
            BloodGroup = "a_positive",
        };

        var result = AtlasCalculator.Analyze(dataset, input, new DateOnly(2026, 5, 17));

        Assert.Equal(36, result.Age);
        Assert.Equal(24.7, result.Bmi);
        Assert.Equal(6, result.CountryHeightDifferenceCm);
        Assert.Equal(9, result.WorldHeightDifferenceCm);
        Assert.True(result.HeightPercentileCountry > 75);
        Assert.Equal("35-39", result.BmiAgeGroup);
        Assert.True(result.BmiReferencePercentile is > 40 and < 70);
        Assert.Equal(26.2, result.CountryReferenceBmi);
        Assert.Equal(79.3, result.CountryEstimatedWeightKg);
        Assert.Equal(0.7, result.CountryWeightDifferenceKg);
        Assert.Equal(7, result.CohortDeltaVs1900Cm);
        Assert.Equal("Yeşil", result.EyeColor?.Label);
        Assert.Equal("Çok nadir", result.HairEyeCombination?.RarityLabel);
        Assert.Equal(3333, result.HairEyeCombination?.OneIn);
        Assert.Equal("Solak", result.HandPreference?.Label);
        Assert.Equal("Türkiye yaklaşık referansı", result.BloodGroup?.ScopeLabel);
        Assert.Equal(0.383, result.BloodGroup?.Share);
    }

    private static ReferenceDataset BuildDataset()
    {
        return new ReferenceDataset
        {
            Countries =
            [
                new CountryReference { Iso = "TUR", Name = "Türkiye" },
            ],
            HeightCohorts =
            [
                new HeightCohortReference
                {
                    CountryIso = "TUR",
                    CountryName = "Türkiye",
                    Sex = "male",
                    BirthYear = 1900,
                    MeanHeightCm = 167,
                },
                new HeightCohortReference
                {
                    CountryIso = "TUR",
                    CountryName = "Türkiye",
                    Sex = "male",
                    BirthYear = 1990,
                    MeanHeightCm = 174,
                },
            ],
            YoungAdultHeight =
            [
                new YoungAdultHeightReference
                {
                    CountryIso = "WORLD",
                    CountryName = "World",
                    Sex = "male",
                    Year = 2019,
                    Age = 19,
                    MeanHeightCm = 171,
                },
            ],
            BmiReferences =
            [
                new BmiReference
                {
                    CountryIso = "TUR",
                    CountryName = "Türkiye",
                    Sex = "male",
                    Year = 2024,
                    AgeGroup = "35-39",
                    Under185 = 0.04,
                    From185To20 = 0.06,
                    From20To25 = 0.36,
                    From25To30 = 0.34,
                    From30To35 = 0.14,
                    From35To40 = 0.04,
                    Over40 = 0.02,
                },
            ],
            TraitReferences =
            [
                new TraitReference
                {
                    Trait = "eyeColor",
                    Value = "green",
                    Label = "Yeşil",
                    Scope = "world",
                    CountryIso = "WORLD",
                    Share = 0.02,
                    Confidence = "orta",
                    SourceId = "eye",
                    Color = "#4d7c45",
                },
                new TraitReference
                {
                    Trait = "hairColor",
                    Value = "red",
                    Label = "Kızıl",
                    Scope = "world",
                    CountryIso = "WORLD",
                    Share = 0.015,
                    Confidence = "dusuk",
                    SourceId = "hair",
                    Color = "#b45325",
                },
                new TraitReference
                {
                    Trait = "handPreference",
                    Value = "left",
                    Label = "Solak",
                    Scope = "world",
                    CountryIso = "WORLD",
                    Share = 0.106,
                    Confidence = "orta",
                    SourceId = "hand",
                    Color = "#285e8f",
                },
                new TraitReference
                {
                    Trait = "bloodGroup",
                    Value = "a_positive",
                    Label = "A Rh+",
                    Scope = "world",
                    CountryIso = "WORLD",
                    Share = 0.28,
                    Confidence = "dusuk",
                    SourceId = "blood",
                    Color = "#0f766e",
                },
                new TraitReference
                {
                    Trait = "bloodGroup",
                    Value = "a_positive",
                    Label = "A Rh+",
                    Scope = "country",
                    CountryIso = "TUR",
                    Share = 0.383,
                    Confidence = "dusuk",
                    SourceId = "blood",
                    Color = "#0f766e",
                },
            ],
        };
    }
}
