using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UFC.Core.Calendar;
using UFC.Core.Matchmaking;
using UFC.Core.Models;
using UFC.Core.Ranking;
using UFC.Core.Simulation;
using UFC.Infrastructure.Data;
using UnityEngine;

namespace UFC.Core.Game
{
    public class GameService
    {
        private readonly GameDatabase _database;
        private readonly System.Random _rng;

        public GameService(GameDatabase database, int? seed = null)
        {
            _database = database;
            _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public GameState LoadState()
        {
            return _database.LoadState();
        }

        public void SaveState(GameState state)
        {
            _database.SaveState(state);
        }

        public bool EnsureHistoriesInitialized(GameState state, DateTime startDate)
        {
            bool changed = false;
            foreach (var kvp in state.FightersByDivision)
            {
                string div = kvp.Key;
                foreach (var fighter in kvp.Value)
                {
                    if (string.IsNullOrWhiteSpace(fighter.Division))
                    {
                        fighter.Division = div;
                        changed = true;
                    }
                    if (string.IsNullOrWhiteSpace(fighter.RatingHistory))
                    {
                        fighter.RatingHistory = SerializeHistory(new List<RatingHistoryEntry>
                        {
                            new RatingHistoryEntry { d = startDate.ToString("yyyy-MM-dd"), r = fighter.Rating.ToString("F2", CultureInfo.InvariantCulture) }
                        });
                        changed = true;
                    }
                    if (string.IsNullOrWhiteSpace(fighter.RankHistory))
                    {
                        string rank = string.IsNullOrWhiteSpace(fighter.RankSlot) ? string.Empty : fighter.RankSlot;
                        if (fighter.IsChamp == 1)
                        {
                            rank = "0";
                        }
                        fighter.RankHistory = SerializeHistory(new List<RankHistoryEntry>
                        {
                            new RankHistoryEntry { d = startDate.ToString("yyyy-MM-dd"), rank = rank }
                        });
                        changed = true;
                    }
                }
            }
            return changed;
        }

        public void EnsureEventsPlanned(GameState state, DateTime today)
        {
            var cfg = new PlanConfig();
            int nextEventId = int.Parse(state.Save.NextEventId ?? "1");
            int nextFightId = int.Parse(state.Save.NextFightId ?? "1");

            var indexByDate = state.Events.ToDictionary(e => e.EventDate, e => e);
            foreach (var eventDate in CalendarPlanner.EventDatesInHorizon(today, cfg.HorizonWeeks, _rng))
            {
                string dateKey = eventDate.ToString("yyyy-MM-dd");
                if (!indexByDate.ContainsKey(dateKey))
                {
            string kind = PickEventKind();
                    var (location, theme) = PickLocationAndTheme(state.FightersByDivision, kind);
                    var row = new EventRow
                    {
                        EventId = nextEventId,
                        EventDate = dateKey,
                        GeneratedOn = string.Empty,
                        AnnouncedMainOn = string.Empty,
                        AnnouncedFullOn = string.Empty,
                        Completed = 0,
                        MainFightId = string.Empty,
                        EventKind = kind,
                        Location = location,
                        ThemeCountry = theme,
                        NotesJson = "[]"
                    };
                    state.Events.Add(row);
                    indexByDate[dateKey] = row;
                    nextEventId++;
                }
            }

            CancelSelfFights(state.Events, state.Fights);

            foreach (var ev in state.Events)
            {
                var eventDate = DateUtil.ParseDate(ev.EventDate);
                if (!eventDate.HasValue || ev.Completed == 1)
                {
                    continue;
                }

                if (today >= CalendarPlanner.MainAnnounceDate(eventDate.Value, cfg) && string.IsNullOrWhiteSpace(ev.AnnouncedMainOn))
                {
                    nextFightId = PlanMainEvent(state, today, eventDate.Value, ev, nextFightId);
                    ev.AnnouncedMainOn = today.ToString("yyyy-MM-dd");
                }

                if (today >= CalendarPlanner.FullGenerateDate(eventDate.Value, cfg) && string.IsNullOrWhiteSpace(ev.GeneratedOn))
                {
                    nextFightId = FillFullCard(state, today, eventDate.Value, ev, nextFightId);
                    ev.GeneratedOn = today.ToString("yyyy-MM-dd");
                    ev.AnnouncedFullOn = today.ToString("yyyy-MM-dd");
                }
            }

            state.Save.NextEventId = nextEventId.ToString();
            state.Save.NextFightId = nextFightId.ToString();
        }

        public void RunEvent(GameState state, DateTime today)
        {
            var eventRow = state.Events.FirstOrDefault(e => e.EventDate == today.ToString("yyyy-MM-dd"));
            if (eventRow == null || eventRow.Completed == 1)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(eventRow.GeneratedOn))
            {
                int nextFightId = int.Parse(state.Save.NextFightId ?? "1");
                nextFightId = FillFullCard(state, today, today, eventRow, nextFightId);
                state.Save.NextFightId = nextFightId.ToString();
                eventRow.GeneratedOn = today.ToString("yyyy-MM-dd");
                eventRow.AnnouncedFullOn = today.ToString("yyyy-MM-dd");
            }

            var cfg = new SimConfig();
            ProcessWithdrawals(state, eventRow, today, cfg);

            int eventId = eventRow.EventId;
            var card = state.Fights.Where(f => f.EventId == eventId && f.Status == "scheduled")
                .OrderBy(f => f.IsMainEvent == 1 ? 0 : 1)
                .ToList();

            var notes = JsonList(eventRow.NotesJson);

            var champBefore = new Dictionary<string, int>();
            foreach (var div in state.FightersByDivision.Keys)
            {
                var champ = CurrentChamp(state, div);
                if (champ != null)
                {
                    champBefore[div] = champ.Id;
                }
            }

            foreach (var fight in card)
            {
                string div = fight.Division ?? "Flyweight";
                int aId = fight.AId;
                int bId = fight.BId;
                if (aId == bId)
                {
                    fight.Status = "cancelled";
                    notes.Add("⚠️ Бой отменён: боец не может драться сам с собой.");
                    continue;
                }

                var a = FindFighter(state, div, aId);
                var b = FindFighter(state, div, bId);

                var result = FightSimulation.SimulateFight(a, b, today, cfg, _rng);
                int winnerId = result.WinnerId;
                int loserId = result.LoserId;

                fight.Status = "completed";
                fight.WinnerId = winnerId.ToString();
                var (method, round, time) = FightSimulation.RandomMethodAndTime(_rng);
                fight.Method = method;
                fight.Round = round.ToString();
                fight.TimeMmss = time;

                a.Rating = result.RaNew;
                b.Rating = result.RbNew;

                var winner = FindFighter(state, div, winnerId);
                var loser = FindFighter(state, div, loserId);
                winner.Wins += 1;
                loser.Losses += 1;
                winner.Streak = winner.Streak >= 0 ? winner.Streak + 1 : 1;
                loser.Streak = loser.Streak <= 0 ? loser.Streak - 1 : -1;

                a.NextAvailableDate = result.NextA.ToString("yyyy-MM-dd");
                b.NextAvailableDate = result.NextB.ToString("yyyy-MM-dd");
                if (result.InjAExtra > 0)
                {
                    notes.Add(NewsService.InjuryMsg(a.Name, result.InjAExtra));
                }
                if (result.InjBExtra > 0)
                {
                    notes.Add(NewsService.InjuryMsg(b.Name, result.InjBExtra));
                }

                a.LastFightDate = today.ToString("yyyy-MM-dd");
                b.LastFightDate = today.ToString("yyyy-MM-dd");

                AppendRatingHistory(a, today);
                AppendRatingHistory(b, today);
                SetPairLast(state.PairsByDivision[div], a.Id, b.Id, today);

                if (fight.IsTitleFight == 1)
                {
                    state.Save.LastTitleFightDate = today.ToString("yyyy-MM-dd");
                    champBefore.TryGetValue(div, out var beforeId);
                    if (beforeId != 0 && beforeId != winnerId)
                    {
                        var oldChamp = FindFighter(state, div, beforeId);
                        oldChamp.IsChamp = 0;
                        var newChamp = FindFighter(state, div, winnerId);
                        newChamp.IsChamp = 1;
                        notes.Add(NewsService.TitleChangeMsg(newChamp.Name));
                    }
                    else
                    {
                        notes.Add($"🏆 Титульный бой завершён: {FighterName(state, div, winnerId)} защитил пояс.");
                    }
                }
            }

            foreach (var div in state.FightersByDivision.Keys)
            {
                RankingManager.RecomputeTop15(state.FightersByDivision[div]);
                foreach (var fighter in state.FightersByDivision[div])
                {
                    AppendRankHistory(fighter, today);
                }
            }

            eventRow.NotesJson = DumpJsonList(notes);
            eventRow.Completed = 1;
        }

        public DateTime AdvanceToNextWeek(GameState state)
        {
            var today = DateUtil.ParseDateOrDefault(state.Save.CurrentDate, new DateTime(2026, 1, 1));
            DateTime next = today.DayOfWeek != DayOfWeek.Saturday ? CalendarPlanner.NextSaturday(today) : today.AddDays(7);
            state.Save.CurrentDate = next.ToString("yyyy-MM-dd");
            return next;
        }

        private static Fighter FindFighter(GameState state, string division, int fid)
        {
            var list = state.FightersByDivision.ContainsKey(division) ? state.FightersByDivision[division] : new List<Fighter>();
            var fighter = list.FirstOrDefault(f => f.Id == fid);
            if (fighter == null)
            {
                throw new Exception($"Missing fighter {fid} in {division}");
            }
            return fighter;
        }

        private static string FighterName(GameState state, string division, int fid)
        {
            try
            {
                return FindFighter(state, division, fid).Name;
            }
            catch
            {
                return $"#{fid}";
            }
        }

        private static Fighter CurrentChamp(GameState state, string division)
        {
            return state.FightersByDivision.ContainsKey(division)
                ? state.FightersByDivision[division].FirstOrDefault(f => f.IsChamp == 1)
                : null;
        }

        private static List<Fighter> Top15Only(GameState state, string division)
        {
            return state.FightersByDivision[division]
                .Where(f => f.IsChamp != 1 && !string.IsNullOrWhiteSpace(f.RankSlot))
                .ToList();
        }

        private static List<Fighter> UnrankedOnly(GameState state, string division)
        {
            return state.FightersByDivision[division]
                .Where(f => f.IsChamp != 1 && string.IsNullOrWhiteSpace(f.RankSlot))
                .ToList();
        }

        private static bool Available(Fighter fighter, DateTime when)
        {
            if (fighter.IsActive != 1)
            {
                return false;
            }
            var nad = DateUtil.ParseDate(fighter.NextAvailableDate);
            if (!nad.HasValue)
            {
                var last = DateUtil.ParseDate(fighter.LastFightDate);
                if (last.HasValue && (when - last.Value).Days < 49)
                {
                    return false;
                }
            }
            return !nad.HasValue || when >= nad.Value;
        }

        private static bool IsTopTierFighter(Fighter fighter)
        {
            if (fighter.IsChamp == 1)
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(fighter.RankSlot))
            {
                return false;
            }
            return int.TryParse(fighter.RankSlot, out var slot) && slot <= 8;
        }

        private static int EventTopTierBoutCount(GameState state, int eventId)
        {
            int count = 0;
            foreach (var fight in state.Fights)
            {
                if (fight.EventId != eventId || fight.Status == "cancelled")
                {
                    continue;
                }
                string div = fight.Division;
                var a = FindFighter(state, div, fight.AId);
                var b = FindFighter(state, div, fight.BId);
                if (IsTopTierFighter(a) && IsTopTierFighter(b))
                {
                    count++;
                }
            }
            return count;
        }

        private static bool FightIsRanked(GameState state, string division, int aId, int bId)
        {
            var a = FindFighter(state, division, aId);
            var b = FindFighter(state, division, bId);
            return !string.IsNullOrWhiteSpace(a.RankSlot) && !string.IsNullOrWhiteSpace(b.RankSlot);
        }

        private static Dictionary<(int, int), DateTime> GetPairLastMap(List<Dictionary<string, string>> pairs)
        {
            var map = new Dictionary<(int, int), DateTime>();
            foreach (var row in pairs)
            {
                if (!row.TryGetValue("a_id", out var aText) || !row.TryGetValue("b_id", out var bText))
                {
                    continue;
                }
                int a = int.Parse(aText);
                int b = int.Parse(bText);
                if (row.TryGetValue("last_fight_date", out var dateText))
                {
                    var dt = DateUtil.ParseDate(dateText);
                    if (dt.HasValue)
                    {
                        map[Matchmaking.Matchmaking.PairKey(a, b)] = dt.Value;
                    }
                }
            }
            return map;
        }

        private static void SetPairLast(List<Dictionary<string, string>> pairs, int aId, int bId, DateTime dt)
        {
            var key = Matchmaking.Matchmaking.PairKey(aId, bId);
            foreach (var row in pairs)
            {
                int ra = int.Parse(row["a_id"]);
                int rb = int.Parse(row["b_id"]);
                if (Matchmaking.Matchmaking.PairKey(ra, rb) == key)
                {
                    row["last_fight_date"] = dt.ToString("yyyy-MM-dd");
                    return;
                }
            }
            pairs.Add(new Dictionary<string, string>
            {
                {"a_id", key.Item1.ToString()},
                {"b_id", key.Item2.ToString()},
                {"last_fight_date", dt.ToString("yyyy-MM-dd")}
            });
        }

        private static void BookedIdsAndPairs(GameState state, string division, int currentEventId, out HashSet<int> bookedIds, out HashSet<(int, int)> bookedPairs)
        {
            bookedIds = new HashSet<int>();
            bookedPairs = new HashSet<(int, int)>();
            foreach (var fight in state.Fights)
            {
                if (fight.Status != "scheduled" || fight.Division != division)
                {
                    continue;
                }
                if (fight.EventId == currentEventId)
                {
                    continue;
                }
                bookedIds.Add(fight.AId);
                bookedIds.Add(fight.BId);
                bookedPairs.Add(Matchmaking.Matchmaking.PairKey(fight.AId, fight.BId));
            }
        }

        private static HashSet<int> EventUsedIds(GameState state, int eventId, string division)
        {
            var used = new HashSet<int>();
            foreach (var fight in state.Fights)
            {
                if (fight.EventId == eventId && fight.Division == division)
                {
                    used.Add(fight.AId);
                    used.Add(fight.BId);
                }
            }
            return used;
        }

        private static void CancelSelfFights(List<EventRow> events, List<FightRow> fights)
        {
            var eventsById = events.ToDictionary(e => e.EventId, e => e);
            foreach (var fight in fights)
            {
                if (fight.AId == 0 || fight.BId == 0 || fight.AId != fight.BId)
                {
                    continue;
                }
                if (fight.Status == "cancelled")
                {
                    continue;
                }
                fight.Status = "cancelled";
                if (!eventsById.TryGetValue(fight.EventId, out var ev))
                {
                    continue;
                }
                var notes = JsonList(ev.NotesJson);
                notes.Add("⚠️ Бой отменён: один и тот же боец был записан в обе стороны.");
                ev.NotesJson = DumpJsonList(notes);
            }
        }

        private int PlanMainEvent(GameState state, DateTime today, DateTime eventDate, EventRow eventRow, int nextFightId)
        {
            string kind = string.IsNullOrWhiteSpace(eventRow.EventKind) ? "FIGHT_NIGHT" : eventRow.EventKind;
            var divisions = state.FightersByDivision.Keys.ToList();
            if (divisions.Count == 0)
            {
                return nextFightId;
            }

            double titleChance = kind == "NUMBERED" ? 0.38 : 0.18;
            if (kind == "COUNTRY")
            {
                titleChance = 0.25;
            }

            string mainDiv = divisions[_rng.Next(divisions.Count)];
            var notes = JsonList(eventRow.NotesJson);
            notes.Add($"📍 {EventDisplayName(kind, eventRow.EventId, eventRow.Location, eventRow.ThemeCountry)}");
            eventRow.NotesJson = DumpJsonList(notes);

            nextFightId = PlanFeaturedBout(state, eventDate, eventRow, nextFightId, mainDiv, titleChance, "MAIN_EVENT", true, "📣 Анонс мейн-ивента");

            if (kind == "NUMBERED")
            {
                var remaining = divisions.Where(d => d != mainDiv).ToList();
                string coDiv = remaining.Count > 0 ? remaining[_rng.Next(remaining.Count)] : mainDiv;
                nextFightId = PlanFeaturedBout(state, eventDate, eventRow, nextFightId, coDiv, 0.22, "CO_MAIN", false, "📣 Анонс ко-мейн ивента");
            }

            return nextFightId;
        }

        private int PlanFeaturedBout(GameState state, DateTime eventDate, EventRow eventRow, int nextFightId, string division, double titleChance, string cardSlot, bool isMainEvent, string noteLabel)
        {
            if (string.IsNullOrWhiteSpace(division))
            {
                return nextFightId;
            }

            int currentEventId = eventRow.EventId;
            BookedIdsAndPairs(state, division, currentEventId, out var bookedIds, out var bookedPairs);
            var usedInEvent = EventUsedIds(state, currentEventId, division);
            foreach (var id in usedInEvent)
            {
                bookedIds.Add(id);
            }

            var champ = CurrentChamp(state, division);
            if (champ != null && Available(champ, eventDate) && _rng.NextDouble() < titleChance && !AlreadyScheduledTitle(state, division, champ.Id))
            {
                var lastTitle = DateUtil.ParseDate(state.Save.LastTitleFightDate);
                bool due = !lastTitle.HasValue || (eventDate - lastTitle.Value).Days >= 56;
                if (due)
                {
                    var ranked = Top15Only(state, division).Where(f => Available(f, eventDate) && !bookedIds.Contains(f.Id)).OrderBy(f => int.Parse(f.RankSlot)).ToList();
                    var pool = ranked.Take(5).ToList();
                    var poolPref = ranked.Take(3).ToList();
                    var used = new HashSet<int> { champ.Id };
                    var pairMap = GetPairLastMap(state.PairsByDivision[division]);
                    var challenger = Matchmaking.Matchmaking.PickBestOpponent(champ, poolPref, used, pairMap, eventDate, true)
                                    ?? Matchmaking.Matchmaking.PickBestOpponent(champ, pool, used, pairMap, eventDate, true);
                    if (challenger != null)
                    {
                        var fight = new FightRow
                        {
                            FightId = nextFightId,
                            EventId = eventRow.EventId,
                            Division = division,
                            AId = champ.Id,
                            BId = challenger.Id,
                            IsTop15 = 1,
                            IsMainEvent = isMainEvent ? 1 : 0,
                            IsTitleFight = 1,
                            CardSlot = cardSlot,
                            Status = "scheduled",
                            WinnerId = string.Empty,
                            Method = string.Empty,
                            Round = string.Empty,
                            TimeMmss = string.Empty
                        };
                        if (AppendFight(state, fight, eventRow))
                        {
                            if (isMainEvent)
                            {
                                eventRow.MainFightId = nextFightId.ToString();
                            }
                            var notes = JsonList(eventRow.NotesJson);
                            notes.Add($"🏆 Анонс титульного боя ({division}): {FighterName(state, division, champ.Id)} vs {FighterName(state, division, challenger.Id)}");
                            eventRow.NotesJson = DumpJsonList(notes);
                            return nextFightId + 1;
                        }
                        return nextFightId;
                    }
                }
            }

            var rankedPool = Top15Only(state, division).Where(f => Available(f, eventDate) && !bookedIds.Contains(f.Id)).OrderBy(f => int.Parse(f.RankSlot)).Take(8).ToList();
            var pairLastMap = GetPairLastMap(state.PairsByDivision[division]);
            (Fighter, Fighter)? bestPair = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < rankedPool.Count; i++)
            {
                for (int j = i + 1; j < rankedPool.Count; j++)
                {
                    var a = rankedPool[i];
                    var b = rankedPool[j];
                    var pk = Matchmaking.Matchmaking.PairKey(a.Id, b.Id);
                    if (bookedPairs.Contains(pk))
                    {
                        continue;
                    }
                    pairLastMap.TryGetValue(pk, out var last);
                    if (last != default && (eventDate - last).Days < 210)
                    {
                        continue;
                    }
                    float score = 1000f - Math.Abs(a.Rating - b.Rating);
                    score += 120f - 20f * Math.Abs(int.Parse(a.RankSlot) - int.Parse(b.RankSlot));
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPair = (a, b);
                    }
                }
            }
            if (!bestPair.HasValue && rankedPool.Count >= 2)
            {
                bestPair = (rankedPool[0], rankedPool[1]);
            }
            if (!bestPair.HasValue)
            {
                return nextFightId;
            }

            var fightRow = new FightRow
            {
                FightId = nextFightId,
                EventId = eventRow.EventId,
                Division = division,
                AId = bestPair.Value.Item1.Id,
                BId = bestPair.Value.Item2.Id,
                IsTop15 = 1,
                IsMainEvent = isMainEvent ? 1 : 0,
                IsTitleFight = 0,
                CardSlot = cardSlot,
                Status = "scheduled",
                WinnerId = string.Empty,
                Method = string.Empty,
                Round = string.Empty,
                TimeMmss = string.Empty
            };
            if (AppendFight(state, fightRow, eventRow))
            {
                if (isMainEvent)
                {
                    eventRow.MainFightId = nextFightId.ToString();
                }
                var notes = JsonList(eventRow.NotesJson);
                notes.Add($"{noteLabel} ({division}): {FighterName(state, division, fightRow.AId)} vs {FighterName(state, division, fightRow.BId)}");
                eventRow.NotesJson = DumpJsonList(notes);
                return nextFightId + 1;
            }

            return nextFightId;
        }

        private bool AlreadyScheduledTitle(GameState state, string division, int champId)
        {
            return state.Fights.Any(f => f.Status == "scheduled" && f.Division == division && f.IsTitleFight == 1 && (f.AId == champId || f.BId == champId));
        }

        private bool AppendFight(GameState state, FightRow fight, EventRow eventRow = null)
        {
            if (fight.AId == 0 || fight.BId == 0)
            {
                return false;
            }
            if (fight.AId == fight.BId)
            {
                if (eventRow != null)
                {
                    var notes = JsonList(eventRow.NotesJson);
                    notes.Add("⚠️ Отменён бой из-за совпадения бойцов в паре.");
                    eventRow.NotesJson = DumpJsonList(notes);
                }
                return false;
            }
            state.Fights.Add(fight);
            return true;
        }

        private int FillFullCard(GameState state, DateTime today, DateTime eventDate, EventRow eventRow, int nextFightId)
        {
            int eventId = eventRow.EventId;
            string kind = string.IsNullOrWhiteSpace(eventRow.EventKind) ? "FIGHT_NIGHT" : eventRow.EventKind;
            string themeCountry = eventRow.ThemeCountry ?? string.Empty;
            var divisions = state.FightersByDivision.Keys.ToList();
            if (divisions.Count == 0)
            {
                return nextFightId;
            }

            var (mainCardCount, prelimsCount) = PickCardPlan(kind);
            var existingCounts = new Dictionary<string, int>();
            var existingTopByDiv = new Dictionary<string, int>();
            var existingUnrByDiv = new Dictionary<string, int>();
            foreach (var fight in state.Fights)
            {
                if (fight.EventId != eventId)
                {
                    continue;
                }
                string div = fight.Division;
                if (fight.CardSlot == "MAIN_EVENT" || fight.CardSlot == "CO_MAIN")
                {
                    continue;
                }
                existingCounts[div] = existingCounts.ContainsKey(div) ? existingCounts[div] + 1 : 1;
                if (FightIsRanked(state, div, fight.AId, fight.BId))
                {
                    existingTopByDiv[div] = existingTopByDiv.ContainsKey(div) ? existingTopByDiv[div] + 1 : 1;
                }
                else
                {
                    existingUnrByDiv[div] = existingUnrByDiv.ContainsKey(div) ? existingUnrByDiv[div] + 1 : 1;
                }
            }
            int totalTarget = mainCardCount + prelimsCount;
            var targets = DivisionTargets(divisions, existingCounts, totalTarget);
            int totalTopDesired = Math.Max(mainCardCount, existingTopByDiv.Values.Sum());
            int remainingTop = Math.Max(0, totalTopDesired - existingTopByDiv.Values.Sum());
            var topTargets = divisions.ToDictionary(d => d, d => existingTopByDiv.ContainsKey(d) ? existingTopByDiv[d] : 0);
            while (remainingTop > 0)
            {
                var candidates = divisions.Where(d => topTargets[d] < targets[d]).ToList();
                if (candidates.Count == 0)
                {
                    break;
                }
                string div = candidates[_rng.Next(candidates.Count)];
                topTargets[div] += 1;
                remainingTop -= 1;
            }
            double topProb = TopFightProbability(kind);
            int topTierLimit = 4;
            int topTierCount = EventTopTierBoutCount(state, eventId);

            foreach (var div in divisions)
            {
                BookedIdsAndPairs(state, div, eventId, out var bookedIds, out var bookedPairs);
                var used = new HashSet<int>(bookedIds);
                var existingAll = state.Fights.Where(f => f.EventId == eventId && f.Division == div).ToList();
                foreach (var f in existingAll)
                {
                    used.Add(f.AId);
                    used.Add(f.BId);
                }
                var eventPairs = new HashSet<(int, int)>(existingAll.Select(f => Matchmaking.Matchmaking.PairKey(f.AId, f.BId)));
                var existing = existingAll.Where(f => f.CardSlot != "MAIN_EVENT" && f.CardSlot != "CO_MAIN").ToList();

                int topExisting = existing.Count(f => f.IsTop15 == 1 && f.Status == "scheduled");
                int unrExisting = existing.Count(f => f.IsTop15 == 0 && f.Status == "scheduled");

                int targetTotal = targets.ContainsKey(div) ? targets[div] : topExisting + unrExisting;
                int topTarget = Math.Min(topTargets[div], targetTotal);
                int unrTarget = Math.Max(0, targetTotal - topTarget);
                int topNeeded = Math.Max(0, topTarget - topExisting);
                int unrNeeded = Math.Max(0, unrTarget - unrExisting);

                var pairLastMap = GetPairLastMap(state.PairsByDivision[div]);
                var topPool = Top15Only(state, div).Where(f => Available(f, eventDate) && !used.Contains(f.Id)).ToList();
                var unrPool = UnrankedOnly(state, div).Where(f => Available(f, eventDate) && !used.Contains(f.Id)).ToList();

                int ThemeBoost(Fighter f) => !string.IsNullOrWhiteSpace(themeCountry) && f.Country == themeCountry ? 1 : 0;

                topPool = topPool.OrderByDescending(f => ThemeBoost(f)).ThenByDescending(f => f.Rating).ToList();
                unrPool = unrPool.OrderByDescending(f => ThemeBoost(f)).ThenByDescending(f => f.Rating).ToList();

                bool allowSpecial = _rng.NextDouble() < 0.06;

                while (topNeeded > 0 && topPool.Count >= 2)
                {
                    var a = topPool[0];
                    topPool.RemoveAt(0);
                    int aRank = int.Parse(a.RankSlot);
                    var candidates = topPool.Where(b => Math.Abs(aRank - int.Parse(b.RankSlot)) <= 6).ToList();
                    candidates = candidates.Where(b => !bookedPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id))).ToList();
                    candidates = candidates.Where(b => !eventPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id))).ToList();
                    if (allowSpecial && a.Streak >= 4)
                    {
                        candidates = topPool.Where(b => !bookedPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id))).ToList();
                        candidates = candidates.Where(b => !eventPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id))).ToList();
                    }
                    var poolFb = (candidates.Count > 0 ? candidates : topPool)
                        .Where(b => !bookedPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id)))
                        .Where(b => !eventPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id)))
                        .ToList();
                    if (topTierCount >= topTierLimit)
                    {
                        poolFb = poolFb.Where(b => !(IsTopTierFighter(a) && IsTopTierFighter(b))).ToList();
                    }
                    var bFighter = Matchmaking.Matchmaking.PickBestOpponent(a, poolFb, used, pairLastMap, eventDate, false);
                    if (bFighter == null)
                    {
                        continue;
                    }
                    topPool = topPool.Where(x => x.Id != bFighter.Id).ToList();
                    used.Add(a.Id);
                    used.Add(bFighter.Id);
                    eventPairs.Add(Matchmaking.Matchmaking.PairKey(a.Id, bFighter.Id));
                    var fight = new FightRow
                    {
                        FightId = nextFightId,
                        EventId = eventId,
                        Division = div,
                        AId = a.Id,
                        BId = bFighter.Id,
                        IsTop15 = 1,
                        IsMainEvent = 0,
                        IsTitleFight = 0,
                        CardSlot = string.Empty,
                        Status = "scheduled",
                        WinnerId = string.Empty,
                        Method = string.Empty,
                        Round = string.Empty,
                        TimeMmss = string.Empty
                    };
                    if (AppendFight(state, fight, eventRow))
                    {
                        nextFightId++;
                        topNeeded--;
                        if (IsTopTierFighter(a) && IsTopTierFighter(bFighter))
                        {
                            topTierCount++;
                        }
                    }
                }

                while (unrNeeded > 0 && unrPool.Count >= 2)
                {
                    var a = unrPool[0];
                    unrPool.RemoveAt(0);
                    var candidates = unrPool.Where(b => Math.Abs(a.Rating - b.Rating) <= 120f).ToList();
                    candidates = candidates.Where(b => !bookedPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id))).ToList();
                    candidates = candidates.Where(b => !eventPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id))).ToList();
                    var poolFb = (candidates.Count > 0 ? candidates : unrPool)
                        .Where(b => !bookedPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id)))
                        .Where(b => !eventPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id)))
                        .ToList();
                    var bFighter = Matchmaking.Matchmaking.PickBestOpponent(a, poolFb, used, pairLastMap, eventDate, false);
                    if (bFighter == null)
                    {
                        break;
                    }
                    unrPool = unrPool.Where(x => x.Id != bFighter.Id).ToList();
                    used.Add(a.Id);
                    used.Add(bFighter.Id);
                    eventPairs.Add(Matchmaking.Matchmaking.PairKey(a.Id, bFighter.Id));
                    var fight = new FightRow
                    {
                        FightId = nextFightId,
                        EventId = eventId,
                        Division = div,
                        AId = a.Id,
                        BId = bFighter.Id,
                        IsTop15 = 0,
                        IsMainEvent = 0,
                        IsTitleFight = 0,
                        CardSlot = string.Empty,
                        Status = "scheduled",
                        WinnerId = string.Empty,
                        Method = string.Empty,
                        Round = string.Empty,
                        TimeMmss = string.Empty
                    };
                    if (AppendFight(state, fight, eventRow))
                    {
                        nextFightId++;
                        unrNeeded--;
                    }
                }

                int currentTotal = state.Fights.Count(f => f.EventId == eventId && f.Division == div);
                while (currentTotal < targetTotal)
                {
                    bool useTop = _rng.NextDouble() < topProb && topPool.Count >= 2;
                    var pool = useTop ? topPool : unrPool;
                    if (pool.Count < 2)
                    {
                        pool = useTop ? unrPool : topPool;
                        useTop = pool == topPool;
                    }
                    if (pool.Count < 2)
                    {
                        break;
                    }
                    var a = pool[0];
                    pool.RemoveAt(0);
                    List<Fighter> candidates;
                    if (useTop)
                    {
                        int aRank = int.Parse(a.RankSlot);
                        candidates = pool.Where(b => Math.Abs(aRank - int.Parse(b.RankSlot)) <= 6).ToList();
                    }
                    else
                    {
                        candidates = pool.Where(b => Math.Abs(a.Rating - b.Rating) <= 120f).ToList();
                    }
                    candidates = candidates.Where(b => !bookedPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id))).ToList();
                    candidates = candidates.Where(b => !eventPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id))).ToList();
                    var poolFb = (candidates.Count > 0 ? candidates : pool)
                        .Where(b => !bookedPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id)))
                        .Where(b => !eventPairs.Contains(Matchmaking.Matchmaking.PairKey(a.Id, b.Id)))
                        .ToList();
                    if (useTop && topTierCount >= topTierLimit)
                    {
                        poolFb = poolFb.Where(b => !(IsTopTierFighter(a) && IsTopTierFighter(b))).ToList();
                    }
                    var bFighter = Matchmaking.Matchmaking.PickBestOpponent(a, poolFb, used, pairLastMap, eventDate, false);
                    if (bFighter == null)
                    {
                        break;
                    }
                    pool = pool.Where(x => x.Id != bFighter.Id).ToList();
                    if (useTop)
                    {
                        topPool = pool;
                    }
                    else
                    {
                        unrPool = pool;
                    }
                    used.Add(a.Id);
                    used.Add(bFighter.Id);
                    eventPairs.Add(Matchmaking.Matchmaking.PairKey(a.Id, bFighter.Id));
                    var fight = new FightRow
                    {
                        FightId = nextFightId,
                        EventId = eventId,
                        Division = div,
                        AId = a.Id,
                        BId = bFighter.Id,
                        IsTop15 = useTop ? 1 : 0,
                        IsMainEvent = 0,
                        IsTitleFight = 0,
                        CardSlot = string.Empty,
                        Status = "scheduled",
                        WinnerId = string.Empty,
                        Method = string.Empty,
                        Round = string.Empty,
                        TimeMmss = string.Empty
                    };
                    if (AppendFight(state, fight, eventRow))
                    {
                        nextFightId++;
                        currentTotal++;
                        if (useTop && IsTopTierFighter(a) && IsTopTierFighter(bFighter))
                        {
                            topTierCount++;
                        }
                    }
                }
            }

            AssignCardSlots(state, eventId, mainCardCount, prelimsCount);
            var notes = JsonList(eventRow.NotesJson);
            notes.Add("📌 Полный кард сформирован.");
            eventRow.NotesJson = DumpJsonList(notes);
            return nextFightId;
        }

        private void AssignCardSlots(GameState state, int eventId, int mainCardCount, int prelimCount)
        {
            var rankedCandidates = new List<(float, FightRow)>();
            var unrankedCandidates = new List<(float, FightRow)>();
            foreach (var fight in state.Fights)
            {
                if (fight.EventId != eventId)
                {
                    continue;
                }
                if (fight.CardSlot == "MAIN_EVENT" || fight.CardSlot == "CO_MAIN")
                {
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(fight.CardSlot))
                {
                    continue;
                }
                float avgRating = AvgRating(state, fight.Division, fight.AId, fight.BId);
                if (FightIsRanked(state, fight.Division, fight.AId, fight.BId))
                {
                    rankedCandidates.Add((avgRating, fight));
                }
                else
                {
                    unrankedCandidates.Add((avgRating, fight));
                }
            }

            rankedCandidates = rankedCandidates.OrderByDescending(x => x.Item1).ToList();
            unrankedCandidates = unrankedCandidates.OrderByDescending(x => x.Item1).ToList();

            int mainSlots = Math.Min(mainCardCount, rankedCandidates.Count);
            for (int i = 0; i < mainSlots; i++)
            {
                rankedCandidates[i].Item2.CardSlot = "MAIN_CARD";
            }

            foreach (var item in unrankedCandidates)
            {
                item.Item2.CardSlot = "PRELIMS";
            }

            int remainingPrelims = Math.Max(0, prelimCount - unrankedCandidates.Count);
            if (remainingPrelims > 0)
            {
                for (int i = mainSlots; i < rankedCandidates.Count && remainingPrelims > 0; i++)
                {
                    rankedCandidates[i].Item2.CardSlot = "PRELIMS";
                    remainingPrelims--;
                }
            }

            foreach (var fight in state.Fights)
            {
                if (fight.EventId != eventId)
                {
                    continue;
                }
                if (fight.CardSlot == "MAIN_EVENT" || fight.CardSlot == "CO_MAIN")
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(fight.CardSlot))
                {
                    fight.CardSlot = "PRELIMS";
                }
            }
        }

        private float AvgRating(GameState state, string division, int aId, int bId)
        {
            var a = FindFighter(state, division, aId);
            var b = FindFighter(state, division, bId);
            return (a.Rating + b.Rating) / 2f;
        }

        private void ProcessWithdrawals(GameState state, EventRow eventRow, DateTime eventDate, SimConfig cfg)
        {
            if (!string.IsNullOrWhiteSpace(eventRow.GeneratedOn) || !string.IsNullOrWhiteSpace(eventRow.AnnouncedFullOn))
            {
                return;
            }

            int eventId = eventRow.EventId;
            var notes = JsonList(eventRow.NotesJson);
            var divisions = state.FightersByDivision.Keys.ToList();

            foreach (var div in divisions)
            {
                var card = state.Fights.Where(f => f.EventId == eventId && f.Division == div && f.Status == "scheduled").ToList();
                var used = new HashSet<int>();
                foreach (var fight in card)
                {
                    used.Add(fight.AId);
                    used.Add(fight.BId);
                }

                var pairLastMap = GetPairLastMap(state.PairsByDivision[div]);
                var topPool = Top15Only(state, div).Where(f => Available(f, eventDate) && !used.Contains(f.Id)).ToList();
                var unrPool = UnrankedOnly(state, div).Where(f => Available(f, eventDate) && !used.Contains(f.Id)).ToList();

                foreach (var fight in card)
                {
                    if (_rng.NextDouble() >= cfg.WithdrawalChance)
                    {
                        continue;
                    }

                    int outId = _rng.NextDouble() < 0.5 ? fight.AId : fight.BId;
                    int stayId = outId == fight.AId ? fight.BId : fight.AId;
                    string outName = FighterName(state, div, outId);
                    notes.Add(NewsService.WithdrawalMsg(outName));

                    bool isTop = fight.IsTop15 == 1;
                    var pool = isTop ? topPool : unrPool;
                    var stay = FindFighter(state, div, stayId);
                    var cand = pool.Where(p => Math.Abs(p.Rating - stay.Rating) <= (isTop ? 180f : 120f)).ToList();
                    var rep = Matchmaking.Matchmaking.PickBestOpponent(stay, cand.Count > 0 ? cand : pool, used, pairLastMap, eventDate, false);
                    if (rep == null)
                    {
                        fight.Status = "cancelled";
                        notes.Add(NewsService.CancelledMsg(FighterName(state, div, fight.AId), FighterName(state, div, fight.BId)));
                        continue;
                    }
                    int repId = rep.Id;
                    notes.Add(NewsService.ReplacementMsg(outName, FighterName(state, div, repId)));
                    if (outId == fight.AId)
                    {
                        fight.AId = repId;
                    }
                    else
                    {
                        fight.BId = repId;
                    }
                    used.Add(repId);
                    if (isTop)
                    {
                        topPool = topPool.Where(x => x.Id != repId).ToList();
                    }
                    else
                    {
                        unrPool = unrPool.Where(x => x.Id != repId).ToList();
                    }
                }
            }

            eventRow.NotesJson = DumpJsonList(notes);
        }

        private string PickEventKind()
        {
            double r = _rng.NextDouble();
            if (r < 0.55)
            {
                return "FIGHT_NIGHT";
            }
            if (r < 0.85)
            {
                return "NUMBERED";
            }
            return "COUNTRY";
        }

        private (string, string) PickLocationAndTheme(Dictionary<string, List<Fighter>> fightersByDiv, string kind)
        {
            string[] cities = { "Las Vegas", "New York", "London", "Paris", "Abu Dhabi", "Singapore", "Tokyo", "Sydney", "Toronto", "Mexico City" };
            if (kind == "COUNTRY")
            {
                var countries = new List<string>();
                foreach (var list in fightersByDiv.Values)
                {
                    foreach (var fighter in list)
                    {
                        if (!string.IsNullOrWhiteSpace(fighter.Country))
                        {
                            countries.Add(fighter.Country);
                        }
                    }
                }
                string theme = countries.Count > 0 ? countries[_rng.Next(countries.Count)] : string.Empty;
                string city = cities[_rng.Next(cities.Length)];
                return (city, theme);
            }
            string[] small = { "Las Vegas", "Apex", "New York", "London", "Paris" };
            return (small[_rng.Next(small.Length)], string.Empty);
        }

        private static string EventDisplayName(string kind, int eventId, string location, string themeCountry)
        {
            string loc = location?.Trim() ?? string.Empty;
            string theme = themeCountry?.Trim() ?? string.Empty;
            if (kind == "NUMBERED")
            {
                int number = 300 + eventId;
                string baseName = $"UFC {number}";
                return string.IsNullOrWhiteSpace(loc) ? baseName : $"{baseName}: {loc}";
            }
            if (kind == "COUNTRY")
            {
                string baseName = "UFC Fight Night";
                if (!string.IsNullOrWhiteSpace(loc))
                {
                    baseName = $"{baseName}: {loc}";
                }
                if (!string.IsNullOrWhiteSpace(theme))
                {
                    baseName = $"{baseName} ({theme} special)";
                }
                return baseName;
            }
            string name = "UFC Fight Night";
            return string.IsNullOrWhiteSpace(loc) ? name : $"{name}: {loc}";
        }

        private (int, int) PickCardPlan(string kind)
        {
            bool hasCoMain = kind == "NUMBERED";
            var options = new List<(int, int)>();
            for (int main = 6; main < 8; main++)
            {
                for (int prelim = 8; prelim < 13; prelim++)
                {
                    int total = main + prelim + (hasCoMain ? 2 : 1);
                    if (total >= 16 && total <= 20)
                    {
                        options.Add((main, prelim));
                    }
                }
            }
            return options.Count > 0 ? options[_rng.Next(options.Count)] : (6, 9);
        }

        private static double TopFightProbability(string kind)
        {
            if (kind == "NUMBERED")
            {
                return 0.65;
            }
            if (kind == "COUNTRY")
            {
                return 0.55;
            }
            return 0.45;
        }

        private Dictionary<string, int> DivisionTargets(List<string> divisions, Dictionary<string, int> existingCounts, int totalFights)
        {
            int maxPerDiv = divisions.Count < 3 ? totalFights : Math.Min(6, Math.Max(4, (int)(totalFights * 0.4)));
            int minDivisions = Math.Min(3, divisions.Count);
            var seeds = divisions.OrderBy(_ => _rng.Next()).Take(minDivisions).ToList();
            var targets = divisions.ToDictionary(d => d, d => existingCounts.ContainsKey(d) ? existingCounts[d] : 0);
            int remaining = totalFights - targets.Values.Sum();

            foreach (var div in seeds)
            {
                int need = Math.Max(0, 2 - targets[div]);
                int add = Math.Min(need, remaining);
                targets[div] += add;
                remaining -= add;
            }

            while (remaining > 0)
            {
                var candidates = divisions.Where(d => targets[d] < maxPerDiv).ToList();
                if (candidates.Count == 0)
                {
                    break;
                }
                string div = candidates[_rng.Next(candidates.Count)];
                targets[div] += 1;
                remaining -= 1;
            }
            return targets;
        }

        private static List<string> JsonList(string json)
        {
            return DeserializeList<string>(json);
        }

        private static string DumpJsonList(List<string> list)
        {
            return SerializeList(list);
        }

        private static void AppendRatingHistory(Fighter fighter, DateTime date)
        {
            var history = DeserializeRatingHistory(fighter.RatingHistory);
            history.Add(new RatingHistoryEntry { d = date.ToString("yyyy-MM-dd"), r = fighter.Rating.ToString("F2", CultureInfo.InvariantCulture) });
            fighter.RatingHistory = SerializeHistory(TrimHistory(history, 60));
        }

        private static void AppendRankHistory(Fighter fighter, DateTime date)
        {
            var history = DeserializeRankHistory(fighter.RankHistory);
            string rank = string.IsNullOrWhiteSpace(fighter.RankSlot) ? string.Empty : fighter.RankSlot;
            if (fighter.IsChamp == 1)
            {
                rank = "0";
            }
            history.Add(new RankHistoryEntry { d = date.ToString("yyyy-MM-dd"), rank = rank });
            fighter.RankHistory = SerializeHistory(TrimHistory(history, 60));
        }

        private static List<RatingHistoryEntry> DeserializeRatingHistory(string json)
        {
            return DeserializeList<RatingHistoryEntry>(json);
        }

        private static List<RankHistoryEntry> DeserializeRankHistory(string json)
        {
            return DeserializeList<RankHistoryEntry>(json);
        }

        private static string SerializeHistory<T>(List<T> history)
        {
            return SerializeList(history);
        }

        private static List<T> TrimHistory<T>(List<T> history, int max)
        {
            if (history == null)
            {
                return new List<T>();
            }
            if (history.Count <= max)
            {
                return history;
            }
            return history.GetRange(history.Count - max, max);
        }

        private static List<T> DeserializeList<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }
            try
            {
                var wrapper = JsonUtility.FromJson<SerializableList<T>>(json);
                return wrapper?.items ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        private static string SerializeList<T>(List<T> items)
        {
            var wrapper = new SerializableList<T> { items = items ?? new List<T>() };
            return JsonUtility.ToJson(wrapper);
        }

        [Serializable]
        private class SerializableList<T>
        {
            public List<T> items = new List<T>();
        }

        [Serializable]
        private class RatingHistoryEntry
        {
            public string d;
            public string r;
        }

        [Serializable]
        private class RankHistoryEntry
        {
            public string d;
            public string rank;
        }
    }
}
