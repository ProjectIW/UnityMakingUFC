from __future__ import annotations

import json
import random
import shutil
from pathlib import Path
from datetime import date, timedelta

from engine.simulation import SimConfig, simulate_fight, random_method_and_time
from engine.matchmaking import pick_best_opponent
from managers.calendar import next_saturday, PlanConfig, main_announce_date, full_generate_date, event_dates_in_horizon
from managers.ranking import recompute_top15
from managers import news_service as news
from utils.io import read_csv_dicts, write_csv_dicts, read_kv, write_kv
from utils.dates import parse_date


ROOT = Path(__file__).resolve().parent
SAVE_ROOT: Path | None = None

BASE_DATA_DIR = ROOT / "Data"


def set_save_root(save_root: Path | None) -> None:
    global SAVE_ROOT
    SAVE_ROOT = save_root


def saves_root() -> Path:
    return ROOT / "Saves"


def save_slot_dir(slot_id: int) -> Path:
    return saves_root() / f"save_{int(slot_id)}"


def save_slot_exists(slot_id: int) -> bool:
    slot = save_slot_dir(slot_id)
    return (slot / "Data" / "_global" / "save_game.csv").exists()


def save_slot_dir_exists(slot_id: int) -> bool:
    return save_slot_dir(slot_id).exists()


def create_save_slot(slot_id: int, overwrite: bool = False) -> Path:
    slot = save_slot_dir(slot_id)
    if slot.exists():
        if not overwrite:
            raise FileExistsError(f"Save slot {slot_id} already exists.")
        shutil.rmtree(slot)
    slot.mkdir(parents=True, exist_ok=True)
    shutil.copytree(BASE_DATA_DIR, slot / "Data")
    return slot


def copy_save_slot(source_slot: int, target_slot: int, overwrite: bool = False) -> Path:
    source = save_slot_dir(source_slot)
    target = save_slot_dir(target_slot)
    if not source.exists():
        raise FileNotFoundError(f"Source save slot {source_slot} missing.")
    if target.exists():
        if not overwrite:
            raise FileExistsError(f"Target save slot {target_slot} already exists.")
        shutil.rmtree(target)
    target.mkdir(parents=True, exist_ok=True)
    shutil.copytree(source / "Data", target / "Data")
    return target


def data_root() -> Path:
    if SAVE_ROOT:
        return SAVE_ROOT / "Data"
    return BASE_DATA_DIR


def global_dir() -> Path:
    return data_root() / "_global"


def events_csv() -> Path:
    return global_dir() / "events.csv"


def fights_csv() -> Path:
    return global_dir() / "fights.csv"


def save_csv() -> Path:
    return global_dir() / "save_game.csv"

EVENTS_COLUMNS = [
    "event_id","event_date","generated_on","announced_main_on","announced_full_on",
    "completed","main_fight_id","event_kind","location","theme_country","notes_json"
]
FIGHTS_COLUMNS = [
    "fight_id","event_id","division","a_id","b_id",
    "is_top15","is_main_event","is_title_fight",
    "card_slot","status","winner_id","method","round","time_mmss"
]
FIGHTERS_COLUMNS = [
    "id","division","name","country","age","rank_raw","rank_type","rank_slot","is_champ",
    "wins","draws","losses","rating","streak","last_fight_date","next_available_date",
    "rating_history","rank_history","is_active"
]
PAIRS_COLUMNS = ["a_id","b_id","last_fight_date"]


def list_divisions() -> list[str]:
    data_dir = data_root()
    if not data_dir.exists():
        return ["Flyweight"]
    divs = []
    for p in data_dir.iterdir():
        if p.is_dir() and p.name != "_global":
            if (p / "fighters.csv").exists():
                divs.append(p.name)
    return sorted(divs) or ["Flyweight"]

def division_dir(division: str) -> Path:
    return data_root() / division

def fighters_csv(division: str) -> Path:
    return division_dir(division) / "fighters.csv"

def pairs_csv(division: str) -> Path:
    return division_dir(division) / "pair_history.csv"


# -------------------- Helpers --------------------
def _pair_key(a_id: int, b_id: int) -> tuple[int,int]:
    return (a_id, b_id) if a_id < b_id else (b_id, a_id)

def _json_load_list(s: str) -> list:
    try:
        v = json.loads(s or "[]")
        return v if isinstance(v, list) else []
    except Exception:
        return []

def _json_dump(v) -> str:
    return json.dumps(v, ensure_ascii=False)

def event_display_name(kind: str, event_id: int | str, location: str, theme_country: str) -> str:
    loc = str(location or "").strip()
    theme = str(theme_country or "").strip()
    if str(kind).strip() == "NUMBERED":
        number = 300 + int(event_id)
        base = f"UFC {number}"
        if loc:
            base = f"{base}: {loc}"
        return base
    if str(kind).strip() == "COUNTRY":
        base = "UFC Fight Night"
        if loc:
            base = f"{base}: {loc}"
        if theme:
            base = f"{base} ({theme} special)"
        return base
    base = "UFC Fight Night"
    if loc:
        base = f"{base}: {loc}"
    return base

def _rh_get(f: dict) -> list[dict]:
    s = str(f.get("rating_history","")).strip()
    if not s:
        return []
    try:
        v = json.loads(s)
        return v if isinstance(v, list) else []
    except Exception:
        return []

def _rh_append(f: dict, d: date) -> None:
    rh = _rh_get(f)
    rh.append({"d": d.isoformat(), "r": float(f.get("rating", 1500.0))})
    f["rating_history"] = _json_dump(rh[-60:])

def _kh_get(f: dict) -> list[dict]:
    s = str(f.get("rank_history","")).strip()
    if not s:
        return []
    try:
        v = json.loads(s)
        return v if isinstance(v, list) else []
    except Exception:
        return []

def _kh_append(f: dict, d: date) -> None:
    kh = _kh_get(f)
    slot = str(f.get("rank_slot","")).strip()
    rank = int(float(slot)) if slot else None
    if int(f.get("is_champ",0)) == 1:
        rank = 0
    kh.append({"d": d.isoformat(), "rank": rank})
    f["rank_history"] = _json_dump(kh[-60:])

def ensure_histories_initialized(fighters_by_div: dict[str,list[dict]], start_date: date) -> bool:
    changed = False
    for div, fighters in fighters_by_div.items():
        for f in fighters:
            if "division" not in f or not str(f.get("division","")).strip():
                f["division"] = div
                changed = True
            if "rating_history" not in f or not str(f.get("rating_history","")).strip():
                f["rating_history"] = _json_dump([{"d": start_date.isoformat(), "r": float(f.get("rating",1500.0))}])
                changed = True
            if "rank_history" not in f or not str(f.get("rank_history","")).strip():
                slot = str(f.get("rank_slot","")).strip()
                rank = int(float(slot)) if slot else None
                if int(f.get("is_champ",0)) == 1:
                    rank = 0
                f["rank_history"] = _json_dump([{"d": start_date.isoformat(), "rank": rank}])
                changed = True
    return changed

def available(f: dict, when: date) -> bool:
    if int(f.get("is_active", 1)) != 1:
        return False
    nad = parse_date(str(f.get("next_available_date","")).strip())
    if nad is None:
        last = parse_date(str(f.get("last_fight_date","")).strip())
        if last is not None and (when - last).days < 49:
            return False
    return nad is None or when >= nad


# -------------------- I/O --------------------
def _ensure_global_files():
    global_dir().mkdir(parents=True, exist_ok=True)
    if not events_csv().exists():
        write_csv_dicts(events_csv(), [], columns=EVENTS_COLUMNS)
    if not fights_csv().exists():
        write_csv_dicts(fights_csv(), [], columns=FIGHTS_COLUMNS)
    if not save_csv().exists():
        write_kv(save_csv(), {
            "current_date":"2026-01-01",
            "next_event_id":"1",
            "next_fight_id":"1",
            "last_title_fight_date":"",
            "random_seed":"12345",
        })

def load_state():
    _ensure_global_files()
    divisions = list_divisions()
    fighters_by_div = {}
    pairs_by_div = {}
    for div in divisions:
        fpath = fighters_csv(div)
        ppath = pairs_csv(div)
        fighters_by_div[div] = read_csv_dicts(fpath) if fpath.exists() else []
        pairs_by_div[div] = read_csv_dicts(ppath) if ppath.exists() else []
    events = read_csv_dicts(events_csv())
    fights = read_csv_dicts(fights_csv())
    save = read_kv(save_csv())
    return fighters_by_div, events, fights, pairs_by_div, save

def save_state(fighters_by_div, events, fights, pairs_by_div, save):
    _ensure_global_files()
    for div, fighters in fighters_by_div.items():
        ddir = division_dir(div)
        ddir.mkdir(parents=True, exist_ok=True)
        write_csv_dicts(fighters_csv(div), fighters, columns=FIGHTERS_COLUMNS)
    for div, pairs in pairs_by_div.items():
        ddir = division_dir(div)
        ddir.mkdir(parents=True, exist_ok=True)
        write_csv_dicts(pairs_csv(div), pairs, columns=PAIRS_COLUMNS)
    write_csv_dicts(events_csv(), events, columns=EVENTS_COLUMNS)
    write_csv_dicts(fights_csv(), fights, columns=FIGHTS_COLUMNS)
    write_kv(save_csv(), save)


# -------------------- Fighter utilities --------------------
def find_fighter(fighters_by_div, division: str, fid: int) -> dict:
    for f in fighters_by_div.get(division, []):
        if int(f.get("id",0)) == int(fid):
            return f
    raise KeyError((division, fid))

def fighter_name(fighters_by_div, division: str, fid: int) -> str:
    try:
        return find_fighter(fighters_by_div, division, fid).get("name", f"#{fid}")
    except Exception:
        return f"#{fid}"

def current_champ(fighters_by_div, division: str):
    for f in fighters_by_div.get(division, []):
        if int(f.get("is_champ",0)) == 1:
            return f
    return None

def top15_only(fighters_by_div, division: str):
    return [f for f in fighters_by_div.get(division, []) if int(f.get("is_champ",0)) != 1 and str(f.get("rank_slot","")).strip()]

def unranked_only(fighters_by_div, division: str):
    return [f for f in fighters_by_div.get(division, []) if int(f.get("is_champ",0)) != 1 and not str(f.get("rank_slot","")).strip()]

def _is_top_tier_fighter(f: dict) -> bool:
    if int(f.get("is_champ", 0)) == 1:
        return True
    slot = str(f.get("rank_slot", "")).strip()
    if not slot:
        return False
    try:
        return int(float(slot)) <= 8
    except ValueError:
        return False

def _event_top_tier_bout_count(fights: list[dict], fighters_by_div, event_id: int) -> int:
    count = 0
    for ft in fights:
        if int(ft.get("event_id", 0)) != event_id:
            continue
        if ft.get("status") == "cancelled":
            continue
        div = ft.get("division", "")
        if not div:
            continue
        a = find_fighter(fighters_by_div, div, int(ft.get("a_id", 0)))
        b = find_fighter(fighters_by_div, div, int(ft.get("b_id", 0)))
        if not a or not b:
            continue
        if _is_top_tier_fighter(a) and _is_top_tier_fighter(b):
            count += 1
    return count

def _fight_is_ranked(fighters_by_div, division: str, a_id: int, b_id: int) -> bool:
    a = find_fighter(fighters_by_div, division, a_id)
    b = find_fighter(fighters_by_div, division, b_id)
    if not a or not b:
        return False
    a_rank = str(a.get("rank_slot","")).strip()
    b_rank = str(b.get("rank_slot","")).strip()
    return bool(a_rank) and bool(b_rank)


# -------------------- Pair history --------------------
def get_pair_last_map(pairs: list[dict]) -> dict[tuple[int,int], date]:
    m = {}
    for r in pairs:
        a = int(r["a_id"]); b = int(r["b_id"])
        dt = parse_date(r["last_fight_date"])
        if dt:
            m[_pair_key(a,b)] = dt
    return m

def set_pair_last(pairs: list[dict], a_id: int, b_id: int, dt: date):
    a,b = _pair_key(a_id,b_id)
    for r in pairs:
        if int(r["a_id"]) == a and int(r["b_id"]) == b:
            r["last_fight_date"] = dt.isoformat()
            return
    pairs.append({"a_id": a, "b_id": b, "last_fight_date": dt.isoformat()})


# -------------------- Scheduling guards --------------------
def booked_ids_and_pairs(fights: list[dict], division: str, current_event_id: int):
    """Return (booked_ids, booked_pairs) for 'scheduled' fights in *other* events.
    This prevents the planner from booking the same fighter/pair multiple times in the future
    (a common cause of repeating main events/cards early in the sim).
    """
    booked_ids = set()
    booked_pairs = set()
    for ft in fights:
        if ft.get("status") != "scheduled":
            continue
        if ft.get("division") != division:
            continue
        try:
            eid = int(ft.get("event_id", 0))
        except Exception:
            continue
        if eid == int(current_event_id):
            continue
        try:
            a = int(ft.get("a_id", 0)); b = int(ft.get("b_id", 0))
        except Exception:
            continue
        booked_ids.add(a); booked_ids.add(b)
        booked_pairs.add(_pair_key(a, b))
    return booked_ids, booked_pairs


# -------------------- Event Types --------------------
def pick_event_kind() -> str:
    r = random.random()
    if r < 0.55:
        return "FIGHT_NIGHT"
    if r < 0.85:
        return "NUMBERED"
    return "COUNTRY"

def pick_location_and_theme(fighters_by_div, kind: str) -> tuple[str,str]:
    if kind == "COUNTRY":
        countries = []
        for fighters in fighters_by_div.values():
            for f in fighters:
                c = str(f.get("country","")).strip()
                if c:
                    countries.append(c)
        theme = random.choice(countries) if countries else ""
        city = random.choice(["Las Vegas","New York","London","Paris","Abu Dhabi","Singapore","Tokyo","Sydney","Toronto","Mexico City"])
        return city, theme
    return random.choice(["Las Vegas","Apex","New York","London","Paris"]), ""


# -------------------- Planning --------------------
def ensure_events_planned(fighters_by_div, events, fights, pairs_by_div, save, today: date):
    pcfg = PlanConfig()
    next_eid = int(save.get("next_event_id","1"))
    next_fid = int(save.get("next_fight_id","1"))

    idx = {e["event_date"]: e for e in events}

    for ed in event_dates_in_horizon(today, pcfg.horizon_weeks):
        eds = ed.isoformat()
        if eds not in idx:
            kind = pick_event_kind()
            loc, theme = pick_location_and_theme(fighters_by_div, kind)
            events.append({
                "event_id": next_eid,
                "event_date": eds,
                "generated_on": "",
                "announced_main_on": "",
                "announced_full_on": "",
                "completed": 0,
                "main_fight_id": "",
                "event_kind": kind,
                "location": loc,
                "theme_country": theme,
                "notes_json": "[]",
            })
            idx[eds] = events[-1]
            next_eid += 1

    _cancel_self_fights(events, fights)

    for e in events:
        ed = parse_date(e["event_date"])
        if not ed or int(e.get("completed",0)) == 1:
            continue

        if today >= main_announce_date(ed, pcfg) and not str(e.get("announced_main_on","")).strip():
            next_fid = plan_main_event(fighters_by_div, fights, pairs_by_div, today, ed, e, next_fid, save)
            e["announced_main_on"] = today.isoformat()

        if today >= full_generate_date(ed, pcfg) and not str(e.get("generated_on","")).strip():
            next_fid = fill_full_card(fighters_by_div, fights, pairs_by_div, today, ed, e, next_fid)
            e["generated_on"] = today.isoformat()
            e["announced_full_on"] = today.isoformat()

    save["next_event_id"] = str(next_eid)
    save["next_fight_id"] = str(next_fid)


def _already_scheduled_title(fights, division: str, champ_id: int) -> bool:
    for ft in fights:
        if ft.get("status") != "scheduled":
            continue
        if ft.get("division") != division:
            continue
        if int(ft.get("is_title_fight",0)) != 1:
            continue
        if int(ft.get("a_id",0)) == champ_id or int(ft.get("b_id",0)) == champ_id:
            return True
    return False


def _event_used_ids(fights: list[dict], event_id: int, division: str) -> set[int]:
    used = set()
    for ft in fights:
        if int(ft.get("event_id", 0)) != event_id:
            continue
        if ft.get("division") != division:
            continue
        try:
            used.add(int(ft.get("a_id", 0)))
            used.add(int(ft.get("b_id", 0)))
        except Exception:
            continue
    return used


def _append_fight(fights: list[dict], fight: dict, event_row: dict | None = None) -> bool:
    try:
        a_id = int(fight.get("a_id", 0))
        b_id = int(fight.get("b_id", 0))
    except Exception:
        return False
    if a_id == 0 or b_id == 0:
        return False
    if a_id == b_id:
        if event_row is not None:
            notes = _json_load_list(event_row.get("notes_json", "[]"))
            notes.append("⚠️ Отменён бой из-за совпадения бойцов в паре.")
            event_row["notes_json"] = _json_dump(notes)
        return False
    fights.append(fight)
    return True


def _cancel_self_fights(events: list[dict], fights: list[dict]) -> None:
    events_by_id = {int(e.get("event_id", 0)): e for e in events}
    for ft in fights:
        try:
            a_id = int(ft.get("a_id", 0))
            b_id = int(ft.get("b_id", 0))
        except Exception:
            continue
        if a_id == 0 or b_id == 0 or a_id != b_id:
            continue
        if ft.get("status") == "cancelled":
            continue
        ft["status"] = "cancelled"
        event_row = events_by_id.get(int(ft.get("event_id", 0)))
        if event_row is None:
            continue
        notes = _json_load_list(event_row.get("notes_json", "[]"))
        notes.append("⚠️ Бой отменён: один и тот же боец был записан в обе стороны.")
        event_row["notes_json"] = _json_dump(notes)


def _plan_featured_bout(
    fighters_by_div,
    fights,
    pairs_by_div,
    event_date: date,
    event_row: dict,
    next_fid: int,
    division: str,
    title_chance: float,
    card_slot: str,
    is_main_event: bool,
    note_label: str,
    save: dict,
) -> int:
    if not division:
        return next_fid
    current_event_id = int(event_row["event_id"])
    booked_ids, booked_pairs = booked_ids_and_pairs(fights, division, current_event_id)
    used_in_event = _event_used_ids(fights, current_event_id, division)
    booked_ids |= used_in_event

    champ = current_champ(fighters_by_div, division)
    if champ and available(champ, event_date) and (random.random() < title_chance) and (not _already_scheduled_title(fights, division, int(champ["id"]))):
        last_title = parse_date(save.get("last_title_fight_date",""))
        due = (last_title is None) or ((event_date - last_title).days >= 56)
        if due:
            ranked = [f for f in top15_only(fighters_by_div, division) if available(f, event_date) and int(f.get('id',0)) not in booked_ids]
            ranked.sort(key=lambda x: int(float(x["rank_slot"])))
            pool = ranked[:5]
            pool_pref = pool[:3]
            used = {int(champ["id"])}
            pair_last_map = get_pair_last_map(pairs_by_div.get(division, []))
            challenger = (
                pick_best_opponent(champ, pool_pref, used, pair_last_map, event_date, is_title_fight=True)
                or pick_best_opponent(champ, pool, used, pair_last_map, event_date, is_title_fight=True)
            )
            if challenger is not None:
                fight = {
                    "fight_id": next_fid, "event_id": int(event_row["event_id"]), "division": division,
                    "a_id": int(champ["id"]), "b_id": int(challenger["id"]),
                    "is_top15": 1, "is_main_event": 1 if is_main_event else 0, "is_title_fight": 1,
                    "card_slot": card_slot,
                    "status": "scheduled", "winner_id": "", "method": "", "round": "", "time_mmss": ""
                }
                if _append_fight(fights, fight, event_row):
                    if is_main_event:
                        event_row["main_fight_id"] = str(next_fid)
                    notes = _json_load_list(event_row.get("notes_json","[]"))
                    notes.append(f"🏆 Анонс титульного боя ({division}): {fighter_name(fighters_by_div,division,int(champ['id']))} vs {fighter_name(fighters_by_div,division,int(challenger['id']))}")
                    event_row["notes_json"] = _json_dump(notes)
                    return next_fid + 1
                return next_fid

    ranked = [f for f in top15_only(fighters_by_div, division) if available(f, event_date) and int(f.get('id',0)) not in booked_ids]
    ranked.sort(key=lambda x: int(float(x["rank_slot"])))
    pool = ranked[:8]
    pair_last_map = get_pair_last_map(pairs_by_div.get(division, []))
    best_pair = None
    best_score = -1e18
    for i in range(len(pool)):
        for j in range(i+1, len(pool)):
            a, b = pool[i], pool[j]
            pk = _pair_key(int(a["id"]), int(b["id"]))
            if pk in booked_pairs:
                continue
            last = pair_last_map.get(pk)
            if last is not None and (event_date - last).days < 210:
                continue
            s = 1000.0 - abs(float(a["rating"]) - float(b["rating"]))
            s += 120.0 - 20.0 * abs(int(float(a["rank_slot"])) - int(float(b["rank_slot"])))
            if s > best_score:
                best_score = s
                best_pair = (a,b)
    if best_pair is None and len(pool) >= 2:
        best_pair = (pool[0], pool[1])
    if best_pair is None:
        return next_fid

    a, b = best_pair
    fight = {
        "fight_id": next_fid, "event_id": int(event_row["event_id"]), "division": division,
        "a_id": int(a["id"]), "b_id": int(b["id"]),
        "is_top15": 1, "is_main_event": 1 if is_main_event else 0, "is_title_fight": 0,
        "card_slot": card_slot,
        "status": "scheduled", "winner_id": "", "method": "", "round": "", "time_mmss": ""
    }
    if _append_fight(fights, fight, event_row):
        if is_main_event:
            event_row["main_fight_id"] = str(next_fid)
        notes = _json_load_list(event_row.get("notes_json","[]"))
        notes.append(f"{note_label} ({division}): {fighter_name(fighters_by_div,division,int(a['id']))} vs {fighter_name(fighters_by_div,division,int(b['id']))}")
        event_row["notes_json"] = _json_dump(notes)
        return next_fid + 1
    return next_fid


def plan_main_event(fighters_by_div, fights, pairs_by_div, today: date, event_date: date, event_row: dict, next_fid: int, save: dict) -> int:
    kind = str(event_row.get("event_kind","FIGHT_NIGHT")).strip() or "FIGHT_NIGHT"
    divisions = list(fighters_by_div.keys())
    if not divisions:
        return next_fid

    title_chance = 0.38 if kind == "NUMBERED" else 0.18
    title_chance = 0.25 if kind == "COUNTRY" else title_chance

    main_div = random.choice(divisions)
    notes = _json_load_list(event_row.get("notes_json","[]"))
    notes.append(f"📍 {event_display_name(kind, event_row.get('event_id',''), event_row.get('location',''), event_row.get('theme_country',''))}")
    event_row["notes_json"] = _json_dump(notes)
    next_fid = _plan_featured_bout(
        fighters_by_div,
        fights,
        pairs_by_div,
        event_date,
        event_row,
        next_fid,
        main_div,
        title_chance,
        "MAIN_EVENT",
        True,
        "📣 Анонс мейн-ивента",
        save,
    )

    if kind == "NUMBERED":
        remaining = [d for d in divisions if d != main_div]
        co_div = random.choice(remaining) if remaining else main_div
        co_title_chance = 0.22
        next_fid = _plan_featured_bout(
            fighters_by_div,
            fights,
            pairs_by_div,
            event_date,
            event_row,
            next_fid,
            co_div,
            co_title_chance,
            "CO_MAIN",
            False,
            "📣 Анонс ко-мейн ивента",
            save,
        )

    return next_fid


def _top_fight_probability(kind: str) -> float:
    if kind == "NUMBERED":
        return 0.65
    if kind == "COUNTRY":
        return 0.55
    return 0.45

def _division_targets(divisions: list[str], existing_counts: dict[str, int], total_fights: int) -> dict[str, int]:
    if len(divisions) < 3:
        max_per_div = total_fights
    else:
        max_per_div = min(6, max(4, int(total_fights * 0.4)))
    min_divisions = min(3, len(divisions))
    seeds = random.sample(divisions, k=min_divisions) if divisions else []
    targets = {d: existing_counts.get(d, 0) for d in divisions}
    remaining = total_fights - sum(existing_counts.values())

    for div in seeds:
        need = max(0, 2 - targets[div])
        add = min(need, remaining)
        targets[div] += add
        remaining -= add

    while remaining > 0:
        candidates = [d for d in divisions if targets[d] < max_per_div]
        if not candidates:
            break
        div = random.choice(candidates)
        targets[div] += 1
        remaining -= 1
    return targets


def _pick_card_plan(kind: str) -> tuple[int, int]:
    options = []
    has_co_main = kind == "NUMBERED"
    for main_card in range(6, 8):
        for prelims in range(8, 13):
            total = main_card + prelims + (2 if has_co_main else 1)
            if 16 <= total <= 20:
                options.append((main_card, prelims))
    return random.choice(options) if options else (6, 9)


def _avg_rating(fighters_by_div, division: str, a_id: int, b_id: int) -> float:
    ra = find_fighter(fighters_by_div, division, a_id).get("rating", 1500.0)
    rb = find_fighter(fighters_by_div, division, b_id).get("rating", 1500.0)
    return (float(ra) + float(rb)) / 2.0


def _assign_card_slots(fighters_by_div, fights: list[dict], event_id: int, main_card_count: int, prelim_count: int) -> None:
    ranked_candidates = []
    unranked_candidates = []
    for ft in fights:
        if int(ft.get("event_id", 0)) != event_id:
            continue
        if ft.get("card_slot") in ("MAIN_EVENT", "CO_MAIN"):
            continue
        if str(ft.get("card_slot","")).strip():
            continue
        div = ft.get("division","")
        avg_rating = _avg_rating(fighters_by_div, div, int(ft.get("a_id",0)), int(ft.get("b_id",0)))
        if _fight_is_ranked(fighters_by_div, div, int(ft.get("a_id",0)), int(ft.get("b_id",0))):
            ranked_candidates.append((avg_rating, ft))
        else:
            unranked_candidates.append((avg_rating, ft))

    ranked_candidates.sort(key=lambda x: x[0], reverse=True)
    unranked_candidates.sort(key=lambda x: x[0], reverse=True)

    main_slots = min(main_card_count, len(ranked_candidates))
    for idx, (_, ft) in enumerate(ranked_candidates[:main_slots]):
        ft["card_slot"] = "MAIN_CARD"

    for _, ft in unranked_candidates:
        ft["card_slot"] = "PRELIMS"

    remaining_prelims = max(0, prelim_count - len(unranked_candidates))
    if remaining_prelims > 0:
        for _, ft in ranked_candidates[main_slots:]:
            if remaining_prelims <= 0:
                break
            ft["card_slot"] = "PRELIMS"
            remaining_prelims -= 1

    for ft in fights:
        if int(ft.get("event_id", 0)) != event_id:
            continue
        if ft.get("card_slot") in ("MAIN_EVENT", "CO_MAIN"):
            continue
        if not str(ft.get("card_slot","")).strip():
            ft["card_slot"] = "PRELIMS"


def fill_full_card(fighters_by_div, fights, pairs_by_div, today: date, event_date: date, event_row: dict, next_fid: int) -> int:
    event_id = int(event_row["event_id"])
    kind = str(event_row.get("event_kind","FIGHT_NIGHT")).strip() or "FIGHT_NIGHT"
    theme_country = str(event_row.get("theme_country","")).strip()
    divisions = list(fighters_by_div.keys())
    if not divisions:
        return next_fid
    main_card_count, prelims_count = _pick_card_plan(kind)
    existing_counts: dict[str, int] = {}
    existing_top_by_div: dict[str, int] = {}
    existing_unr_by_div: dict[str, int] = {}
    for ft in fights:
        if int(ft.get("event_id", 0)) != event_id:
            continue
        div = ft.get("division")
        if ft.get("card_slot") in ("MAIN_EVENT", "CO_MAIN"):
            continue
        existing_counts[div] = existing_counts.get(div, 0) + 1
        if _fight_is_ranked(fighters_by_div, div, int(ft.get("a_id",0)), int(ft.get("b_id",0))):
            existing_top_by_div[div] = existing_top_by_div.get(div, 0) + 1
        else:
            existing_unr_by_div[div] = existing_unr_by_div.get(div, 0) + 1
    total_target = main_card_count + prelims_count
    targets = _division_targets(divisions, existing_counts, total_target)
    total_top_desired = max(main_card_count, sum(existing_top_by_div.values()))
    remaining_top = max(0, total_top_desired - sum(existing_top_by_div.values()))
    top_targets = {d: existing_top_by_div.get(d, 0) for d in divisions}
    while remaining_top > 0:
        candidates = [d for d in divisions if top_targets[d] < targets.get(d, 0)]
        if not candidates:
            break
        div = random.choice(candidates)
        top_targets[div] += 1
        remaining_top -= 1
    top_prob = _top_fight_probability(kind)
    top_tier_limit = 4
    top_tier_count = _event_top_tier_bout_count(fights, fighters_by_div, event_id)

    for div in divisions:
        used = set()
        booked_ids, booked_pairs = booked_ids_and_pairs(fights, div, event_id)
        used |= set(booked_ids)
        existing_all = [f for f in fights if int(f["event_id"]) == event_id and f.get("division") == div]
        for f in existing_all:
            used.add(int(f["a_id"])); used.add(int(f["b_id"]))
        event_pairs = {_pair_key(int(f["a_id"]), int(f["b_id"])) for f in existing_all}
        existing = [f for f in existing_all if f.get("card_slot") not in ("MAIN_EVENT", "CO_MAIN")]

        top_existing = sum(1 for f in existing if int(f.get("is_top15",0))==1 and f.get("status")=="scheduled")
        unr_existing = sum(1 for f in existing if int(f.get("is_top15",0))==0 and f.get("status")=="scheduled")

        target_total = targets.get(div, top_existing + unr_existing)
        top_target = min(top_targets.get(div, 0), target_total)
        unr_target = max(0, target_total - top_target)
        top_needed = max(0, top_target - top_existing)
        unr_needed = max(0, unr_target - unr_existing)

        pair_last_map = get_pair_last_map(pairs_by_div.get(div, []))
        top_pool = [f for f in top15_only(fighters_by_div, div) if available(f, event_date) and int(f["id"]) not in used]
        unr_pool = [f for f in unranked_only(fighters_by_div, div) if available(f, event_date) and int(f["id"]) not in used]

        def theme_boost(x: dict):
            c = str(x.get("country","")).strip()
            return 1 if (theme_country and c == theme_country) else 0

        top_pool.sort(key=lambda x: (theme_boost(x), float(x["rating"])), reverse=True)
        unr_pool.sort(key=lambda x: (theme_boost(x), float(x["rating"])), reverse=True)

        allow_special = random.random() < 0.06

        while top_needed > 0 and len(top_pool) >= 2:
            a = top_pool.pop(0)
            a_rank = int(float(a["rank_slot"]))
            candidates = [b for b in top_pool if abs(a_rank - int(float(b["rank_slot"]))) <= 6]
            candidates = [b for b in candidates if _pair_key(int(a["id"]), int(b["id"])) not in booked_pairs]
            candidates = [b for b in candidates if _pair_key(int(a["id"]), int(b["id"])) not in event_pairs]
            if allow_special and int(a.get("streak",0)) >= 4:
                candidates = [b for b in top_pool if _pair_key(int(a["id"]), int(b["id"])) not in booked_pairs]
                candidates = [b for b in candidates if _pair_key(int(a["id"]), int(b["id"])) not in event_pairs]
            pool_fb = [bb for bb in (candidates or top_pool) if _pair_key(int(a["id"]), int(bb["id"])) not in booked_pairs]
            pool_fb = [bb for bb in pool_fb if _pair_key(int(a["id"]), int(bb["id"])) not in event_pairs]
            if top_tier_count >= top_tier_limit:
                pool_fb = [bb for bb in pool_fb if not (_is_top_tier_fighter(a) and _is_top_tier_fighter(bb))]
            b = pick_best_opponent(a, pool_fb, used, pair_last_map, event_date, is_title_fight=False)
            if b is None:
                continue
            top_pool = [x for x in top_pool if int(x["id"]) != int(b["id"])]
            used.add(int(a["id"])); used.add(int(b["id"]))
            event_pairs.add(_pair_key(int(a["id"]), int(b["id"])))
            fight = {
                "fight_id": next_fid, "event_id": event_id, "division": div,
                "a_id": int(a["id"]), "b_id": int(b["id"]),
                "is_top15": 1, "is_main_event": 0, "is_title_fight": 0,
                "card_slot": "",
                "status": "scheduled", "winner_id": "", "method": "", "round": "", "time_mmss": ""
            }
            if _append_fight(fights, fight, event_row):
                next_fid += 1
                top_needed -= 1
                if _is_top_tier_fighter(a) and _is_top_tier_fighter(b):
                    top_tier_count += 1

        while unr_needed > 0 and len(unr_pool) >= 2:
            a = unr_pool.pop(0)
            candidates = [b for b in unr_pool if abs(float(a["rating"]) - float(b["rating"])) <= 120.0]
            candidates = [b for b in candidates if _pair_key(int(a["id"]), int(b["id"])) not in booked_pairs]
            candidates = [b for b in candidates if _pair_key(int(a["id"]), int(b["id"])) not in event_pairs]
            pool_fb = [bb for bb in (candidates or unr_pool) if _pair_key(int(a["id"]), int(bb["id"])) not in booked_pairs]
            pool_fb = [bb for bb in pool_fb if _pair_key(int(a["id"]), int(bb["id"])) not in event_pairs]
            b = pick_best_opponent(a, pool_fb, used, pair_last_map, event_date, is_title_fight=False)
            if b is None:
                break
            unr_pool = [x for x in unr_pool if int(x["id"]) != int(b["id"])]
            used.add(int(a["id"])); used.add(int(b["id"]))
            event_pairs.add(_pair_key(int(a["id"]), int(b["id"])))
            fight = {
                "fight_id": next_fid, "event_id": event_id, "division": div,
                "a_id": int(a["id"]), "b_id": int(b["id"]),
                "is_top15": 0, "is_main_event": 0, "is_title_fight": 0,
                "card_slot": "",
                "status": "scheduled", "winner_id": "", "method": "", "round": "", "time_mmss": ""
            }
            if _append_fight(fights, fight, event_row):
                next_fid += 1
                unr_needed -= 1

        current_total = sum(1 for f in fights if int(f.get("event_id", 0)) == event_id and f.get("division") == div)
        while current_total < target_total:
            pool = top_pool if (random.random() < top_prob and len(top_pool) >= 2) else unr_pool
            is_top = pool is top_pool
            if len(pool) < 2:
                pool = unr_pool if pool is top_pool else top_pool
                is_top = pool is top_pool
            if len(pool) < 2:
                break
            a = pool.pop(0)
            if is_top:
                a_rank = int(float(a["rank_slot"]))
                candidates = [b for b in pool if abs(a_rank - int(float(b["rank_slot"]))) <= 6]
            else:
                candidates = [b for b in pool if abs(float(a["rating"]) - float(b["rating"])) <= 120.0]
            candidates = [b for b in candidates if _pair_key(int(a["id"]), int(b["id"])) not in booked_pairs]
            candidates = [b for b in candidates if _pair_key(int(a["id"]), int(b["id"])) not in event_pairs]
            pool_fb = [bb for bb in (candidates or pool) if _pair_key(int(a["id"]), int(bb["id"])) not in booked_pairs]
            pool_fb = [bb for bb in pool_fb if _pair_key(int(a["id"]), int(bb["id"])) not in event_pairs]
            if is_top and top_tier_count >= top_tier_limit:
                pool_fb = [bb for bb in pool_fb if not (_is_top_tier_fighter(a) and _is_top_tier_fighter(bb))]
            b = pick_best_opponent(a, pool_fb, used, pair_last_map, event_date, is_title_fight=False)
            if b is None:
                break
            pool = [x for x in pool if int(x["id"]) != int(b["id"])]
            if is_top:
                top_pool = pool
            else:
                unr_pool = pool
            used.add(int(a["id"])); used.add(int(b["id"]))
            event_pairs.add(_pair_key(int(a["id"]), int(b["id"])))
            fight = {
                "fight_id": next_fid, "event_id": event_id, "division": div,
                "a_id": int(a["id"]), "b_id": int(b["id"]),
                "is_top15": 1 if is_top else 0, "is_main_event": 0, "is_title_fight": 0,
                "card_slot": "",
                "status": "scheduled", "winner_id": "", "method": "", "round": "", "time_mmss": ""
            }
            if _append_fight(fights, fight, event_row):
                next_fid += 1
                current_total += 1
                if is_top and _is_top_tier_fighter(a) and _is_top_tier_fighter(b):
                    top_tier_count += 1

    _assign_card_slots(fighters_by_div, fights, event_id, main_card_count, prelims_count)
    notes = _json_load_list(event_row.get("notes_json","[]"))
    notes.append("📌 Полный кард сформирован.")
    event_row["notes_json"] = _json_dump(notes)
    return next_fid


# -------------------- Withdrawals --------------------
def process_withdrawals(fighters_by_div, fights, pairs_by_div, event_row: dict, event_date: date, cfg: SimConfig):
    if str(event_row.get("generated_on","")).strip() or str(event_row.get("announced_full_on","")).strip():
        return
    event_id = int(event_row["event_id"])
    notes = _json_load_list(event_row.get("notes_json","[]"))
    divisions = list(fighters_by_div.keys())

    for div in divisions:
        card = [f for f in fights if int(f["event_id"]) == event_id and f.get("division")==div and f.get("status") == "scheduled"]
        used = set()
        for f in card:
            used.add(int(f["a_id"])); used.add(int(f["b_id"]))

        pair_last_map = get_pair_last_map(pairs_by_div.get(div, []))
        top_pool = [x for x in top15_only(fighters_by_div, div) if available(x, event_date) and int(x["id"]) not in used]
        unr_pool = [x for x in unranked_only(fighters_by_div, div) if available(x, event_date) and int(x["id"]) not in used]

        for fight in card:
            if random.random() >= cfg.withdrawal_chance:
                continue
            out_id = int(fight["a_id"]) if random.random() < 0.5 else int(fight["b_id"])
            stay_id = int(fight["b_id"]) if out_id == int(fight["a_id"]) else int(fight["a_id"])

            out_name = fighter_name(fighters_by_div, div, out_id)
            notes.append(news.withdrawal_msg(out_name))

            is_top = int(fight.get("is_top15",0)) == 1
            pool = top_pool if is_top else unr_pool
            stay = find_fighter(fighters_by_div, div, stay_id)

            cand = [p for p in pool if abs(float(p["rating"]) - float(stay["rating"])) <= (180.0 if is_top else 120.0)]
            rep = pick_best_opponent(stay, cand or pool, used, pair_last_map, event_date, is_title_fight=False)
            if rep is None:
                fight["status"] = "cancelled"
                notes.append(news.cancelled_msg(fighter_name(fighters_by_div,div,int(fight["a_id"])), fighter_name(fighters_by_div,div,int(fight["b_id"]))))
                continue
            rep_id = int(rep["id"])
            notes.append(news.replacement_msg(out_name, fighter_name(fighters_by_div, div, rep_id)))
            if out_id == int(fight["a_id"]):
                fight["a_id"] = rep_id
            else:
                fight["b_id"] = rep_id
            used.add(rep_id)
            if is_top:
                top_pool = [x for x in top_pool if int(x["id"]) != rep_id]
            else:
                unr_pool = [x for x in unr_pool if int(x["id"]) != rep_id]

    event_row["notes_json"] = _json_dump(notes)


# -------------------- Run event --------------------
def run_event(fighters_by_div, events, fights, pairs_by_div, save, today: date):
    event_row = next((e for e in events if e["event_date"] == today.isoformat()), None)
    if not event_row or int(event_row.get("completed",0)) == 1:
        return

    if not str(event_row.get("generated_on","")).strip():
        next_fid = int(save.get("next_fight_id","1"))
        next_fid = fill_full_card(fighters_by_div, fights, pairs_by_div, today, today, event_row, next_fid)
        save["next_fight_id"] = str(next_fid)
        event_row["generated_on"] = today.isoformat()
        event_row["announced_full_on"] = today.isoformat()

    cfg = SimConfig()
    process_withdrawals(fighters_by_div, fights, pairs_by_div, event_row, today, cfg)

    event_id = int(event_row["event_id"])
    card = [f for f in fights if int(f["event_id"]) == event_id and f.get("status") == "scheduled"]
    card.sort(key=lambda x: 0 if int(x.get("is_main_event",0))==1 else 1)

    notes = _json_load_list(event_row.get("notes_json","[]"))

    champ_before = {}
    for div in fighters_by_div.keys():
        c = current_champ(fighters_by_div, div)
        if c:
            champ_before[div] = int(c["id"])

    for fight in card:
        div = fight.get("division","Flyweight")
        a_id = int(fight["a_id"])
        b_id = int(fight["b_id"])
        if a_id == b_id:
            fight["status"] = "cancelled"
            notes.append("⚠️ Бой отменён: боец не может драться сам с собой.")
            continue
        a = find_fighter(fighters_by_div, div, a_id)
        b = find_fighter(fighters_by_div, div, b_id)

        res = simulate_fight(a, b, today, cfg)
        winner_id = int(res["winner_id"]); loser_id = int(res["loser_id"])

        fight["status"] = "completed"
        fight["winner_id"] = str(winner_id)

        method, rnd, mmss = random_method_and_time()
        fight["method"] = method
        fight["round"] = str(rnd)
        fight["time_mmss"] = mmss

        a["rating"] = float(res["ra_new"])
        b["rating"] = float(res["rb_new"])

        w = find_fighter(fighters_by_div, div, winner_id)
        l = find_fighter(fighters_by_div, div, loser_id)
        w["wins"] = int(w.get("wins",0)) + 1
        l["losses"] = int(l.get("losses",0)) + 1
        w_st = int(w.get("streak",0)); l_st = int(l.get("streak",0))
        w["streak"] = w_st + 1 if w_st >= 0 else 1
        l["streak"] = l_st - 1 if l_st <= 0 else -1

        a["next_available_date"] = res["next_a"]
        b["next_available_date"] = res["next_b"]
        if int(res["inj_a_extra"]) > 0:
            notes.append(news.injury_msg(a["name"], int(res["inj_a_extra"])))
        if int(res["inj_b_extra"]) > 0:
            notes.append(news.injury_msg(b["name"], int(res["inj_b_extra"])))

        a["last_fight_date"] = today.isoformat()
        b["last_fight_date"] = today.isoformat()

        _rh_append(a, today); _rh_append(b, today)
        set_pair_last(pairs_by_div[div], int(a["id"]), int(b["id"]), today)

        if int(fight.get("is_title_fight",0)) == 1:
            save["last_title_fight_date"] = today.isoformat()
            before_id = champ_before.get(div)
            if before_id is not None and before_id != winner_id:
                old = find_fighter(fighters_by_div, div, before_id); old["is_champ"] = 0
                newc = find_fighter(fighters_by_div, div, winner_id); newc["is_champ"] = 1
                notes.append(news.title_change_msg(newc["name"]))
            else:
                notes.append(f"🏆 Титульный бой завершён: {fighter_name(fighters_by_div, div, winner_id)} защитил пояс.")

    for div in fighters_by_div.keys():
        recompute_top15(fighters_by_div[div])
        for f in fighters_by_div[div]:
            _kh_append(f, today)

    event_row["notes_json"] = _json_dump(notes)
    event_row["completed"] = 1


# -------------------- Queries --------------------
def next_event_row(events, today: date):
    future = []
    for e in events:
        ed = parse_date(e["event_date"])
        if ed and ed >= today and int(e.get("completed",0)) == 0:
            future.append((ed, e))
    future.sort(key=lambda x: x[0])
    return future[0][1] if future else None

def advance_to_next_week(save: dict) -> date:
    today = parse_date(save.get("current_date","2026-01-01"), default=date(2026,1,1))
    if today.weekday() != 5:
        nxt = next_saturday(today)
    else:
        nxt = today + timedelta(days=7)
    save["current_date"] = nxt.isoformat()
    return nxt
