"""Ranking manager: recompute champ + top-15."""
from __future__ import annotations

def recompute_top15(fighters: list[dict]) -> None:
    champ = None
    others = []
    for f in fighters:
        if int(f.get("is_champ", 0)) == 1:
            champ = f
        else:
            others.append(f)
    # rating desc, streak desc
    others.sort(key=lambda x: (float(x.get("rating", 1500.0)), int(x.get("streak", 0))), reverse=True)
    top15 = others[:15]
    rest = others[15:]
    for i, f in enumerate(top15, start=1):
        f["rank_slot"] = str(i)
        f["rank_type"] = "RANKED"
        f["rank_raw"] = str(i)
    for f in rest:
        f["rank_slot"] = ""
        f["rank_type"] = "UNRANKED"
        f["rank_raw"] = "***"
    if champ is not None:
        champ["rank_slot"] = ""
        champ["rank_type"] = "CHAMP"
        champ["rank_raw"] = "Ч"
