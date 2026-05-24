namespace AtlasOfYou.Core;

public sealed class ReferenceDataset
{
    public int SchemaVersion { get; set; }

    public DateTimeOffset GeneratedAtUtc { get; set; }

    public List<CountryReference> Countries { get; set; } = [];

    public List<HeightCohortReference> HeightCohorts { get; set; } = [];

    public List<YoungAdultHeightReference> YoungAdultHeight { get; set; } = [];

    public List<BmiReference> BmiReferences { get; set; } = [];

    public List<TraitReference> TraitReferences { get; set; } = [];

    public List<SourceInfo> Sources { get; set; } = [];

    public List<string> Notes { get; set; } = [];
}

public sealed class CountryReference
{
    public string Iso { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public sealed class HeightCohortReference
{
    public string CountryIso { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public string Sex { get; set; } = string.Empty;

    public int BirthYear { get; set; }

    public double MeanHeightCm { get; set; }

    public double Lower95Cm { get; set; }

    public double Upper95Cm { get; set; }
}

public sealed class YoungAdultHeightReference
{
    public string CountryIso { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public string Sex { get; set; } = string.Empty;

    public int Year { get; set; }

    public int Age { get; set; }

    public double MeanHeightCm { get; set; }

    public double Lower95Cm { get; set; }

    public double Upper95Cm { get; set; }

    public double StandardError { get; set; }
}

public sealed class BmiReference
{
    public string CountryIso { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public string Sex { get; set; } = string.Empty;

    public int Year { get; set; }

    public string AgeGroup { get; set; } = string.Empty;

    public double Under185 { get; set; }

    public double From185To20 { get; set; }

    public double From20To25 { get; set; }

    public double From25To30 { get; set; }

    public double From30To35 { get; set; }

    public double From35To40 { get; set; }

    public double Over40 { get; set; }
}

public sealed class SourceInfo
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Trust { get; set; } = string.Empty;
}

public sealed class TraitReference
{
    public string Trait { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string CountryIso { get; set; } = string.Empty;

    public double Share { get; set; }

    public string Confidence { get; set; } = string.Empty;

    public string SourceId { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;
}
