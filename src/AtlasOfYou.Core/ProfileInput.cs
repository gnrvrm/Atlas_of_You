namespace AtlasOfYou.Core;

public sealed class ProfileInput
{
    public string CountryIso { get; init; } = string.Empty;

    public string Sex { get; init; } = string.Empty;

    public int BirthYear { get; init; }

    public double HeightCm { get; init; }

    public double WeightKg { get; init; }
}
