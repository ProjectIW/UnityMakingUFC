# UFC Flyweight Console Simulator (MVP)

Консольный симулятор UFC (Flyweight) с недельной промоткой времени.

**Ивенты всегда по субботам.**

## Что реализовано
- Старт игры: 2026-01-01
- Каждая кнопка `Next Week` переносит игру на ближайшую субботу (первый раз) или на +7 дней дальше
- Ивенты создаются в горизонте 12 недель вперёд
- Main Event анонсируется за 8 недель, полный кард фиксируется за 4 недели
- 8 боёв: 4 top-15 + 4 вне топ-15
- Чемпион дерётся примерно раз в 8 недель (если доступен)
- Бой: Elo + "форма дня" (шум) + возраст + серия
- Снятие/замена: 10%; травма после боя: 12% (и новостные плашки)
- Сохранение истории: `data/events.csv` и `data/fights.csv`

## Запуск
```bash
pip install -r requirements.txt
python main.py
```

## Данные
Все CSV в `data/`:
- fighters.csv
- events.csv
- fights.csv
- pair_history.csv
- save_game.csv (key/value)


## Streamlit UI
```bash
streamlit run app.py
```

## Дизайн-ассеты (флаги и лица бойцов)
Ассеты лежат в папке `Design/`:
- `Design/countries/` — мини-флаги стран.
- `Design/faces/` — квадратные фото бойцов.

### Как привязать флаги к странам
В `app.py` используется нормализация названия страны в slug (латиница, нижний регистр, пробелы → `_`).
Примеры:
- `United States` → `united_states.png`
- `Brazil` → `brazil.png`
- `Czech Republic` → `czech_republic.png`

Поддерживаемые расширения: `png`, `jpg`, `jpeg`, `webp`, `svg`.

### Как привязать фото к бойцам
Фото бойцов берутся по ID из CSV (`data/*/fighters.csv`, колонка `id`).
Имя файла = ID бойца:
- `Design/faces/101.png`
- `Design/faces/101.jpg`

Поддерживаемые расширения: `png`, `jpg`, `jpeg`, `webp`.


## Multi-division layout
- data/Flyweight/fighters.csv
- data/Bantamweight/fighters.csv
- data/<Division>/pair_history.csv
Global state:
- data/_global/events.csv
- data/_global/fights.csv
- data/_global/save_game.csv

Run:
```bash
streamlit run app.py
```
