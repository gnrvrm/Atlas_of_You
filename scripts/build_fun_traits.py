from __future__ import annotations

import datetime as dt
import json
import pathlib


ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT_FILE = ROOT / "src" / "AtlasOfYou.App" / "wwwroot" / "data" / "fun-traits.json"


SOURCES = [
    {
        "id": "eye-color-mnt",
        "title": "Eye colors: most common and percentages, Medical News Today",
        "url": "https://www.medicalnewstoday.com/articles/eye-color-percentage",
        "trust": "medium",
    },
    {
        "id": "handedness-meta-analysis",
        "title": "Human handedness: a meta-analysis, Psychological Bulletin 2020",
        "url": "https://research-repository.st-andrews.ac.uk/handle/10023/19889",
        "trust": "medium",
    },
    {
        "id": "blood-type-country-distribution",
        "title": "ABO and Rh blood type distribution by country",
        "url": "https://en.wikipedia.org/wiki/Blood_type_distribution_by_country",
        "trust": "low",
    },
    {
        "id": "human-hair-color-taxonomy",
        "title": "Human hair color natural categories",
        "url": "https://en.wikipedia.org/wiki/Human_hair_color",
        "trust": "low",
    },
]


TRAITS = [
    # Eye color, global approximate priors.
    ("eyeColor", "brown", "Kahverengi", "world", "WORLD", 0.79, "orta", "eye-color-mnt", "#5a3825"),
    ("eyeColor", "blue", "Mavi", "world", "WORLD", 0.09, "orta", "eye-color-mnt", "#4d8bc6"),
    ("eyeColor", "hazel", "Ela", "world", "WORLD", 0.05, "orta", "eye-color-mnt", "#8a6b32"),
    ("eyeColor", "amber", "Kehribar", "world", "WORLD", 0.04, "dusuk", "eye-color-mnt", "#b7791f"),
    ("eyeColor", "green", "Yeşil", "world", "WORLD", 0.02, "orta", "eye-color-mnt", "#4d7c45"),
    ("eyeColor", "gray", "Gri", "world", "WORLD", 0.01, "dusuk", "eye-color-mnt", "#7d8790"),

    # Hair color, global approximate priors. Natural categories are stable; exact global shares are coarse.
    ("hairColor", "black", "Siyah / koyu kahve", "world", "WORLD", 0.78, "dusuk", "human-hair-color-taxonomy", "#181512"),
    ("hairColor", "brown", "Kahverengi", "world", "WORLD", 0.11, "dusuk", "human-hair-color-taxonomy", "#6b4226"),
    ("hairColor", "blond", "Sarışın", "world", "WORLD", 0.03, "dusuk", "human-hair-color-taxonomy", "#d6ad5b"),
    ("hairColor", "red", "Kızıl", "world", "WORLD", 0.015, "dusuk", "human-hair-color-taxonomy", "#b45325"),
    ("hairColor", "gray_white", "Gri / beyaz", "world", "WORLD", 0.02, "dusuk", "human-hair-color-taxonomy", "#c8c8bd"),
    ("hairColor", "other", "Diğer / karışık", "world", "WORLD", 0.045, "dusuk", "human-hair-color-taxonomy", "#8b7d6b"),

    # Hand preference, global approximate priors.
    ("handPreference", "right", "Sağlak", "world", "WORLD", 0.884, "orta", "handedness-meta-analysis", "#0f766e"),
    ("handPreference", "left", "Solak", "world", "WORLD", 0.106, "orta", "handedness-meta-analysis", "#285e8f"),
    ("handPreference", "ambidextrous", "İki elli", "world", "WORLD", 0.01, "dusuk", "handedness-meta-analysis", "#b45309"),

    # Global fallback blood group priors; country rows override when present.
    ("bloodGroup", "o_positive", "0 Rh+", "world", "WORLD", 0.37, "dusuk", "blood-type-country-distribution", "#a43d55"),
    ("bloodGroup", "a_positive", "A Rh+", "world", "WORLD", 0.28, "dusuk", "blood-type-country-distribution", "#0f766e"),
    ("bloodGroup", "b_positive", "B Rh+", "world", "WORLD", 0.20, "dusuk", "blood-type-country-distribution", "#285e8f"),
    ("bloodGroup", "ab_positive", "AB Rh+", "world", "WORLD", 0.05, "dusuk", "blood-type-country-distribution", "#b45309"),
    ("bloodGroup", "o_negative", "0 Rh-", "world", "WORLD", 0.04, "dusuk", "blood-type-country-distribution", "#7a2336"),
    ("bloodGroup", "a_negative", "A Rh-", "world", "WORLD", 0.03, "dusuk", "blood-type-country-distribution", "#0f4f4b"),
    ("bloodGroup", "b_negative", "B Rh-", "world", "WORLD", 0.02, "dusuk", "blood-type-country-distribution", "#1d4f77"),
    ("bloodGroup", "ab_negative", "AB Rh-", "world", "WORLD", 0.01, "dusuk", "blood-type-country-distribution", "#7c2d12"),

    # Turkey blood group distribution, approximate country-level row.
    ("bloodGroup", "o_positive", "0 Rh+", "country", "TUR", 0.294, "dusuk", "blood-type-country-distribution", "#a43d55"),
    ("bloodGroup", "a_positive", "A Rh+", "country", "TUR", 0.383, "dusuk", "blood-type-country-distribution", "#0f766e"),
    ("bloodGroup", "b_positive", "B Rh+", "country", "TUR", 0.132, "dusuk", "blood-type-country-distribution", "#285e8f"),
    ("bloodGroup", "ab_positive", "AB Rh+", "country", "TUR", 0.064, "dusuk", "blood-type-country-distribution", "#b45309"),
    ("bloodGroup", "o_negative", "0 Rh-", "country", "TUR", 0.044, "dusuk", "blood-type-country-distribution", "#7a2336"),
    ("bloodGroup", "a_negative", "A Rh-", "country", "TUR", 0.055, "dusuk", "blood-type-country-distribution", "#0f4f4b"),
    ("bloodGroup", "b_negative", "B Rh-", "country", "TUR", 0.021, "dusuk", "blood-type-country-distribution", "#1d4f77"),
    ("bloodGroup", "ab_negative", "AB Rh-", "country", "TUR", 0.007, "dusuk", "blood-type-country-distribution", "#7c2d12"),
]


def main() -> None:
    OUT_FILE.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "schemaVersion": 2,
        "generatedAtUtc": dt.datetime.now(dt.UTC).replace(microsecond=0).isoformat(),
        "traitReferences": [
            {
                "trait": trait,
                "value": value,
                "label": label,
                "scope": scope,
                "countryIso": country_iso,
                "share": share,
                "confidence": confidence,
                "sourceId": source_id,
                "color": color,
            }
            for trait, value, label, scope, country_iso, share, confidence, source_id, color in TRAITS
        ],
        "sources": SOURCES,
        "notes": [
            "Göz rengi, saç rengi, el tercihi ve kan grubu alanları yaklaşık prevalans sinyali olarak sunulur; percentile veya tıbbi/genetik yorum değildir.",
            "Saç + göz kombinasyonu, iki özelliğin bağımsız olduğu varsayımıyla çarpılarak hesaplanır.",
            "Kan grubu verileri kaynaklar arasında dağınık olduğu için düşük güven etiketiyle gösterilir.",
        ],
    }
    OUT_FILE.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {OUT_FILE.relative_to(ROOT)}")
    print(f"Trait references: {len(payload['traitReferences'])}")


if __name__ == "__main__":
    main()
