namespace AtlasOfYou.Core;

public sealed class ProfileInput
{
    public string CountryIso { get; init; } = string.Empty;

    public string Sex { get; init; } = string.Empty;

    public int BirthYear { get; init; }

    public double HeightCm { get; init; }

    public double WeightKg { get; init; }

    public string EyeColor { get; init; } = string.Empty;

    public string HairColor { get; init; } = string.Empty;

    public string HandPreference { get; init; } = string.Empty;

    public string BloodGroup { get; init; } = string.Empty;
}
