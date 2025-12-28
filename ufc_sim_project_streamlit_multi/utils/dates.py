from __future__ import annotations
from datetime import date

def parse_date(s: str, default: date | None = None) -> date | None:
    s = (s or "").strip()
    if not s:
        return default
    try:
        y, m, d = s.split("-")
        return date(int(y), int(m), int(d))
    except Exception:
        return default
