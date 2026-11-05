"""Encode synthetic text at the aggregate sizes of the local Mongo copy."""

import json
import statistics
import time
import urllib.request
from pathlib import Path


base = "http://127.0.0.1:7998/embeddings"
seed = "В вымышленном мире участники обсуждают решение, его причины и следующий шаг. "


def text(index, length):
    content = f"Пример {index}. " + seed * ((length // len(seed)) + 1)
    return "categorize: " + content[:length]


def encode(items):
    request = urllib.request.Request(
        base,
        json.dumps({"model": "frida", "input": items}, ensure_ascii=False).encode("utf-8"),
        {"Content-Type": "application/json"},
    )
    started = time.monotonic()
    with urllib.request.urlopen(request, timeout=120) as response:
        json.load(response)
    return (time.monotonic() - started) * 1000


groups = {
    "player_posts_single": ([383] * 132, 1),
    "star_profiles_single": ([2800] * 199, 1),
    "world_anchors_batch4": ([394] * 80 + [43] * 80 + [155] * 80 + [67] * 240 + [98] * 178, 4),
}

for name, (lengths, batch_size) in groups.items():
    items = [text(index, length) for index, length in enumerate(lengths)]
    started = time.monotonic()
    times = [encode(items[index : index + batch_size]) for index in range(0, len(items), batch_size)]
    elapsed = time.monotonic() - started
    ordered = sorted(times)
    print(
        f"{name}: vectors={len(items)} calls={len(times)} elapsed_s={elapsed:.1f} "
        f"call_p50_ms={statistics.median(ordered):.0f} "
        f"call_p95_ms={ordered[int(len(ordered) * 0.95) - 1]:.0f}"
    )
