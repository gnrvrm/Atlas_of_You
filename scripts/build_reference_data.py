from __future__ import annotations

import csv
import datetime as dt
import io
import json
import pathlib
import shutil
import subprocess
import time
import urllib.error
import urllib.request
import zipfile


ROOT = pathlib.Path(__file__).resolve().parents[1]
RAW_DIR = ROOT / "data" / "raw"
APP_DATA_DIR = ROOT / "src" / "AtlasOfYou.App" / "wwwroot" / "data"
MANIFEST_FILE = APP_DATA_DIR / "atlas-manifest.json"
COUNTRY_DATA_DIR = APP_DATA_DIR / "atlas-country"
LEGACY_REFERENCE_FILE = APP_DATA_DIR / "atlas-reference.json"

USER_AGENT = "AtlasOfYou/0.1 (+https://github.com/gnrvrm/Atlas_of_You)"
CHUNK_SIZE = 1024 * 256

URLS = {
    "height_cohort": "https://ncdrisc.org/downloads/height/NCD_RisC_eLife_2016_height_age18_countries.csv",
    "height_young_country": "https://ncdrisc.org/downloads/bmi-height-2020/height/all_countries/NCD_RisC_Lancet_2020_height_child_adolescent_country.zip",
    "height_young_world": "https://ncdrisc.org/downloads/bmi-height-2020/height/global/NCD_RisC_Lancet_2020_height_child_adolescent_global.csv",
    "bmi_female": "https://ncdrisc.org/downloads/bmi-2026/adult/NCD_RisC_Nature_2026_BMI_female_age_specific_country.zip",
    "bmi_male": "https://ncdrisc.org/downloads/bmi-2026/adult/NCD_RisC_Nature_2026_BMI_male_age_specific_country.zip",
}

RAW_FILES = {
    "height_cohort": RAW_DIR / "NCD_RisC_eLife_2016_height_age18_countries.csv",
    "height_young_country": RAW_DIR / "NCD_RisC_Lancet_2020_height_child_adolescent_country.zip",
    "height_young_world": RAW_DIR / "NCD_RisC_Lancet_2020_height_child_adolescent_global.csv",
    "bmi_female": RAW_DIR / "NCD_RisC_Nature_2026_BMI_female_age_specific_country.zip",
    "bmi_male": RAW_DIR / "NCD_RisC_Nature_2026_BMI_male_age_specific_country.zip",
}

BMI_COLUMNS = {
    "under185": "Prevalence of BMI<18.5 kg/m² (underweight)",
    "from185To20": "Prevalence of BMI 18.5 kg/m² to <20 kg/m²",
    "from20To25": "Prevalence of BMI 20 kg/m² to <25 kg/m²",
    "from25To30": "Prevalence of BMI 25 kg/m² to <30 kg/m²",
    "from30To35": "Prevalence of BMI 30 kg/m² to <35 kg/m²",
    "from35To40": "Prevalence of BMI 35 kg/m² to <40 kg/m²",
    "over40": "Prevalence of BMI >=40 kg/m² (morbid obesity)",
}

COUNTRY_NAME_OVERRIDES = {
    "TUR": "Türkiye",
}


def main() -> None:
    RAW_DIR.mkdir(parents=True, exist_ok=True)
    APP_DATA_DIR.mkdir(parents=True, exist_ok=True)

    for key, url in URLS.items():
        download_with_resume(url, RAW_FILES[key])

    height_cohorts, country_names = load_height_cohorts(RAW_FILES["height_cohort"])
    young_height = load_young_adult_height(
        RAW_FILES["height_young_country"],
        RAW_FILES["height_young_world"],
        country_names,
    )
    bmi_references = load_bmi_references(
        [RAW_FILES["bmi_female"], RAW_FILES["bmi_male"]]
    )

    for row in bmi_references:
        if row["countryIso"] != "WORLD":
            country_names.setdefault(row["countryIso"], display_country_name(row["countryIso"], row["countryName"]))

    countries = [
        {"iso": iso, "name": name}
        for iso, name in sorted(country_names.items(), key=lambda item: item[1])
        if iso != "WORLD"
    ]

    payload = {
        "schemaVersion": 1,
        "generatedAtUtc": dt.datetime.now(dt.UTC).replace(microsecond=0).isoformat(),
        "countries": countries,
        "heightCohorts": height_cohorts,
        "youngAdultHeight": young_height,
        "bmiReferences": bmi_references,
        "sources": [
            {
                "id": "height-cohort",
                "title": "A century of trends in adult human height, NCD-RisC, eLife 2016",
                "url": URLS["height_cohort"],
                "trust": "high",
            },
            {
                "id": "height-young-adult",
                "title": "Height trajectories of children and adolescents, NCD-RisC, Lancet 2020",
                "url": URLS["height_young_country"],
                "trust": "high",
            },
            {
                "id": "bmi-age-specific",
                "title": "Adult BMI age-specific country distributions, NCD-RisC, Nature 2026",
                "url": URLS["bmi_female"],
                "trust": "high",
            },
        ],
        "notes": [
            "Boy percentile değeri ortalama boy etrafında sabit popülasyon standart sapması varsayımı ile yaklaşık hesaplanır.",
            "BMI sonucu ideal kilo yorumu değildir; NCD-RisC referans dağılımındaki kategori konumunu gösterir.",
            "1996 sonrası doğum yılları için boy karşılaştırmasında 2019 yaş-19 genç yetişkin referansı kullanılır.",
        ],
    }

    write_app_payload(payload)
    print(f"Wrote {MANIFEST_FILE.relative_to(ROOT)}")
    print(f"Wrote {COUNTRY_DATA_DIR.relative_to(ROOT)}/*.json")
    print(f"Countries: {len(countries)}")
    print(f"Height cohorts: {len(height_cohorts)}")
    print(f"Young adult height references: {len(young_height)}")
    print(f"BMI references: {len(bmi_references)}")


def write_json(path: pathlib.Path, payload: dict) -> None:
    path.write_text(
        json.dumps(payload, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8",
    )


def write_app_payload(payload: dict) -> None:
    COUNTRY_DATA_DIR.mkdir(parents=True, exist_ok=True)

    for existing in COUNTRY_DATA_DIR.glob("*.json"):
        existing.unlink()

    manifest = {
        "schemaVersion": payload["schemaVersion"],
        "generatedAtUtc": payload["generatedAtUtc"],
        "countries": payload["countries"],
        "sources": payload["sources"],
        "notes": payload["notes"],
    }
    write_json(MANIFEST_FILE, manifest)
    write_json(LEGACY_REFERENCE_FILE, payload)

    country_isos = {country["iso"] for country in payload["countries"]}
    country_isos.add("WORLD")

    for iso in sorted(country_isos):
        country_payload = {
            "schemaVersion": payload["schemaVersion"],
            "generatedAtUtc": payload["generatedAtUtc"],
            "countries": [],
            "heightCohorts": [
                item for item in payload["heightCohorts"] if item["countryIso"] == iso
            ],
            "youngAdultHeight": [
                item for item in payload["youngAdultHeight"] if item["countryIso"] == iso
            ],
            "bmiReferences": [
                item for item in payload["bmiReferences"] if item["countryIso"] == iso
            ],
            "traitReferences": [],
            "sources": [],
            "notes": [],
        }
        write_json(COUNTRY_DATA_DIR / f"{iso}.json", country_payload)


def download_with_resume(url: str, destination: pathlib.Path, attempts: int = 8) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    marker = destination.with_name(destination.name + ".complete")
    if destination.exists() and marker.exists():
        print(f"Using cached {destination.name}")
        return

    curl = shutil.which("curl") or shutil.which("curl.exe")
    if curl:
        download_with_curl(curl, url, destination, marker, attempts)
        return

    for attempt in range(1, attempts + 1):
        resume_at = destination.stat().st_size if destination.exists() else 0
        request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
        if resume_at > 0:
            request.add_header("Range", f"bytes={resume_at}-")

        mode = "ab" if resume_at > 0 else "wb"
        try:
            with urllib.request.urlopen(request, timeout=120) as response:
                if resume_at > 0 and response.status != 206:
                    mode = "wb"
                with destination.open(mode + "") as output:
                    while True:
                        chunk = response.read(CHUNK_SIZE)
                        if not chunk:
                            marker.write_text(dt.datetime.now(dt.UTC).isoformat(), encoding="utf-8")
                            print(f"Downloaded {destination.name}")
                            return
                        output.write(chunk)
        except urllib.error.HTTPError as exc:
            if exc.code == 416:
                marker.write_text(dt.datetime.now(dt.UTC).isoformat(), encoding="utf-8")
                print(f"Already complete: {destination.name}")
                return
            if attempt == attempts:
                raise
        except (OSError, TimeoutError) as exc:
            if attempt == attempts:
                raise RuntimeError(f"Failed to download {url}") from exc

        time.sleep(min(2 * attempt, 12))


def download_with_curl(
    curl: str,
    url: str,
    destination: pathlib.Path,
    marker: pathlib.Path,
    attempts: int,
) -> None:
    for attempt in range(1, attempts + 1):
        result = subprocess.run(
            [
                curl,
                "-L",
                "--fail",
                "--retry",
                "2",
                "--retry-delay",
                "2",
                "-C",
                "-",
                "-o",
                str(destination),
                url,
            ],
            check=False,
        )
        if result.returncode == 0:
            marker.write_text(dt.datetime.now(dt.UTC).isoformat(), encoding="utf-8")
            print(f"Downloaded {destination.name}")
            return
        time.sleep(min(2 * attempt, 12))

    raise RuntimeError(f"Failed to download {url}")


def load_height_cohorts(path: pathlib.Path) -> tuple[list[dict], dict[str, str]]:
    rows: list[dict] = []
    country_names: dict[str, str] = {}

    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            sex = normalize_sex(row["Sex"])
            birth_year = int(row["Year of birth"])
            item = {
                "countryIso": row["ISO"],
                "countryName": display_country_name(row["ISO"], row["Country"]),
                "sex": sex,
                "birthYear": birth_year,
                "meanHeightCm": rounded(row["Mean height (cm)"]),
                "lower95Cm": rounded(row["Mean height lower 95% uncertainty interval (cm)"]),
                "upper95Cm": rounded(row["Mean height upper 95% uncertainty interval (cm)"]),
            }
            rows.append(item)
            country_names[item["countryIso"]] = item["countryName"]

    rows.sort(key=lambda item: (item["countryIso"], item["sex"], item["birthYear"]))
    return rows, country_names


def load_young_adult_height(
    country_zip: pathlib.Path,
    world_csv: pathlib.Path,
    country_names: dict[str, str],
) -> list[dict]:
    iso_by_country = {name: iso for iso, name in country_names.items()}
    iso_by_country.update({"Turkey": "TUR", "Turkiye": "TUR", "Türkiye": "TUR"})
    latest_by_key: dict[tuple[str, str], dict] = {}

    with zipfile.ZipFile(country_zip) as archive:
        entry = archive.infolist()[0]
        with archive.open(entry) as raw:
            reader = csv.DictReader(io.TextIOWrapper(raw, encoding="utf-8-sig", newline=""))
            for row in reader:
                if int(row["Age group"]) != 19:
                    continue
                iso = iso_by_country.get(row["Country"])
                if not iso:
                    continue
                item = height_young_item(
                    iso,
                    display_country_name(iso, row["Country"]),
                    normalize_sex(row["Sex"]),
                    int(row["Year"]),
                    19,
                    row,
                )
                keep_latest(latest_by_key, (iso, item["sex"]), item)

    with world_csv.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            if int(row["Age group"]) != 19:
                continue
            item = height_young_item(
                "WORLD",
                "World",
                normalize_sex(row["Sex"]),
                int(row["Year"]),
                19,
                row,
            )
            keep_latest(latest_by_key, ("WORLD", item["sex"]), item)

    return sorted(latest_by_key.values(), key=lambda item: (item["countryIso"], item["sex"]))


def height_young_item(
    iso: str,
    country: str,
    sex: str,
    year: int,
    age: int,
    row: dict[str, str],
) -> dict:
    return {
        "countryIso": iso,
        "countryName": country,
        "sex": sex,
        "year": year,
        "age": age,
        "meanHeightCm": rounded(row["Mean height"]),
        "lower95Cm": rounded(row["Mean height lower 95% uncertainty interval"]),
        "upper95Cm": rounded(row["Mean height upper 95% uncertainty interval"]),
        "standardError": rounded(row["Mean height standard error"]),
    }


def load_bmi_references(paths: list[pathlib.Path]) -> list[dict]:
    references: list[dict] = []

    for path in paths:
        latest_year = -1
        current_rows: list[dict] = []
        with zipfile.ZipFile(path) as archive:
            entry = archive.infolist()[0]
            with archive.open(entry) as raw:
                reader = csv.DictReader(io.TextIOWrapper(raw, encoding="utf-8-sig", newline=""))
                for row in reader:
                    year = int(row["Year"])
                    if year > latest_year:
                        latest_year = year
                        current_rows = []
                    if year == latest_year:
                        current_rows.append(row)

        for row in current_rows:
            country_name = row["Country/Region/World"]
            iso = row["ISO"] if row["ISO"] else ("WORLD" if country_name == "World" else "")
            if not iso:
                continue
            item = {
                "countryIso": iso,
                "countryName": display_country_name(iso, country_name),
                "sex": normalize_sex(row["Sex"]),
                "year": int(row["Year"]),
                "ageGroup": row["Age group"],
            }
            for json_key, csv_key in BMI_COLUMNS.items():
                item[json_key] = rounded(row[csv_key])
            references.append(item)

    references.sort(key=lambda item: (item["countryIso"], item["sex"], age_group_start(item["ageGroup"])))
    return references


def keep_latest(items: dict[tuple[str, str], dict], key: tuple[str, str], item: dict) -> None:
    existing = items.get(key)
    if existing is None or item["year"] > existing["year"]:
        items[key] = item


def normalize_sex(value: str) -> str:
    normalized = value.strip().lower()
    if normalized in {"men", "boys", "male"}:
        return "male"
    if normalized in {"women", "girls", "female"}:
        return "female"
    raise ValueError(f"Unsupported sex value: {value}")


def display_country_name(iso: str, source_name: str) -> str:
    return COUNTRY_NAME_OVERRIDES.get(iso, source_name)


def age_group_start(value: str) -> int:
    prefix = value.replace("plus", "+").split("-", 1)[0].replace("+", "")
    return int(prefix)


def rounded(value: str | float) -> float:
    return round(float(value), 6)


if __name__ == "__main__":
    main()
