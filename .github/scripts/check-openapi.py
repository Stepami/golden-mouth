#!/usr/bin/env python3
"""Compare the build-time OpenAPI document with the frontend snapshot."""
import difflib
import json
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
fresh = root / "backend/src/Reki.Api/OpenApi/reki-api.json"
snapshot = root / "frontend/openapi/reki-api.json"

def canonical(path):
    return json.dumps(json.loads(path.read_text()), ensure_ascii=False, sort_keys=True, indent=2).splitlines()

try:
    expected, actual = canonical(snapshot), canonical(fresh)
except (OSError, ValueError) as error:
    sys.exit(f"Cannot check OpenAPI: {error}")
if expected != actual:
    print("OpenAPI snapshot is stale. Build the API, copy its generated document to frontend/openapi/reki-api.json, then regenerate the frontend client.")
    print("\n".join(difflib.unified_diff(expected, actual, fromfile="snapshot", tofile="build-time OpenAPI")))
    sys.exit(1)
print("OpenAPI snapshot matches the build-time document.")
