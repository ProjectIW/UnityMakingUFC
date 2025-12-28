"""CSV I/O utilities (pandas-based)."""
from __future__ import annotations
import pandas as pd
from pathlib import Path

def read_csv_dicts(path: Path) -> list[dict]:
    if not path.exists():
        return []
    df = pd.read_csv(path, encoding="utf-8").fillna("")
    return df.to_dict(orient="records")

def write_csv_dicts(path: Path, rows: list[dict], columns: list[str] | None = None) -> None:
    df = pd.DataFrame(rows)
    if columns is not None:
        for c in columns:
            if c not in df.columns:
                df[c] = ""
        df = df[columns]
    df.to_csv(path, index=False, encoding="utf-8")

def read_kv(path: Path) -> dict[str,str]:
    df = pd.read_csv(path, encoding="utf-8")
    return {str(r["key"]): str(r["value"]) for _, r in df.iterrows()}

def write_kv(path: Path, kv: dict[str,str]) -> None:
    df = pd.DataFrame([{"key":k, "value":v} for k,v in kv.items()])
    df.to_csv(path, index=False, encoding="utf-8")
