"""Separate historical Git blobs from checkout byte identities; read-only diagnostic."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    args = p.parse_args()
    root = args.repo.resolve(strict=True)
    output = args.output.resolve()
    if output.parent != root / "Docs/testing/evidence/h1-f4/context-binding-v2-audit" or output.exists():
        raise SystemExit("Expected new output in exact evidence directory")
    head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    previous = "c2b82030b15e3d5e5a9e5aa9fb5dae37fcba314d"
    audit = json.loads((root / "Docs/testing/evidence/h1-f4/context-binding-v2-audit/result.json").read_bytes())
    sha = lambda data: hashlib.sha256(data).hexdigest()
    rows = []
    for path in (name for name in audit["files_sha256"] if name.endswith(".json")):
        old = subprocess.check_output(["git", "show", previous + ":" + path], cwd=root)
        current = subprocess.check_output(["git", "show", head + ":" + path], cwd=root)
        checkout = (root / path).read_bytes()
        rows.append({"path": path, "historical_git_blob_sha256": sha(old),
                     "baseline_git_blob_sha256": sha(current), "checkout_sha256": sha(checkout),
                     "historical_blob_equals_baseline_blob": old == current,
                     "historical_json_equals_baseline_json": json.loads(old) == json.loads(current),
                     "checkout_equals_baseline_blob": checkout == current,
                     "checkout_differs_only_by_crlf": checkout != current and checkout.replace(b"\r\n", b"\n") == current.replace(b"\r\n", b"\n")})
    result = {"historical_source": previous, "baseline_source": head,
              "probe_sha256": sha(Path(__file__).read_bytes()), "files": rows,
              "unchanged_source_blobs": sum(row["historical_blob_equals_baseline_blob"] for row in rows),
              "total": len(rows),
              "limitation": "Git source comparison, not historical imported TextAsset/runtime byte evidence. CRLF comparison is diagnostic only; production binding still hashes exact captured bytes without normalization. No legacy activation authority is granted."}
    with output.open("x", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"output": str(output), "sha256": sha(output.read_bytes()),
                      "unchanged_source_blobs": result["unchanged_source_blobs"], "total": len(rows)}))


if __name__ == "__main__":
    main()
