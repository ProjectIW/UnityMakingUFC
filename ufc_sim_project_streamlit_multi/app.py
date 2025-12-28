from __future__ import annotations

import base64
import json
import mimetypes
import re
import shutil
import textwrap
import unicodedata
from pathlib import Path
from datetime import date

import streamlit as st
import pandas as pd
import plotly.express as px
import streamlit.components.v1 as components

import main as game
from utils.io import read_kv


st.set_page_config(page_title="UFC Sim: PRO Manager", layout="wide")

ASSETS_DIR = Path(__file__).resolve().parent / "Design"
if not ASSETS_DIR.exists():
    ASSETS_DIR = Path(__file__).resolve().parent / "design"
COUNTRIES_DIR = ASSETS_DIR / "countries"
FACES_DIR = ASSETS_DIR / "faces"
SAVE_SLOTS = 3

st.markdown("""
<style>
  .stApp { background: #0b0c0f; color: #eaeaea; }
  section[data-testid="stSidebar"] {
    background: rgba(30, 30, 32, 0.5);
    border-right: 1px solid rgba(255,255,255,0.08);
  }
  section[data-testid="stSidebar"] .sidebar-panel,
section[data-testid="stSidebar"] .sidebar-panel * {
  color: #f4f4f5 !important;
}

section[data-testid="stSidebar"] [data-testid="stExpander"] summary,
section[data-testid="stSidebar"] [data-testid="stExpander"] summary * {
  color: #f4f4f5 !important;
}

section[data-testid="stSidebar"] [data-testid="stExpander"] summary svg {
  fill: #f4f4f5 !important;
}

section[data-testid="stSidebar"] [data-testid="stExpander"] {
  background: rgba(255,255,255,0.06);
  border: 1px solid rgba(255,255,255,0.10);
  border-radius: 12px;
  padding: 6px 8px;
}

section[data-testid="stSidebar"] .stButton > button {
  background: rgba(255,255,255,0.10) !important;
  color: #f4f4f5 !important;
  border: 1px solid rgba(255,255,255,0.16) !important;
}

section[data-testid="stSidebar"] input,
section[data-testid="stSidebar"] textarea {
  background: rgba(255,255,255,0.10) !important;
  color: #f4f4f5 !important;
}

/* selectbox/multiselect Streamlit (baseweb) */
section[data-testid="stSidebar"] [data-baseweb="select"] > div {
  background: rgba(255,255,255,0.10) !important;
  color: #f4f4f5 !important;
}

  .hero {
    padding: 22px 28px;
    border-radius: 14px;
    background: linear-gradient(180deg, rgba(20,22,28,0.96) 0%, rgba(9,10,12,0.96) 100%);
    border: 1px solid rgba(255,255,255,0.06);
    margin-bottom: 14px;
  }
  .hero h1 { margin: 0; font-size: 28px; letter-spacing: 0.2px; }
  .hero p { margin: 6px 0 0 0; opacity: 0.78; }

  .fighter-card {
    background: linear-gradient(90deg, rgba(27,27,29,1) 0%, rgba(11,11,12,1) 100%);
    border-left: 4px solid #e62429;
    border-radius: 14px;
    padding: 16px 18px;
    margin: 6px 0;
    display: flex;
    justify-content: space-between;
    align-items: center;
    box-shadow: 0 12px 26px rgba(0,0,0,0.3);
    transition: transform 120ms ease, box-shadow 120ms ease;
  }
  .fighter-card:hover {
    transform: translateY(-1px);
    box-shadow: 0 16px 30px rgba(0,0,0,0.35);
  }
  .fighter-left { display: flex; gap: 14px; align-items: center; }
  .fighter-photo {
    width: 52px;
    height: 52px;
    border-radius: 10px;
    background: rgba(255,255,255,0.08);
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .fighter-photo img { width: 100%; height: 100%; object-fit: cover; }
  .rank-pill {
    width: 34px; height: 34px; border-radius: 10px;
    display:flex; align-items:center; justify-content:center;
    background: rgba(230,36,41,0.14);
    color: #ff5a5f; font-weight: 900;
  }
  .champ-pill { background: rgba(212,175,55,0.14) !important; color: #d4af37 !important; }
  .f-name { font-size: 18px; font-weight: 900; line-height: 1.15; }
  .f-sub { font-size: 12px; opacity: 0.78; margin-top: 2px; display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
  .flag-pill {
    width: 22px;
    height: 14px;
    border-radius: 5px;
    background: rgba(255,255,255,0.14);
    display: inline-flex;
    align-items: center;
    justify-content: center;
    overflow: hidden;
  }
  .flag-pill img { width: 100%; height: 100%; object-fit: cover; display: block; }
  .flag-placeholder { background: rgba(255,255,255,0.18); }

  .right-box { text-align: right; min-width: 140px; }
  .rating-badge {
    display:inline-block; padding: 6px 12px; border-radius: 10px;
    background:#e62429; color:#fff; font-weight: 900; min-width: 62px; text-align:center;
  }
  .streak { font-size: 12px; font-weight: 900; margin-top: 4px; }
  .streak.pos { color: #2ecc71; }
  .streak.neg { color: #ff4d4d; }
  .inj { font-size: 12px; color: #ff5a5f; font-weight: 900; margin-left: 8px; }

  .event-banner {
    background: linear-gradient(90deg, rgba(230,36,41,0.9) 0%, rgba(144,14,20,0.95) 100%);
    border-radius: 12px;
    padding: 14px 16px;
    font-size: 18px;
    font-weight: 900;
    letter-spacing: 0.4px;
    margin: 8px 0 12px 0;
    border: 1px solid rgba(255,255,255,0.06);
  }
  .event-section-title {
    margin: 12px 0 6px 0;
    font-size: 13px;
    letter-spacing: 2px;
    text-transform: uppercase;
    color: rgba(255,255,255,0.65);
    font-weight: 900;
  }
  .fight-row {
    padding: 12px 0;
    border-bottom: 1px solid rgba(255,255,255,0.06);
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 18px;
  }
  .fight-side {
    display: flex;
    align-items: center;
    gap: 8px;
    min-width: 140px;
    justify-content: flex-end;
  }
  .fight-side.winner {
    padding: 4px 6px;
    border-radius: 999px;
    border: 2px solid rgba(46, 204, 113, 0.9);
    box-shadow: 0 0 8px rgba(46, 204, 113, 0.35);
  }
  .fight-side.right {
    justify-content: flex-start;
  }
  .fight-flag {
    width: 28px;
    height: 18px;
    border-radius: 5px;
    overflow: hidden;
    background: rgba(255,255,255,0.16);
  }
  .fight-flag img { width: 100%; height: 100%; object-fit: cover; display: block; }
  .fight-photo {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    background: rgba(255,255,255,0.14);
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;
  }
  .fight-photo img { width: 100%; height: 100%; object-fit: cover; }
  .fight-center {
    text-align: center;
    min-width: 320px;
  }
  .fight-names {
    font-size: 16px;
    font-weight: 900;
  }
  .fight-names .winner { color: #2ecc71; }
  .fight-names .loser { color: rgba(255,255,255,0.55); }
  .fight-names .vs { color: rgba(255,255,255,0.75); padding: 0 6px; }
  .fight-meta {
    font-size: 12px;
    opacity: 0.7;
    margin-top: 2px;
  }
  .w { color: #2ecc71; font-weight: 900; }
  .l { color: rgba(255,255,255,0.45); }
  .tag { opacity:0.65; font-weight: 800; font-size: 11px; text-transform: uppercase; letter-spacing: 1px; }

  details.fighter-details {
    border-radius: 14px;
    margin: 6px 0;
  }
  details.fighter-details > summary {
    list-style: none;
    cursor: pointer;
  }
  details.fighter-details > summary::-webkit-details-marker { display: none; }
  details.fighter-details[open] .fighter-card {
    border-left-color: #ff4d4d;
  }
  .fighter-details-body {
    padding: 12px 18px 18px 18px;
    border-radius: 14px;
    background: rgba(255,255,255,0.02);
    border: 1px solid rgba(255,255,255,0.06);
    margin-top: -6px;
  }
  .details-grid { display: flex; gap: 18px; flex-wrap: wrap; }
  .details-col { flex: 1 1 220px; min-width: 220px; }
  .details-metric { font-weight: 800; margin-bottom: 8px; }
  .details-list { margin: 6px 0 0 0; padding-left: 18px; }
  .details-list li { margin-bottom: 6px; opacity: 0.88; }

  .stTabs [data-baseweb="tab"] {
    cursor: grab;
  }
  .stTabs [data-baseweb="tab"]:active {
    cursor: grabbing;
  }

  .save-hero {
    padding: 26px 28px;
    border-radius: 16px;
    background: radial-gradient(circle at top, rgba(230,36,41,0.18), rgba(10,10,12,0.98));
    border: 1px solid rgba(255,255,255,0.08);
    margin-bottom: 18px;
  }
  .save-hero h2 { margin: 0; font-size: 26px; font-weight: 900; letter-spacing: 0.5px; }
  .save-hero p { margin: 6px 0 0 0; opacity: 0.75; }
  .save-card {
    background: linear-gradient(135deg, rgba(20,20,22,0.96), rgba(8,8,10,0.96));
    border-radius: 14px;
    padding: 16px 18px;
    border: 1px solid rgba(255,255,255,0.06);
    box-shadow: 0 12px 24px rgba(0,0,0,0.35);
  }
  .save-title { font-weight: 900; font-size: 16px; letter-spacing: 0.4px; }
  .save-meta { font-size: 12px; opacity: 0.7; margin-top: 6px; }
  .save-badge {
    display:inline-block;
    padding: 4px 8px;
    border-radius: 999px;
    background: rgba(230,36,41,0.16);
    color: #ff5a5f;
    font-weight: 900;
    font-size: 11px;
    letter-spacing: 0.3px;
  }

  .sidebar-panel {
    padding: 22px 18px;
    border-radius: 16px;
    background: rgba(25, 25, 27, 0.55);
    border: 1px solid rgba(255,255,255,0.08);
    box-shadow: 0 18px 28px rgba(0,0,0,0.35);
  }
  .sidebar-title {
    font-size: 22px;
    font-weight: 900;
    letter-spacing: 0.4px;
    margin-bottom: 4px;
  }
  .sidebar-subtitle {
    font-size: 12px;
    opacity: 0.75;
    margin-bottom: 14px;
  }
  .sidebar-section {
    margin-top: 12px;
  }
  .sidebar-row {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    padding: 8px 0;
    border-bottom: 1px solid rgba(255,255,255,0.08);
  }
  .sidebar-row:last-child { border-bottom: none; }
  .sidebar-label {
    font-size: 12px;
    opacity: 0.7;
    text-transform: uppercase;
    letter-spacing: 1px;
  }
  .sidebar-value {
    font-size: 14px;
    font-weight: 800;
    text-align: right;
  }
  .sidebar-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 4px 8px;
    border-radius: 999px;
    background: rgba(255,255,255,0.08);
    font-size: 11px;
    font-weight: 800;
    letter-spacing: 0.4px;
  }
</style>
""", unsafe_allow_html=True)


def _slot_save_file(slot_id: int) -> Path:
    return game.save_slot_dir(slot_id) / "Data" / "_global" / "save_game.csv"


def _slot_meta(slot_id: int) -> dict | None:
    save_file = _slot_save_file(slot_id)
    if not save_file.exists():
        return None
    try:
        kv = read_kv(save_file)
    except Exception:
        kv = {}
    return {
        "current_date": kv.get("current_date", "—"),
        "random_seed": kv.get("random_seed", "—"),
    }


def _ensure_session_defaults():
    st.session_state.setdefault("active_save_slot", None)
    st.session_state.setdefault("show_save_exit", False)


def _set_active_slot(slot_id: int):
    st.session_state.active_save_slot = slot_id
    st.session_state.show_save_exit = False
    game.set_save_root(game.save_slot_dir(slot_id))
    st.rerun()


def _create_and_activate_slot(slot_id: int):
    game.create_save_slot(slot_id, overwrite=False)
    _set_active_slot(slot_id)


def _exit_to_menu():
    st.session_state.active_save_slot = None
    st.session_state.show_save_exit = False
    game.set_save_root(None)
    st.rerun()

def _clear_save_slot(slot_id: int):
    slot_dir = game.save_slot_dir(slot_id)
    if slot_dir.exists():
        shutil.rmtree(slot_dir)
    if st.session_state.active_save_slot == slot_id:
        _exit_to_menu()


def _render_save_menu():
    st.markdown(
        """
        <div class="save-hero">
          <h2>UFC Manager PRO</h2>
          <p>Вход в карьеру • создание новой игры • управление сохранениями</p>
        </div>
        """,
        unsafe_allow_html=True,
    )

    t_create, t_load = st.tabs(["🆕 СОЗДАТЬ ИГРУ", "💾 ВЫБРАТЬ СОХРАНЕНИЕ"])

    with t_create:
        st.subheader("Новая карьера")
        st.caption("Создайте новую игру в любом свободном слоте.")
        for slot_id in range(1, SAVE_SLOTS + 1):
            exists = game.save_slot_dir_exists(slot_id)
            with st.container():
                left, right = st.columns([3, 1])
                with left:
                    st.markdown(
                        f"""
                        <div class="save-card">
                          <div class="save-title">Слот {slot_id}</div>
                          <div class="save-meta">Статус: {'ЗАНЯТ' if exists else 'СВОБОДЕН'}</div>
                        </div>
                        """,
                        unsafe_allow_html=True,
                    )
                with right:
                    st.button(
                        "Создать",
                        key=f"create_slot_{slot_id}",
                        use_container_width=True,
                        disabled=exists,
                        on_click=_create_and_activate_slot,
                        args=(slot_id,),
                    )

    with t_load:
        st.subheader("Сохранения")
        for slot_id in range(1, SAVE_SLOTS + 1):
            meta = _slot_meta(slot_id)
            exists = meta is not None
            with st.container():
                left, right, right_clear = st.columns([3, 1, 1])
                with left:
                    st.markdown(
                        f"""
                        <div class="save-card">
                          <div class="save-title">Слот {slot_id}</div>
                          <div class="save-meta">Дата в игре: {meta['current_date'] if exists else '—'}</div>
                          <div class="save-meta">Seed: {meta['random_seed'] if exists else '—'}</div>
                          <div class="save-meta">
                            <span class="save-badge">{'ДОСТУПНО' if exists else 'ПУСТО'}</span>
                          </div>
                        </div>
                        """,
                        unsafe_allow_html=True,
                    )
                    if exists:
                        st.checkbox(
                            f"Подтвердить очистку слота {slot_id}",
                            key=f"confirm_clear_slot_{slot_id}",
                        )
                with right:
                    st.button(
                        "Загрузить",
                        key=f"load_slot_{slot_id}",
                        use_container_width=True,
                        disabled=not exists,
                        on_click=_set_active_slot,
                        args=(slot_id,),
                    )
                with right_clear:
                    st.button(
                        "Очистить",
                        key=f"clear_slot_{slot_id}",
                        use_container_width=True,
                        disabled=not exists or not st.session_state.get(f"confirm_clear_slot_{slot_id}", False),
                        on_click=_clear_save_slot,
                        args=(slot_id,),
                    )


_ensure_session_defaults()

if st.session_state.active_save_slot is None:
    _render_save_menu()
    st.stop()

    if not game.save_slot_exists(st.session_state.active_save_slot):
        if not game.save_slot_dir_exists(st.session_state.active_save_slot):
            st.error("Слот не найден. Выберите другое сохранение.")
        else:
            st.error("Сохранение повреждено. Создайте новый слот.")
        if st.button("Вернуться в меню", use_container_width=True):
            _exit_to_menu()
        st.stop()

game.set_save_root(game.save_slot_dir(st.session_state.active_save_slot))


def _parse_date(s: str) -> date | None:
    try:
        return date.fromisoformat(str(s))
    except Exception:
        return None


def _rank_label(f: dict) -> str:
    if int(f.get("is_champ", 0)) == 1:
        return "C"
    slot_raw = f.get("rank_slot", "")
    slot = str(slot_raw).strip()
    if not slot:
        return "-"
    try:
        slot_num = float(slot_raw)
    except (TypeError, ValueError):
        return slot
    if slot_num.is_integer():
        return str(int(slot_num))
    return str(slot_num)


def _is_injured(f: dict, today: date) -> bool:
    nad = _parse_date(f.get("next_available_date", ""))
    return (nad is not None) and (nad > today)


def _history_json(s: str) -> list[dict]:
    s = str(s or "").strip()
    if not s:
        return []
    try:
        v = json.loads(s)
        return v if isinstance(v, list) else []
    except Exception:
        return []


def _rank_series(f: dict) -> pd.DataFrame:
    hist = _history_json(f.get("rank_history", ""))
    xs, ys = [], []
    for p in hist:
        d = _parse_date(p.get("d", ""))
        r = p.get("rank", None)
        if d is None or r is None:
            continue
        xs.append(d.isoformat())
        ys.append(int(r))
    if not xs:
        return pd.DataFrame()
    return pd.DataFrame({"date": xs, "rank": ys})


def _last_5_fights(fights, division: str, fid: int) -> list[dict]:
    out = []
    for ft in fights:
        if ft.get("status") != "completed":
            continue
        if ft.get("division") != division:
            continue
        a_id = int(ft.get("a_id",0)); b_id = int(ft.get("b_id",0))
        if fid not in (a_id, b_id):
            continue
        out.append(ft)
    out.sort(key=lambda x: int(x.get("fight_id",0)), reverse=True)
    return out[:5]


def _slugify_country(value: str) -> str:
    text = unicodedata.normalize("NFKD", str(value or "")).encode("ascii", "ignore").decode("ascii")
    text = text.lower().strip()
    text = re.sub(r"[^a-z0-9]+", "_", text)
    return text.strip("_")


def _image_data_uri(path: Path) -> str | None:
    if not path.exists() or not path.is_file():
        return None
    mime, _ = mimetypes.guess_type(path.name)
    if not mime:
        mime = "image/png"
    data = base64.b64encode(path.read_bytes()).decode("utf-8")
    return f"data:{mime};base64,{data}"



# --- Sidebar background image (Design/sidebar_bg.png) + 50% gray overlay ---
_SIDEBAR_BG_URI = _image_data_uri(ASSETS_DIR / "sidebar_bg.jpg")
if _SIDEBAR_BG_URI:
    st.markdown(
        f"""
        <style>
        section[data-testid="stSidebar"] {{
            position: relative;
            overflow: hidden;
            background-color: transparent !important; /* override previous rgba background */
        }}
        section[data-testid="stSidebar"]::before {{
            content: "";
            position: absolute;
            inset: -12px;
            background-image: url("{_SIDEBAR_BG_URI}");
            background-size: cover;
            background-position: center;
            background-repeat: no-repeat;
            filter: blur(10px);
            transform: scale(1.05);
            z-index: 0;
            pointer-events: none;
        }}
        section[data-testid="stSidebar"]::after {{
            content: "";
            position: absolute;
            inset: 0;
            background: rgba(30, 30, 32, 0.50); /* 50% grey overlay */
            z-index: 1;
            pointer-events: none;
        }}
        section[data-testid="stSidebar"] > div {{
            position: relative;
            z-index: 2;
        }}
        </style>
        """,
        unsafe_allow_html=True,
    )

def _country_flag_uri(country: str) -> str | None:
    slug = _slugify_country(country)
    if not slug:
        return None
    for ext in ("png", "jpg", "jpeg", "webp", "svg"):
        candidate = COUNTRIES_DIR / f"{slug}.{ext}"
        uri = _image_data_uri(candidate)
        if uri:
            return uri
    return None


def _fighter_face_uri(fid: int) -> str | None:
    if fid <= 0:
        return None
    for ext in ("png", "jpg", "jpeg", "webp"):
        candidate = FACES_DIR / f"{fid}.{ext}"
        uri = _image_data_uri(candidate)
        if uri:
            return uri
    return None


def _fighter_info(fighters_by_div, division: str, fid: int) -> dict:
    try:
        return game.find_fighter(fighters_by_div, division, fid)
    except Exception:
        return {"id": fid, "name": f"#{fid}", "country": ""}


def _fight_card_slot(ft: dict) -> str:
    slot = str(ft.get("card_slot","")).strip()
    if slot:
        return slot
    if int(ft.get("is_main_event",0)) == 1:
        return "MAIN_EVENT"
    return "MAIN_CARD"


def _filtered_notes(notes: list[str]) -> list[str]:
    filtered = []
    for n in notes:
        if "🩼" in n or "🏆" in n:
            filtered.append(n)
    return filtered


def step_next_week():
    fighters_by_div, events, fights, pairs_by_div, save = game.load_state()
    today = _parse_date(save.get("current_date", "2026-01-01")) or date(2026, 1, 1)

    try:
        import random
        random.seed(int(save.get("random_seed","12345")))
    except Exception:
        pass

    if game.ensure_histories_initialized(fighters_by_div, today):
        game.save_state(fighters_by_div, events, fights, pairs_by_div, save)

    game.ensure_events_planned(fighters_by_div, events, fights, pairs_by_div, save, today)
    today2 = game.advance_to_next_week(save)
    game.ensure_events_planned(fighters_by_div, events, fights, pairs_by_div, save, today2)
    if today2.weekday() == 5:
        game.run_event(fighters_by_div, events, fights, pairs_by_div, save, today2)

    game.save_state(fighters_by_div, events, fights, pairs_by_div, save)
    st.session_state.toast = f"✅ Неделя промотана: {today.isoformat()} → {today2.isoformat()}"


def reset_seed(new_seed: int):
    fighters_by_div, events, fights, pairs_by_div, save = game.load_state()
    save["random_seed"] = str(int(new_seed))
    game.save_state(fighters_by_div, events, fights, pairs_by_div, save)


fighters_by_div, events, fights, pairs_by_div, save = game.load_state()
today = _parse_date(save.get("current_date", "2026-01-01")) or date(2026, 1, 1)

if game.ensure_histories_initialized(fighters_by_div, today):
    game.save_state(fighters_by_div, events, fights, pairs_by_div, save)
    fighters_by_div, events, fights, pairs_by_div, save = game.load_state()

game.ensure_events_planned(fighters_by_div, events, fights, pairs_by_div, save, today)
game.save_state(fighters_by_div, events, fights, pairs_by_div, save)

divisions = list(fighters_by_div.keys())


with st.sidebar:
    st.markdown(
        f"""
        <div class="sidebar-panel">
          <div class="sidebar-title">UFC Manager PRO</div>
          <div class="sidebar-subtitle">Мульти-вес • симуляция по субботам</div>
          <div class="sidebar-section">
            <div class="sidebar-row">
              <div class="sidebar-label">Текущая дата</div>
              <div class="sidebar-value">{today.isoformat()}</div>
            </div>
          </div>
        </div>
        """,
        unsafe_allow_html=True,
    )

    nxt = game.next_event_row(events, today)
    if nxt:
        fixed = bool(str(nxt.get("generated_on","")).strip())
        display = game.event_display_name(
            nxt.get("event_kind","FIGHT_NIGHT"),
            nxt.get("event_id",""),
            nxt.get("location",""),
            nxt.get("theme_country",""),
        )
        st.markdown(
            f"""
            <div class="sidebar-panel" style="margin-top: 14px;">
              <div class="sidebar-section">
                <div class="sidebar-row">
                  <div class="sidebar-label">Ближайший ивент</div>
                  <div class="sidebar-value">{nxt['event_date']}</div>
                </div>
                <div class="sidebar-row">
                  <div class="sidebar-label">Тип</div>
                  <div class="sidebar-value">{display}</div>
                </div>
                <div class="sidebar-row">
                  <div class="sidebar-label">Кард</div>
                  <div class="sidebar-value">{'Зафиксирован' if fixed else 'Формируется'}</div>
                </div>
              </div>
            </div>
            """,
            unsafe_allow_html=True,
        )

    st.divider()
    st.button("Провести ивент (16–20 боёв)", use_container_width=True, on_click=step_next_week)

    with st.expander("Настройки"):
        seed = st.number_input("Random seed", min_value=1, max_value=10_000_000, value=int(save.get("random_seed","12345")))
        st.button("Применить seed", use_container_width=True, on_click=reset_seed, args=(seed,))

    if st.session_state.get("toast"):
        st.success(st.session_state.toast)
        st.session_state.toast = None

    st.divider()
    if st.button("Сохранить и выйти", use_container_width=True):
        st.session_state.show_save_exit = True
        st.rerun()


if st.session_state.show_save_exit:
    st.markdown("## 💾 Сохранить и выйти")
    st.caption("Выберите свободный слот или подтвердите перезапись существующего сохранения.")
    current_slot = st.session_state.active_save_slot

    def _save_and_exit(target_slot: int, overwrite: bool):
        game.save_state(fighters_by_div, events, fights, pairs_by_div, save)
        if target_slot != current_slot:
            game.copy_save_slot(current_slot, target_slot, overwrite=overwrite)
        _exit_to_menu()

    for slot_id in range(1, SAVE_SLOTS + 1):
        meta = _slot_meta(slot_id)
        exists = meta is not None
        label = "Текущий слот" if slot_id == current_slot else ("Занят" if exists else "Свободен")
        with st.container():
            left, right = st.columns([3, 1])
            with left:
                st.markdown(
                    f"""
                    <div class="save-card">
                      <div class="save-title">Слот {slot_id}</div>
                      <div class="save-meta">Статус: {label}</div>
                      <div class="save-meta">Дата в игре: {meta['current_date'] if exists else '—'}</div>
                    </div>
                    """,
                    unsafe_allow_html=True,
                )
                allow_overwrite = True
                if exists and slot_id != current_slot:
                    allow_overwrite = st.checkbox(
                        f"Перезаписать слот {slot_id}",
                        key=f"overwrite_slot_{slot_id}",
                    )
            with right:
                st.button(
                    "Сохранить",
                    key=f"save_exit_{slot_id}",
                    use_container_width=True,
                    disabled=exists and slot_id != current_slot and not allow_overwrite,
                    on_click=_save_and_exit,
                    args=(slot_id, exists and slot_id != current_slot),
                )

    if st.button("Отмена", use_container_width=True):
        st.session_state.show_save_exit = False
        st.rerun()

st.markdown(f"""
<div class="hero">
  <h1>UFC Sim: PRO Manager</h1>
  <p>Несколько весов в одном ивенте • типы ивентов • история • травмы/снятия • методы побед.</p>
</div>
""", unsafe_allow_html=True)

components.html(
    """
    <script>
      const ensureSortable = () => {
        if (window.parent.Sortable) {
          return Promise.resolve();
        }
        return new Promise((resolve, reject) => {
          const script = window.parent.document.createElement("script");
          script.src = "https://cdn.jsdelivr.net/npm/sortablejs@1.15.2/Sortable.min.js";
          script.onload = resolve;
          script.onerror = reject;
          window.parent.document.head.appendChild(script);
        });
      };

      const initSortableTabs = () => {
        const tabWrappers = window.parent.document.querySelectorAll('div[data-testid="stTabs"]');
        tabWrappers.forEach((wrapper) => {
          const tabList = wrapper.querySelector('div[data-baseweb="tab-list"]');
          if (!tabList || tabList.dataset.sortableInit) {
            return;
          }
          const panelsHost = wrapper.querySelector('div[data-baseweb="tab-panel"]')?.parentElement;
          tabList.dataset.sortableInit = "true";
          new window.parent.Sortable(tabList, {
            animation: 150,
            draggable: '[data-baseweb="tab"]',
            onEnd: (evt) => {
              if (!panelsHost || evt.oldIndex === evt.newIndex) {
                return;
              }
              const panels = panelsHost.querySelectorAll('div[data-baseweb="tab-panel"]');
              const moved = panels[evt.oldIndex];
              const target = panels[evt.newIndex];
              if (!moved || !target) {
                return;
              }
              if (evt.newIndex > evt.oldIndex) {
                target.after(moved);
              } else {
                target.before(moved);
              }
            }
          });
        });
      };

      ensureSortable()
        .then(() => {
          const tabsObserver = new MutationObserver(initSortableTabs);
          tabsObserver.observe(window.parent.document.body, { childList: true, subtree: true });
          initSortableTabs();
        })
        .catch((err) => console.error("SortableJS failed to load", err));
    </script>
    """,
    height=0,
    width=0,
)

t1, t2, t3 = st.tabs(["🏆 РЕЙТИНГ", "🗓️ ИВЕНТЫ", "📜 ПРОШЕДШИЕ ИВЕНТЫ"])

with t1:
    st.subheader("Официальный ростер")
    q = st.text_input("Поиск бойца…", placeholder="Имя или страна")

    div_tabs = st.tabs([f"⚡ {d}" for d in divisions]) if divisions else []
    for i, div in enumerate(divisions):
        with div_tabs[i]:
            df = pd.DataFrame(fighters_by_div.get(div, [])).copy()
            if df.empty:
                st.info("Нет бойцов в этой категории.")
                continue

            for col in ["rating","wins","losses","age","streak","is_champ"]:
                if col in df.columns:
                    df[col] = pd.to_numeric(df[col], errors="coerce").fillna(0)

            if q.strip():
                qq = q.strip().lower()
                df = df[df["name"].str.lower().str.contains(qq) | df["country"].str.lower().str.contains(qq)]

            df = df.sort_values(["is_champ","rating"], ascending=[False, False])

            ed_by_id = {int(e.get("event_id",0)): e.get("event_date","") for e in events if str(e.get("event_id","")).strip()}

            for _, r in df.iterrows():
                f = r.to_dict()
                rank = _rank_label(f)
                is_ch = int(f.get("is_champ",0)) == 1
                inj = _is_injured(f, today)
                streak = int(f.get("streak",0))
                streak_cls = "pos" if streak >= 0 else "neg"
                rec = f"Record: {int(f.get('wins',0))}-{int(f.get('losses',0))}"
                country = str(f.get("country","")).strip()
                subtitle = f"{int(f.get('age',0))} лет | {rec}"
                flag_uri = _country_flag_uri(country)
                face_uri = _fighter_face_uri(int(f.get("id",0)))
                flag_html = (
                    f"<img src='{flag_uri}' alt='{country} flag' />"
                    if flag_uri
                    else "<div class='flag-placeholder' style='width:100%; height:100%;'></div>"
                )
                face_html = (
                    f"<img src='{face_uri}' alt='{f.get('name','')}' />"
                    if face_uri
                    else ""
                )

                fid = int(f.get("id",0))
                last5 = _last_5_fights(fights, div, fid)
                fights_html = ""
                if last5:
                    items = []
                    for ft in last5:
                        a_id = int(ft["a_id"]); b_id = int(ft["b_id"])
                        opp_id = b_id if fid == a_id else a_id
                        opp_name = game.fighter_name(fighters_by_div, div, opp_id)
                        w_id = int(ft.get("winner_id") or 0)
                        res = "W" if w_id == fid else "L"
                        dt = ed_by_id.get(int(ft.get("event_id",0)), "")
                        method = ft.get("method","")
                        rnd = ft.get("round","")
                        tm = ft.get("time_mmss","")
                        items.append(f"<li><strong>{res}</strong> vs {opp_name} • {dt} • {method} R{rnd} {tm}</li>")
                    fights_html = "<ul class='details-list'>" + "".join(items) + "</ul>"
                else:
                    fights_html = "<div style='opacity:0.7;'>Пока нет боёв в истории.</div>"

                rdf = _rank_series(f)
                if not rdf.empty and rdf["rank"].nunique() > 1:
                    fig = px.line(rdf, x="date", y="rank", height=220)
                    fig.update_yaxes(autorange="reversed", title=None)
                    fig.update_layout(margin=dict(l=0,r=0,t=10,b=0),
                                      paper_bgcolor='rgba(0,0,0,0)',
                                      plot_bgcolor='rgba(0,0,0,0)',
                                      xaxis_title=None)
                    chart_html = fig.to_html(include_plotlyjs="cdn", full_html=False)
                else:
                    chart_html = "<div style='opacity:0.7;'>График рангов появится после первых изменений позиций.</div>"

                details_html = (
                    "<details class=\"fighter-details\">"
                    "<summary>"
                    "<div class=\"fighter-card\">"
                    "<div class=\"fighter-left\">"
                    f"<div class=\"fighter-photo\">{face_html}</div>"
                    f"<div class=\"rank-pill {'champ-pill' if is_ch else ''}\">{rank}</div>"
                    "<div>"
                    f"<div class=\"f-name\">{f.get('name','')}</div>"
                    "<div class=\"f-sub\">"
                    f"<span class=\"flag-pill\">{flag_html}</span>"
                    f"<span>{subtitle}</span>"
                    f"{'<span class=inj>🚑 В ЛАЗАРЕТЕ</span>' if inj else ''}"
                    "</div>"
                    "</div>"
                    "</div>"
                    "<div class=\"right-box\">"
                    "<div style=\"font-size:10px; opacity:0.6; font-weight:900;\">ELO</div>"
                    f"<div class=\"rating-badge\">{float(f.get('rating',1500.0)):.0f}</div>"
                    f"<div class=\"streak {streak_cls}\">Streak: {streak:+d}</div>"
                    "</div>"
                    "</div>"
                    "</summary>"
                    "<div class=\"fighter-details-body\">"
                    "<div class=\"details-grid\">"
                    "<div class=\"details-col\">"
                    f"<div class=\"details-metric\">Рейтинг: {float(f.get('rating',1500.0)):.0f}</div>"
                    f"<div class=\"details-metric\">Возраст: {int(f.get('age',0))}</div>"
                    f"<div class=\"details-metric\">Статус: {'Недоступен' if inj else 'Доступен'}</div>"
                    "</div>"
                    "<div class=\"details-col\">"
                    "<div class=\"details-metric\">Последние 5 поединков:</div>"
                    f"{fights_html}"
                    "</div>"
                    "</div>"
                    f"{chart_html}"
                    "</div>"
                    "</details>"
                )

                st.markdown(details_html, unsafe_allow_html=True)

def _render_fight_row(ft: dict, show_results: bool):
    div = ft.get("division","")
    a_id = int(ft["a_id"]); b_id = int(ft["b_id"])
    a_info = _fighter_info(fighters_by_div, div, a_id)
    b_info = _fighter_info(fighters_by_div, div, b_id)
    a_name = a_info.get("name", f"#{a_id}")
    b_name = b_info.get("name", f"#{b_id}")
    a_flag = _country_flag_uri(a_info.get("country",""))
    b_flag = _country_flag_uri(b_info.get("country",""))
    a_face = _fighter_face_uri(a_id)
    b_face = _fighter_face_uri(b_id)

    a_flag_html = f"<img src='{a_flag}' alt='' />" if a_flag else ""
    b_flag_html = f"<img src='{b_flag}' alt='' />" if b_flag else ""
    a_face_html = f"<img src='{a_face}' alt='' />" if a_face else ""
    b_face_html = f"<img src='{b_face}' alt='' />" if b_face else ""
    a_flag_block = f"<div class='fight-flag'>{a_flag_html}</div>" if a_flag_html else "<div class='fight-flag'></div>"
    b_flag_block = f"<div class='fight-flag'>{b_flag_html}</div>" if b_flag_html else "<div class='fight-flag'></div>"
    a_photo_block = f"<div class='fight-photo'>{a_face_html}</div>"
    b_photo_block = f"<div class='fight-photo'>{b_face_html}</div>"

    slot = _fight_card_slot(ft)
    slot_map = {
        "MAIN_EVENT": "Main Event",
        "CO_MAIN": "Co-Main Event",
        "MAIN_CARD": "Main Card",
        "PRELIMS": "Prelims",
    }
    slot_label = slot_map.get(slot, slot.title())
    is_title = int(ft.get("is_title_fight",0)) == 1
    label = f"{div} • {slot_label}{' • Title' if is_title else ''}"

    status = ft.get("status","scheduled")
    winner_id = int(ft.get("winner_id") or 0)
    a_wins = status == "completed" and show_results and winner_id == a_id
    b_wins = status == "completed" and show_results and winner_id == b_id

    if status == "cancelled":
        center = f"<div class='fight-names'><span class='loser'>❌ {a_name} vs {b_name} — отменён</span></div>"
        meta = f"<div class='fight-meta tag'>{label}</div>"
    elif status == "completed" and show_results:
        method = ft.get("method","")
        rnd = ft.get("round","")
        tm = ft.get("time_mmss","")
        a_class = "winner" if a_wins else ("loser" if b_wins else "")
        b_class = "winner" if b_wins else ("loser" if a_wins else "")
        center = (
            "<div class='fight-names'>"
            f"<span class='{a_class}'>{a_name}</span>"
            "<span class='vs'>vs</span>"
            f"<span class='{b_class}'>{b_name}</span>"
            "</div>"
        )
        meta = f"<div class='fight-meta'>{method} R{rnd} {tm} • <span class='tag'>{label}</span></div>"
    else:
        center = f"<div class='fight-names'>🥊 {a_name} <span class='vs'>vs</span> {b_name}</div>"
        meta = f"<div class='fight-meta tag'>{label}</div>"

    left_class = "fight-side winner" if a_wins else "fight-side"
    right_class = "fight-side right winner" if b_wins else "fight-side right"

    st.markdown(
        "<div class='fight-row'>"
        f"<div class='{left_class}'>{a_flag_block}{a_photo_block}</div>"
        f"<div class='fight-center'>{center}{meta}</div>"
        f"<div class='{right_class}'>{b_photo_block}{b_flag_block}</div>"
        "</div>",
        unsafe_allow_html=True
    )


def _render_event_list(events_sorted: list[dict], show_completed: bool, limit: int):
    shown = 0
    for e in events_sorted:
        if shown >= limit:
            break
        event_date = _parse_date(e.get("event_date",""))
        if event_date is None:
            continue
        is_done = int(e.get("completed",0)) == 1
        if show_completed != is_done:
            continue

        display = game.event_display_name(
            e.get("event_kind","FIGHT_NIGHT"),
            e.get("event_id",""),
            e.get("location",""),
            e.get("theme_country",""),
        )
        title = f"{'✅' if is_done else '⏳'} {display} • {event_date.isoformat()}"

        with st.expander(title, expanded=(shown == 0)):
            st.markdown(f"<div class='event-banner'>{title}</div>", unsafe_allow_html=True)

            notes = _filtered_notes(_history_json(e.get("notes_json","[]")))
            if notes:
                st.caption("Новости")
                for n in notes[-12:]:
                    st.write(n)
                st.divider()

            event_id = int(e.get("event_id",0))
            card = [f for f in fights if int(f.get("event_id",0)) == event_id]

            if not card:
                st.caption("Кард ещё не собран (слишком далеко до даты).")
            else:
                groups = {
                    "MAIN_EVENT": [],
                    "CO_MAIN": [],
                    "MAIN_CARD": [],
                    "PRELIMS": [],
                }
                for ft in card:
                    slot = _fight_card_slot(ft)
                    groups.setdefault(slot, []).append(ft)
                for slot in groups:
                    groups[slot].sort(key=lambda x: (-int(x.get("is_top15",0)), int(x.get("fight_id",0))))

                order = [
                    ("MAIN_EVENT", "Main Event"),
                    ("CO_MAIN", "Co-Main Event"),
                    ("MAIN_CARD", "Main Card"),
                    ("PRELIMS", "Prelims"),
                ]
                for slot, label in order:
                    if not groups.get(slot):
                        continue
                    st.markdown(f"<div class='event-section-title'>{label}</div>", unsafe_allow_html=True)
                    for ft in groups[slot]:
                        _render_fight_row(ft, show_results=is_done)

        shown += 1


with t2:
    st.subheader("Ближайшие ивенты")

    def ed(e):
        d = _parse_date(e.get("event_date",""))
        return d or date(1900,1,1)

    events_sorted = sorted(events, key=ed, reverse=True)
    limit = st.slider("Сколько ивентов показать", 5, 40, 12)
    _render_event_list(events_sorted, show_completed=False, limit=limit)


with t3:
    st.subheader("Прошедшие ивенты")

    def ed_hist(e):
        d = _parse_date(e.get("event_date",""))
        return d or date(1900,1,1)

    events_sorted_hist = sorted(events, key=ed_hist, reverse=True)
    limit_hist = st.slider("Сколько ивентов показать", 5, 40, 12, key="past-events-limit")
    _render_event_list(events_sorted_hist, show_completed=True, limit=limit_hist)
