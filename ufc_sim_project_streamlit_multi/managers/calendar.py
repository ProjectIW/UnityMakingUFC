"""Calendar + event planning (Saturdays only)."""
from __future__ import annotations
from dataclasses import dataclass
from datetime import date, timedelta
import random

SATURDAY = 5  # Monday=0

def next_saturday(d: date) -> date:
    delta = (SATURDAY - d.weekday()) % 7
    return d + timedelta(days=delta)

@dataclass
class PlanConfig:
    main_announce_weeks: int = 8
    full_generate_weeks: int = 4
    horizon_weeks: int = 12

def main_announce_date(event_date: date, cfg: PlanConfig) -> date:
    return event_date - timedelta(days=7*cfg.main_announce_weeks)

def full_generate_date(event_date: date, cfg: PlanConfig) -> date:
    return event_date - timedelta(days=7*cfg.full_generate_weeks)

def event_dates_in_horizon(start_date: date, horizon_weeks: int) -> list[date]:
    first_sat = next_saturday(start_date)
    saturdays = [first_sat + timedelta(days=7*w) for w in range(horizon_weeks)]
    by_month: dict[tuple[int,int], list[date]] = {}
    for d in saturdays:
        by_month.setdefault((d.year, d.month), []).append(d)

    picks: list[date] = []
    for _, days in sorted(by_month.items()):
        if not days:
            continue
        count = random.choice([1, 2, 2, 2, 3])
        count = min(count, len(days))
        picks.extend(random.sample(days, k=count))
    return sorted(picks)
