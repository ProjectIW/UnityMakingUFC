"""Matchmaking scoring for scheduled events."""
from __future__ import annotations
from datetime import date
from typing import Optional

def _pair_key(a_id: int, b_id: int) -> tuple[int,int]:
    return (a_id, b_id) if a_id < b_id else (b_id, a_id)

def score_pair(
    fa: dict,
    fb: dict,
    is_title_fight: bool,
    pair_last_fought: Optional[date],
    event_date: date,
    rematch_cooldown_days: int = 210,
) -> float:
    if pair_last_fought is not None and (event_date - pair_last_fought).days < rematch_cooldown_days:
        return -9999.0

    ra = float(fa["rating"]); rb = float(fb["rating"])
    score = 1000.0 - abs(ra - rb)

    # rank bonus (only if both ranked)
    ra_slot = str(fa.get("rank_slot","")).strip()
    rb_slot = str(fb.get("rank_slot","")).strip()
    if ra_slot and rb_slot:
        a = int(float(ra_slot)); b = int(float(rb_slot))
        score += 120.0 - 20.0 * abs(a - b)
        if a <= 5 and b <= 5:
            score += 40.0

    # streak bonus
    def clamp_int(x: int, lo: int, hi: int) -> int:
        return max(lo, min(hi, x))
    score += 10.0 * clamp_int(int(fa.get("streak", 0)), -3, 5)
    score += 10.0 * clamp_int(int(fb.get("streak", 0)), -3, 5)

    if is_title_fight:
        score *= 2.0
    return score

def pick_best_opponent(
    a: dict,
    candidates: list[dict],
    used_ids: set[int],
    pair_last_fought_map: dict[tuple[int,int], date],
    event_date: date,
    is_title_fight: bool,
) -> Optional[dict]:
    best = None
    best_score = -1e18
    a_id = int(a["id"])
    for b in candidates:
        b_id = int(b["id"])
        if b_id == a_id or b_id in used_ids:
            continue
        last = pair_last_fought_map.get(_pair_key(a_id, b_id))
        s = score_pair(a, b, is_title_fight=is_title_fight, pair_last_fought=last, event_date=event_date)
        if s > best_score:
            best_score = s
            best = b
    return best
