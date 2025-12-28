"""Core math formulas for UFC console sim (Flyweight MVP)."""
from __future__ import annotations
import random

def clamp(x: float, lo: float, hi: float) -> float:
    return max(lo, min(hi, x))

def age_bonus(age: int) -> float:
    """Peak around 30; soft decline."""
    return max(-40.0, 20.0 - 4.0 * abs(age - 30))

def streak_bonus(streak: int) -> float:
    s = int(clamp(streak, -3, 5))
    return float(s) * 10.0

def elo_prob(ra: float, rb: float) -> float:
    return 1.0 / (1.0 + 10.0 ** ((rb - ra) / 400.0))

def effective_rating(base_rating: float, age: int, streak: int, sigma: float) -> float:
    return base_rating + age_bonus(age) + streak_bonus(streak) + random.gauss(0.0, sigma)

def _rank_factor(rank_a: int | None, rank_b: int | None) -> float:
    if rank_a is None and rank_b is None:
        return 0.0
    if rank_a is None or rank_b is None:
        gap = 12
    else:
        gap = abs(rank_a - rank_b)
    return min(gap / 15.0, 1.0) * 0.35

def _mismatch_factor(ra: float, rb: float) -> float:
    diff = abs(ra - rb)
    return 0.65 + min(diff / 350.0, 1.0) * 0.75

def _upset_bonus(expected: float) -> float:
    return max(0.0, 0.5 - expected) * 1.6

def apply_elo(
    ra: float,
    rb: float,
    winner_a: bool,
    k: float = 24.0,
    rank_a: int | None = None,
    rank_b: int | None = None,
) -> tuple[float, float]:
    pa = elo_prob(ra, rb)
    sa = 1.0 if winner_a else 0.0
    mult = _mismatch_factor(ra, rb) + _rank_factor(rank_a, rank_b) + _upset_bonus(pa if winner_a else (1.0 - pa))
    mult = clamp(mult, 0.55, 2.2)
    k_eff = k * mult
    ra2 = ra + k_eff * (sa - pa)
    rb2 = rb + k_eff * ((1.0 - sa) - (1.0 - pa))
    return ra2, rb2
