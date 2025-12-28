"""News strings generator."""
from __future__ import annotations

def withdrawal_msg(name: str) -> str:
    return f"⚠️ Снятие: {name} выбыл из боя (травма/болезнь)."

def replacement_msg(out_name: str, in_name: str) -> str:
    return f"🔁 Замена: вместо {out_name} выходит {in_name}."

def cancelled_msg(a: str, b: str) -> str:
    return f"❌ Бой отменён: {a} vs {b} (не найден заменяющий)."

def injury_msg(name: str, extra_days: int) -> str:
    weeks = max(1, extra_days // 7)
    return f"🩼 Травма: {name} выбыл минимум на {weeks} нед."

def result_msg(winner: str, loser: str) -> str:
    return f"✅ Результат: {winner} победил {loser}."

def title_change_msg(new_champ: str) -> str:
    return f"🏆 Новый чемпион: {new_champ}!"
