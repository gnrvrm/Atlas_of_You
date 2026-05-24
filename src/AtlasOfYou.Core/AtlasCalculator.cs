namespace AtlasOfYou.Core;

public static class AtlasCalculator
{
    private const double MaleHeightSdCm = 7.0;
    private const double FemaleHeightSdCm = 6.5;

    public static ComparisonResult Analyze(ReferenceDataset data, ProfileInput input, DateOnly? today = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(input);

        var sex = NormalizeSex(input.Sex);
        var currentDate = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var age = currentDate.Year - input.BirthYear;
        if (age < 18)
        {
            throw new ArgumentException("Atlas of You MVP is designed for adults aged 18 or older.");
        }

        if (input.HeightCm <= 0 || input.WeightKg <= 0)
        {
            throw new ArgumentException("Height and weight must be positive values.");
        }

        var country = data.Countries.FirstOrDefault(item => item.Iso == input.CountryIso)
            ?? new CountryReference { Iso = input.CountryIso, Name = input.CountryIso };

        var heightReference = FindHeightReference(data, input.CountryIso, country.Name, sex, input.BirthYear);
        var worldHeightReference = FindWorldHeightReference(data, sex)
            ?? heightReference with { CountryIso = "WORLD", CountryName = "World" };
        var sd = sex == "male" ? MaleHeightSdCm : FemaleHeightSdCm;

        var cohort1900 = FindClosestCohort(data, input.CountryIso, sex, 1900);
        var selectedCohort = FindClosestCohort(data, input.CountryIso, sex, Math.Min(input.BirthYear, 1996));

        var bmi = CalculateBmi(input.HeightCm, input.WeightKg);
        var bmiCountryReference = FindBmiReference(data, input.CountryIso, sex, age)
            ?? FindBmiReference(data, "WORLD", sex, age);
        var bmiWorldReference = FindBmiReference(data, "WORLD", sex, age);
        double? bmiPercentile = bmiCountryReference is null
            ? null
            : EstimateBmiReferencePercentile(bmi, bmiCountryReference);
        double? countryReferenceBmi = bmiCountryReference is null
            ? null
            : EstimateReferenceBmi(bmiCountryReference);
        double? worldReferenceBmi = bmiWorldReference is null
            ? null
            : EstimateReferenceBmi(bmiWorldReference);
        double? countryEstimatedWeightKg = countryReferenceBmi is null
            ? null
            : EstimateWeightKg(heightReference.MeanHeightCm, countryReferenceBmi.Value);
        double? worldEstimatedWeightKg = worldReferenceBmi is null
            ? null
            : EstimateWeightKg(worldHeightReference.MeanHeightCm, worldReferenceBmi.Value);
        var eyeColor = BuildTraitComparison(data, "eyeColor", input.EyeColor, country.Iso, country.Name);
        var hairColor = BuildTraitComparison(data, "hairColor", input.HairColor, country.Iso, country.Name);
        var handPreference = BuildTraitComparison(data, "handPreference", input.HandPreference, country.Iso, country.Name);
        var bloodGroup = BuildTraitComparison(data, "bloodGroup", input.BloodGroup, country.Iso, country.Name);
        var hairEyeCombination = BuildHairEyeCombination(hairColor, eyeColor);

        var notes = new List<string>
        {
            "Boy percentile değeri, referans ortalamasının etrafında yaklaşık popülasyon dağılımı varsayımı kullanır.",
            "BMI sonucu ideal kilo yorumu değildir; aynı yaş/cinsiyet/ülke referans dağılımındaki konumu gösterir.",
            "Ortalama kilo görseli, BMI kategori dağılımından türetilmiş yaklaşık referans BMI ve ortalama boy ile hesaplanır.",
            "Göz rengi, saç rengi, el tercihi ve kan grubu yaklaşık prevalans sinyali olarak sunulur; tıbbi veya genetik yorum değildir.",
            "Saç + göz kombinasyonu, iki özelliğin bağımsız olduğu varsayımıyla hesaplanır.",
        };

        if (heightReference.HeightReferenceBirthYear != input.BirthYear)
        {
            notes.Add("Doğum yılı için birebir boy referansı bulunmadığında en yakın mevcut kohort veya yaş-19 referansı kullanılır.");
        }

        return new ComparisonResult
        {
            CountryIso = country.Iso,
            CountryName = country.Name,
            Sex = sex,
            Age = age,
            Bmi = Math.Round(bmi, 1),
            BmiBandLabel = GetBmiBandLabel(bmi),
            BmiReferencePercentile = bmiPercentile is null ? null : Math.Round(bmiPercentile.Value, 1),
            BmiAgeGroup = bmiCountryReference?.AgeGroup,
            BmiReferenceYear = bmiCountryReference?.Year,
            CountryReferenceBmi = countryReferenceBmi is null ? null : Math.Round(countryReferenceBmi.Value, 1),
            WorldReferenceBmi = worldReferenceBmi is null ? null : Math.Round(worldReferenceBmi.Value, 1),
            BmiCountryReference = bmiCountryReference,
            BmiWorldReference = bmiWorldReference,
            CountryMeanHeightCm = Math.Round(heightReference.MeanHeightCm, 1),
            WorldMeanHeightCm = Math.Round(worldHeightReference.MeanHeightCm, 1),
            CountryHeightDifferenceCm = Math.Round(input.HeightCm - heightReference.MeanHeightCm, 1),
            WorldHeightDifferenceCm = Math.Round(input.HeightCm - worldHeightReference.MeanHeightCm, 1),
            CountryEstimatedWeightKg = countryEstimatedWeightKg is null ? null : Math.Round(countryEstimatedWeightKg.Value, 1),
            WorldEstimatedWeightKg = worldEstimatedWeightKg is null ? null : Math.Round(worldEstimatedWeightKg.Value, 1),
            CountryWeightDifferenceKg = countryEstimatedWeightKg is null ? null : Math.Round(input.WeightKg - countryEstimatedWeightKg.Value, 1),
            WorldWeightDifferenceKg = worldEstimatedWeightKg is null ? null : Math.Round(input.WeightKg - worldEstimatedWeightKg.Value, 1),
            HeightPercentileCountry = Math.Round(EstimateNormalPercentile(input.HeightCm, heightReference.MeanHeightCm, sd), 1),
            HeightPercentileWorld = Math.Round(EstimateNormalPercentile(input.HeightCm, worldHeightReference.MeanHeightCm, sd), 1),
            HeightReferenceLabel = heightReference.Label,
            HeightReferenceBirthYear = heightReference.HeightReferenceBirthYear,
            HeightReferenceYear = heightReference.HeightReferenceYear,
            Cohort1900MeanHeightCm = cohort1900 is null ? null : Math.Round(cohort1900.MeanHeightCm, 1),
            CohortDeltaVs1900Cm = cohort1900 is null || selectedCohort is null
                ? null
                : Math.Round(selectedCohort.MeanHeightCm - cohort1900.MeanHeightCm, 1),
            EyeColor = eyeColor,
            HairColor = hairColor,
            HairEyeCombination = hairEyeCombination,
            HandPreference = handPreference,
            BloodGroup = bloodGroup,
            Notes = notes,
        };
    }

    public static double CalculateBmi(double heightCm, double weightKg)
    {
        var heightMeters = heightCm / 100.0;
        return weightKg / (heightMeters * heightMeters);
    }

    public static double EstimateWeightKg(double heightCm, double bmi)
    {
        var heightMeters = heightCm / 100.0;
        return bmi * heightMeters * heightMeters;
    }

    public static bool AgeGroupContains(string ageGroup, int age)
    {
        var normalized = ageGroup.Replace("plus", "+", StringComparison.OrdinalIgnoreCase);

        if (normalized.EndsWith('+'))
        {
            return age >= int.Parse(normalized.TrimEnd('+'));
        }

        var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        return age >= int.Parse(parts[0]) && age <= int.Parse(parts[1]);
    }

    public static double EstimateNormalPercentile(double value, double mean, double standardDeviation)
    {
        if (standardDeviation <= 0)
        {
            return 50;
        }

        var z = (value - mean) / standardDeviation;
        return Math.Clamp(NormalCdf(z) * 100.0, 0.1, 99.9);
    }

    public static double EstimateBmiReferencePercentile(double bmi, BmiReference reference)
    {
        var bands = GetBmiBands(reference);

        var total = bands.Sum(item => item.Share);
        if (total <= 0)
        {
            return 50;
        }

        var cumulative = 0.0;
        foreach (var band in bands)
        {
            if (bmi < band.Upper || band == bands[^1])
            {
                var position = Math.Clamp((bmi - band.Lower) / (band.Upper - band.Lower), 0, 1);
                return Math.Clamp((cumulative + (band.Share * position)) / total * 100.0, 0.1, 99.9);
            }

            cumulative += band.Share;
        }

        return 99.9;
    }

    public static double EstimateReferenceBmi(BmiReference reference)
    {
        var bands = GetBmiBands(reference);
        var total = bands.Sum(item => item.Share);
        if (total <= 0)
        {
            return 24.0;
        }

        return bands.Sum(item => item.Midpoint * item.Share) / total;
    }

    public static string GetBmiBandLabel(double bmi)
    {
        return bmi switch
        {
            < 18.5 => "18.5 altı",
            < 20 => "18.5-20",
            < 25 => "20-25",
            < 30 => "25-30",
            < 35 => "30-35",
            < 40 => "35-40",
            _ => "40 ve üzeri",
        };
    }

    public static TraitComparison? BuildTraitComparison(
        ReferenceDataset data,
        string trait,
        string value,
        string countryIso,
        string countryName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var reference = FindTraitReference(data, trait, value, countryIso);
        if (reference is null)
        {
            return null;
        }

        return new TraitComparison
        {
            Trait = reference.Trait,
            Value = reference.Value,
            Label = reference.Label,
            Share = Math.Round(reference.Share, 4),
            Confidence = reference.Confidence,
            SourceId = reference.SourceId,
            Color = reference.Color,
            Scope = reference.Scope,
            ScopeLabel = BuildScopeLabel(reference, countryName),
            RarityLabel = GetRarityLabel(reference.Share),
            OneIn = EstimateOneIn(reference.Share),
        };
    }

    public static TraitComparison? BuildHairEyeCombination(TraitComparison? hairColor, TraitComparison? eyeColor)
    {
        if (hairColor is null || eyeColor is null)
        {
            return null;
        }

        var share = Math.Clamp(hairColor.Share * eyeColor.Share, 0, 1);
        return new TraitComparison
        {
            Trait = "hairEyeCombination",
            Value = $"{hairColor.Value}+{eyeColor.Value}",
            Label = $"{hairColor.Label} saç + {eyeColor.Label.ToLowerInvariant()} göz",
            Share = Math.Round(share, 5),
            Confidence = "dusuk",
            SourceId = $"{hairColor.SourceId}+{eyeColor.SourceId}",
            Color = hairColor.Color,
            Scope = "model",
            ScopeLabel = "Bağımsızlık varsayımı",
            RarityLabel = GetRarityLabel(share),
            OneIn = EstimateOneIn(share),
            Note = "Bu değer saç ve göz renginin birbirinden bağımsız dağıldığı varsayımıyla üretilir.",
        };
    }

    private static HeightReferenceChoice FindHeightReference(
        ReferenceDataset data,
        string countryIso,
        string countryName,
        string sex,
        int birthYear)
    {
        var cohort = birthYear <= 1996
            ? FindClosestCohort(data, countryIso, sex, birthYear)
            : null;

        if (cohort is not null)
        {
            return new HeightReferenceChoice(
                cohort.CountryIso,
                cohort.CountryName,
                cohort.MeanHeightCm,
                $"Doğum kohortu {cohort.BirthYear}",
                cohort.BirthYear,
                null);
        }

        var youngAdult = data.YoungAdultHeight.FirstOrDefault(item =>
            item.CountryIso == countryIso &&
            item.Sex == sex);

        if (youngAdult is not null)
        {
            return new HeightReferenceChoice(
                youngAdult.CountryIso,
                youngAdult.CountryName,
                youngAdult.MeanHeightCm,
                $"{youngAdult.Year} yaş-{youngAdult.Age} referansı",
                null,
                youngAdult.Year);
        }

        var fallback = FindClosestCohort(data, countryIso, sex, 1996);
        if (fallback is not null)
        {
            return new HeightReferenceChoice(
                fallback.CountryIso,
                fallback.CountryName,
                fallback.MeanHeightCm,
                $"En yakın doğum kohortu {fallback.BirthYear}",
                fallback.BirthYear,
                null);
        }

        var worldFallback = FindWorldHeightReference(data, sex);
        if (worldFallback is not null)
        {
            return worldFallback;
        }

        throw new InvalidOperationException($"No height reference found for {countryName} / {sex}.");
    }

    private static HeightReferenceChoice? FindWorldHeightReference(ReferenceDataset data, string sex)
    {
        var world = data.YoungAdultHeight.FirstOrDefault(item =>
            item.CountryIso == "WORLD" &&
            item.Sex == sex);

        return world is null
            ? null
            : new HeightReferenceChoice(
                "WORLD",
                "World",
                world.MeanHeightCm,
                $"{world.Year} dünya yaş-{world.Age} referansı",
                null,
                world.Year);
    }

    private static HeightCohortReference? FindClosestCohort(ReferenceDataset data, string countryIso, string sex, int birthYear)
    {
        return data.HeightCohorts
            .Where(item => item.CountryIso == countryIso && item.Sex == sex)
            .MinBy(item => Math.Abs(item.BirthYear - birthYear));
    }

    private static BmiReference? FindBmiReference(ReferenceDataset data, string countryIso, string sex, int age)
    {
        return data.BmiReferences.FirstOrDefault(item =>
            item.CountryIso == countryIso &&
            item.Sex == sex &&
            AgeGroupContains(item.AgeGroup, age));
    }

    private static TraitReference? FindTraitReference(ReferenceDataset data, string trait, string value, string countryIso)
    {
        var normalizedTrait = trait.Trim();
        var normalizedValue = value.Trim();

        return data.TraitReferences.FirstOrDefault(item =>
                StringComparer.OrdinalIgnoreCase.Equals(item.Trait, normalizedTrait) &&
                StringComparer.OrdinalIgnoreCase.Equals(item.Value, normalizedValue) &&
                StringComparer.OrdinalIgnoreCase.Equals(item.CountryIso, countryIso) &&
                StringComparer.OrdinalIgnoreCase.Equals(item.Scope, "country"))
            ?? data.TraitReferences.FirstOrDefault(item =>
                StringComparer.OrdinalIgnoreCase.Equals(item.Trait, normalizedTrait) &&
                StringComparer.OrdinalIgnoreCase.Equals(item.Value, normalizedValue) &&
                StringComparer.OrdinalIgnoreCase.Equals(item.CountryIso, "WORLD"));
    }

    private static string BuildScopeLabel(TraitReference reference, string countryName)
    {
        return StringComparer.OrdinalIgnoreCase.Equals(reference.Scope, "country")
            ? $"{countryName} yaklaşık referansı"
            : "Dünya/genel yaklaşık referansı";
    }

    private static string GetRarityLabel(double share)
    {
        return share switch
        {
            >= 0.3 => "Çok yaygın",
            >= 0.1 => "Yaygın",
            >= 0.03 => "Daha az yaygın",
            >= 0.01 => "Nadir",
            _ => "Çok nadir",
        };
    }

    private static int? EstimateOneIn(double share)
    {
        return share <= 0
            ? null
            : Math.Max(1, (int)Math.Round(1.0 / share, MidpointRounding.AwayFromZero));
    }

    private static string NormalizeSex(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "male" or "men" or "boys" or "erkek" => "male",
            "female" or "women" or "girls" or "kadin" or "kadın" => "female",
            _ => throw new ArgumentException($"Unsupported sex value: {value}"),
        };
    }

    private static double NormalCdf(double z)
    {
        // Abramowitz and Stegun 7.1.26 approximation, accurate enough for UI percentiles.
        var sign = z < 0 ? -1 : 1;
        var x = Math.Abs(z) / Math.Sqrt(2.0);
        var t = 1.0 / (1.0 + 0.3275911 * x);
        var erf = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);
        return 0.5 * (1.0 + (sign * erf));
    }

    private static BmiBand[] GetBmiBands(BmiReference reference)
    {
        return
        [
            new BmiBand(14.0, 18.5, reference.Under185),
            new BmiBand(18.5, 20.0, reference.From185To20),
            new BmiBand(20.0, 25.0, reference.From20To25),
            new BmiBand(25.0, 30.0, reference.From25To30),
            new BmiBand(30.0, 35.0, reference.From30To35),
            new BmiBand(35.0, 40.0, reference.From35To40),
            new BmiBand(40.0, 50.0, reference.Over40),
        ];
    }

    private sealed record BmiBand(double Lower, double Upper, double Share)
    {
        public double Midpoint => (Lower + Upper) / 2.0;
    }

    private sealed record HeightReferenceChoice(
        string CountryIso,
        string CountryName,
        double MeanHeightCm,
        string Label,
        int? HeightReferenceBirthYear,
        int? HeightReferenceYear);
}
