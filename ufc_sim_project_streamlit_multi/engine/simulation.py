"""Fight simulation + injuries + withdrawals."""
from __future__ import annotations
import random
from dataclasses import dataclass
from datetime import date, timedelta

from .formulas import effective_rating, elo_prob, apply_elo

@dataclass
class SimConfig:
    sigma: float = 90.0          # randomness ("upsets rarely but happen")
    k: float = 24.0              # Elo K-factor
    injury_after_fight_chance: float = 0.12
    withdrawal_chance: float = 0.10
    rest_days: int = 49
    injury_extra_min: int = 28
    injury_extra_max: int = 84

def choose_winner(rating_a_eff: float, rating_b_eff: float) -> bool:
    """Return True if A wins."""
    p = elo_prob(rating_a_eff, rating_b_eff)
    return random.random() < p

def after_fight_availability(event_date: date, cfg: SimConfig) -> tuple[date, int]:
    base = event_date + timedelta(days=cfg.rest_days)
    if random.random() < cfg.injury_after_fight_chance:
        extra = random.randint(cfg.injury_extra_min, cfg.injury_extra_max)
        return base + timedelta(days=extra), extra
    return base, 0

def simulate_fight(fa: dict, fb: dict, event_date: date, cfg: SimConfig) -> dict:
    ra_eff = effective_rating(float(fa['rating']), int(fa['age']), int(fa['streak']), cfg.sigma)
    rb_eff = effective_rating(float(fb['rating']), int(fb['age']), int(fb['streak']), cfg.sigma)
    a_wins = choose_winner(ra_eff, rb_eff)

    winner_id = int(fa['id']) if a_wins else int(fb['id'])
    loser_id = int(fb['id']) if a_wins else int(fa['id'])

    def rank_value(f: dict) -> int | None:
        if int(f.get("is_champ", 0)) == 1:
            return 0
        slot = str(f.get("rank_slot", "")).strip()
        if not slot:
            return None
        return int(float(slot))

    ra_new, rb_new = apply_elo(
        float(fa['rating']),
        float(fb['rating']),
        winner_a=a_wins,
        k=cfg.k,
        rank_a=rank_value(fa),
        rank_b=rank_value(fb),
    )
    next_a, inj_a = after_fight_availability(event_date, cfg)
    next_b, inj_b = after_fight_availability(event_date, cfg)

    return {
        "winner_id": winner_id,
        "loser_id": loser_id,
        "ra_new": ra_new,
        "rb_new": rb_new,
        "next_a": next_a.isoformat(),
        "next_b": next_b.isoformat(),
        "inj_a_extra": inj_a,
        "inj_b_extra": inj_b,
    }


def random_method_and_time():
    """Return (method, round, time_mmss)."""
    r = random.random()
    if r < 0.52:
        method = random.choice(["U-DEC", "S-DEC", "M-DEC"])
        rnd = 3
        sec = random.randint(10, 300)
    elif r < 0.82:
        method = random.choice([
            "KO (head kick)",
            "TKO (punches)",
            "TKO (ground and pound)",
            "TKO (doctor stoppage)",
        ])
        rnd = random.choice([1, 1, 2, 2, 3])
        sec = random.randint(10, 290 if rnd < 3 else 300)
    else:
        method = random.choice([
            "SUB (RNC)",
            "SUB (Armbar)",
            "SUB (Guillotine)",
            "SUB (Triangle)",
            "SUB (Kimura)",
        ])
        rnd = random.choice([1, 2, 2, 3])
        sec = random.randint(10, 290 if rnd < 3 else 300)
    mm = sec // 60
    ss = sec % 60
    return method, rnd, f"{mm:02d}:{ss:02d}"
