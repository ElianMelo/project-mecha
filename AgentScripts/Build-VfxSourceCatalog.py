"""Inventory supplied VFX, including unopened unitypackages, without importing them.

Run with the bundled Python (Pillow and pypdf). This is an authoring inventory,
not an automated test. It never edits vendor assets or Unity metadata.
"""
from __future__ import annotations

import argparse
from collections import Counter
import csv
import hashlib
from io import BytesIO
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import tarfile

from PIL import Image
from pypdf import PdfReader

ROOT = Path(__file__).resolve().parents[1]
LIBRARIES = ("Assets/GabrielAguiarProductions", "Assets/JMO Assets", "Assets/PixPlays", "Assets/Hovl Studio")
IMPORT_COMMITS = ("2072054", "a258685")
SOURCE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".tga", ".tif", ".tiff", ".psd", ".exr", ".hdr",
                     ".mat", ".shader", ".shadergraph", ".shadersubgraph", ".hlsl", ".cginc",
                     ".cfxrshader", ".fbx", ".obj", ".mesh", ".vfxoperator", ".vfxblock"}
TEXT_EXTENSIONS = {".prefab", ".unity", ".mat", ".asset", ".shader", ".shadergraph", ".shadersubgraph",
                   ".hlsl", ".cginc", ".cfxrshader", ".cs", ".txt", ".md", ".html", ".json",
                   ".vfx", ".vfxoperator", ".vfxblock", ".anim", ".controller", ".playable", ".asmdef"}
FIELDS = ["Package", "Container", "Path", "Role", "Kind", "Guid", "Bytes", "Sha256", "Dimensions",
          "Shader", "DependencyGuids", "Summary", "Tags", "Availability"]


def git(*args: str) -> str:
    return subprocess.check_output(["git", "-c", f"safe.directory={ROOT.as_posix()}", "--no-optional-locks",
                                    "-C", str(ROOT), *args], encoding="utf-8")


def package(path: str) -> str:
    parts = path.split("/")
    if len(parts) < 3:
        return path
    if parts[1] == "GabrielAguiarProductions":
        return "GabrielAguiarProductions / Ultra Mega Pack"
    return "/".join(parts[1:3])


def role(path: str) -> str:
    base = re.sub(r"\.meta$", "", path)
    # Demo lightmaps, probes and display graphics are not effect building blocks.
    if re.search(r"/(demo[^/]*|documentation[^/]*|screenshots?)/|lightmap|reflectionprobe|lightingdata", base, re.I):
        return "Reference"
    return "VFXSource" if PurePosixPath(base).suffix.lower() in SOURCE_EXTENSIONS else "Reference"


def inspect_blob(data: bytes, path: str) -> dict:
    suffix = PurePosixPath(path).suffix.lower()
    result = {"Bytes": len(data), "Sha256": hashlib.sha256(data).hexdigest(), "Dimensions": "",
              "Shader": "", "DependencyGuids": "", "Summary": "", "Tags": ""}
    if suffix in {".png", ".jpg", ".jpeg", ".tga", ".tif", ".tiff", ".psd"}:
        try:
            with Image.open(BytesIO(data)) as img:
                result["Dimensions"] = f"{img.width}x{img.height};{img.mode}"
        except (OSError, ValueError):
            result["Summary"] = "Image header not readable by Pillow; inspect in Unity."
    if suffix in TEXT_EXTENSIONS:
        text = data.decode("utf-8-sig", errors="replace")
        refs = sorted(set(re.findall(r"(?:guid: ?|\"guid\"\s*:\s*\")([a-f0-9]{32})", text)))
        result["DependencyGuids"] = ";".join(refs)
        names = list(dict.fromkeys(re.findall(r"^\s*m_Name: ([^\r\n]+)", text, re.M)))
        counts = Counter(re.findall(r"^--- !u!(\d+)", text, re.M))
        detail = [f"{counts[c]} {label}" for c, label in (("198", "ParticleSystems"), ("199", "ParticleRenderers"),
                  ("108", "Lights"), ("120", "Trails"), ("54", "Rigidbodies"), ("114", "Behaviours")) if counts[c]]
        result["Summary"] = "; ".join(detail + (["Names: " + " | ".join(names[:24])] if names else []))
        shader_name = re.search(r'Shader\s+"([^"\r\n]+)"', text)
        shader_guid = re.search(r"m_Shader:.*guid: ([a-f0-9]{32})", text)
        result["Shader"] = shader_name.group(1) if shader_name else (shader_guid.group(1) if shader_guid else "")
        tags = []
        for tag, pattern in {
            "custom-streams": r"Custom1|Custom2|CustomVertex|TEXCOORD|customData",
            "dissolve": r"dissolv|cutoff|AlphaClip", "distortion": r"distort|GrabPass|OpaqueTexture",
            "flipbook": r"flipbook|textureSheet|UVModule", "collision": r"OnCollision|CollisionModule|collisionModule",
            "subemitters": r"SubModule|subEmitters|SubEmitter", "light": r"LightModule|AnimatedLight|lightSourse",
            "builtin-only-risk": r"GrabPass|UnityCG\.cginc|CGPROGRAM", "urp": r"UniversalPipeline|Universal Render Pipeline",
            "hdrp": r"HDRenderPipeline|HighDefinition|HDRP", "pooling": r"[Pp]ool|OnEnable|OnDisable",
        }.items():
            if re.search(pattern, text):
                tags.append(tag)
        result["Tags"] = ";".join(tags)
    return result


def row(path: str, container: str, data: bytes, meta: bytes = b"", guid: str = "") -> dict:
    match = re.search(rb"^guid: ([a-f0-9]{32})", meta, re.M)
    return {"Package": package(path), "Container": container, "Path": path, "Role": role(path),
            "Kind": "Metadata" if path.endswith(".meta") else (PurePosixPath(path).suffix.lower().lstrip(".") or "Folder"),
            "Guid": match.group(1).decode() if match else guid,
            **inspect_blob(data, path),
            "Availability": "Archive only; not imported" if container else "On disk"}


def scan_archive(path: Path) -> tuple[list[dict], dict]:
    records = {}
    members = 0
    unusual = []
    package_manifest = None
    # Streaming avoids repeated decompression of the 600 MB archives. No member is extracted to disk.
    with tarfile.open(path, "r|gz") as archive:
        for member in archive:
            members += 1
            if member.isdir():
                continue
            parts = PurePosixPath(member.name).parts
            if member.isfile() and parts == ("packagemanagermanifest", "asset"):
                package_manifest = archive.extractfile(member).read().decode("utf-8-sig")
                continue
            if not member.isfile() or len(parts) != 2 or not re.fullmatch(r"[0-9a-f]{32}", parts[0]):
                unusual.append(member.name)
                continue
            guid, kind = parts
            if kind not in {"asset", "asset.meta", "pathname", "preview.png"}:
                unusual.append(member.name)
                continue
            record = records.setdefault(guid, {})
            data = archive.extractfile(member).read()
            if kind == "pathname":
                record["path"] = data.decode("utf-8-sig").strip().replace("\\", "/")
            elif kind != "preview.png":
                record[kind] = data
            # Unity writes asset + metadata + pathname in this order. Release large data immediately.
            if "path" in record and "asset" in record and "asset.meta" in record:
                record["row"] = row(record["path"], path.relative_to(ROOT).as_posix(),
                                    record.pop("asset"), record.pop("asset.meta"), guid)
    rows = []
    for guid, record in records.items():
        if "row" in record:
            rows.append(record["row"])
        elif "path" in record:
            rows.append(row(record["path"], path.relative_to(ROOT).as_posix(), record.get("asset", b""),
                            record.get("asset.meta", b""), guid))
        else:
            unusual.append(f"{guid}: missing pathname")
    return rows, {"path": path.relative_to(ROOT).as_posix(), "members": members,
                  "assets": len(rows), "unusualMembers": unusual, "packageManagerManifest": package_manifest}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "Docs")
    parser.add_argument("--guide-text", type=Path, default=ROOT / "Temp/VfxRefinery/Guides")
    parser.add_argument("--refresh-metadata", action="store_true",
                        help="Refresh only on-disk .meta hashes after Unity label edits; library content must be unchanged.")
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    args.guide_text.mkdir(parents=True, exist_ok=True)
    if args.refresh_metadata:
        output = args.output / "VfxSourceCatalog.csv"
        with output.open(newline="", encoding="utf-8-sig") as stream:
            rows = list(csv.DictReader(stream))
        changed = 0
        for item in rows:
            if not item["Container"] and item["Kind"] == "Metadata":
                path = ROOT / item["Path"]
                data = path.read_bytes()
                refreshed = inspect_blob(data, item["Path"])
                if item["Sha256"] != refreshed["Sha256"]:
                    item.update(refreshed)
                    changed += 1
        with output.open("w", newline="", encoding="utf-8-sig") as stream:
            writer = csv.DictWriter(stream, fieldnames=FIELDS)
            writer.writeheader()
            writer.writerows(rows)
        print(f"Refreshed {changed} on-disk metadata rows. No archives imported or rescanned.")
        return
    maintained = set(git("ls-files", "-z", "--cached", "--others", "--exclude-standard").split("\0"))
    # Supplied archives are ignored by Git (*.unitypackage); they still need to be discoverable.
    paths = sorted({p.relative_to(ROOT).as_posix() for prefix in LIBRARIES
                    for p in (ROOT / prefix).rglob("*") if p.is_file()} |
                   {prefix + ".meta" for prefix in LIBRARIES if (ROOT / (prefix + ".meta")).is_file()})
    rows, archives, guides = [], [], []
    for asset_path in sorted(paths):
        path = ROOT / asset_path
        meta_path = Path(str(path) + ".meta")
        meta = meta_path.read_bytes() if meta_path.exists() else b""
        if path.suffix.lower() == ".unitypackage":
            archive_row = row(asset_path, "", b"", meta)
            with path.open("rb") as stream:
                archive_row["Sha256"] = hashlib.file_digest(stream, "sha256").hexdigest()
            archive_row["Bytes"] = path.stat().st_size
            rows.append(archive_row)
            print(f"Scanning archive: {asset_path}", flush=True)
            inner, summary = scan_archive(path)
            rows.extend(inner)
            archives.append(summary)
            continue
        data = path.read_bytes()
        rows.append(row(asset_path, "", data, meta))
        if path.suffix.lower() == ".pdf":
            reader = PdfReader(BytesIO(data))
            page_texts = [p.extract_text() or "" for p in reader.pages]
            (args.guide_text / (path.stem + ".txt")).write_text(
                "\n\n".join(f"PAGE {i+1}\n{text}" for i, text in enumerate(page_texts)), encoding="utf-8")
            guides.append({"path": asset_path, "pages": len(page_texts),
                           "imageOnlyPages": [i+1 for i, text in enumerate(page_texts) if len(text.strip()) < 30]})
    # Resolve human-readable shader names and dependency paths without hiding unresolved GUIDs.
    by_guid = {}
    for item in rows:
        if item["Guid"] and item["Kind"] != "Metadata":
            by_guid.setdefault(item["Guid"], item)
    for item in rows:
        shader = by_guid.get(item["Shader"])
        if shader:
            item["Shader"] = shader["Path"]
    output = args.output / "VfxSourceCatalog.csv"
    with output.open("w", newline="", encoding="utf-8-sig") as stream:
        writer = csv.DictWriter(stream, fieldnames=FIELDS)
        writer.writeheader()
        writer.writerows(sorted(rows, key=lambda item: (item["Container"], item["Path"])))
    introduced = sorted(set(p for commit in IMPORT_COMMITS for p in git("diff-tree", "--no-commit-id", "--name-only", "-r", commit).splitlines()))
    covered = {r["Path"] for r in rows if not r["Container"]}
    report = {"importCommits": list(IMPORT_COMMITS), "importCommitFullHashes": [git("rev-parse", c).strip() for c in IMPORT_COMMITS],
              "introducedFiles": len(introduced), "introducedFilesOutsideVfxLibraries": [p for p in introduced if p not in covered],
              "diskFiles": len(paths), "localIgnoredFiles": [p for p in paths if p not in maintained],
              "catalogRows": len(rows), "archiveAssets": sum(a["assets"] for a in archives),
              "rolesOnDisk": dict(Counter(r["Role"] for r in rows if not r["Container"] and r["Kind"] != "Metadata")),
              "kindsOnDisk": dict(Counter(r["Kind"] for r in rows if not r["Container"])), "archives": archives, "guides": guides,
              "packages": {p: dict(Counter(r["Role"] for r in rows if r["Package"] == p and r["Kind"] != "Metadata")) for p in sorted({r["Package"] for r in rows})}}
    (args.output / "VfxPackageScan.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: report[k] for k in ("introducedFiles", "introducedFilesOutsideVfxLibraries", "diskFiles", "catalogRows", "archiveAssets", "rolesOnDisk")}, indent=2))


if __name__ == "__main__":
    main()
