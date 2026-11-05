"""Issue #244: reproducible economy experiment using saved classifier scores.

Run: python docs/balance-sim/issue_244_balance.py
No models, services, credentials, packages, or invented skill-quality drift.
The baseline reproduces the current post calculator; candidate modes replace
random flops with the four proposed content causes. This is a simulation, not
a backend implementation or a measured forecast of player behaviour.
"""

import argparse
import hashlib
import json
import math
import random
import re
import statistics
from pathlib import Path


FOLDER = Path(__file__).resolve().parent
CAUSES = ("angry_abuse", "near_copy", "sad_tone", "anxious_tone")
DIFFICULTIES = ("easy", "normal", "hard")
DRAMAS = ("off", "soft", "mid", "hot")
MILESTONES = (1000, 5000, 10000, 100000, 1000000)
THRESHOLDS = {
    "easy": dict(angry_abuse=.9, near_copy_cosine=.98, near_copy_nli=.95,
                 sad_tone=.98, anxious_tone=.96),
    "normal": dict(angry_abuse=.8, near_copy_cosine=.96, near_copy_nli=.9,
                   sad_tone=.96, anxious_tone=.94),
    "hard": dict(angry_abuse=.7, near_copy_cosine=.94, near_copy_nli=.85,
                 sad_tone=.94, anxious_tone=.92),
}
LOSS_VARIANTS = {
    "mild": dict(zip(DIFFICULTIES, (.05, .08, .10))),
    "selected": dict(zip(DIFFICULTIES, (.08, .12, .16))),
    "strong": dict(zip(DIFFICULTIES, (.12, .18, .24))),
}
LOSS_CAPS = dict(zip(DIFFICULTIES, (.005, .01, .015)))
DRAMA_SETTINGS = {
    "off": dict(threshold_steps=0, loss_multiplier=.5),
    "soft": dict(threshold_steps=1, loss_multiplier=.75),
    "mid": dict(threshold_steps=2, loss_multiplier=1),
    "hot": dict(threshold_steps=3, loss_multiplier=1.25),
}
THRESHOLD_REDUCTION = dict(angry_abuse=.05, near_copy_cosine=.01, near_copy_nli=.015,
                           sad_tone=.01, anxious_tone=.01)
PERSONAS = {
    "casual": dict(posts=3, every=2, quality=.4, own=.3, preferred="easy"),
    "average": dict(posts=10, every=1, quality=.55, own=.5, preferred="normal"),
    "active": dict(posts=10, every=1, quality=.75, own=.9, preferred="normal"),
    "plus": dict(posts=20, every=1, quality=.65, own=.7, preferred="normal"),
    "weak": dict(posts=10, every=1, quality=.3, own=.1, preferred="normal"),
    "it": dict(posts=10, every=1, quality=.55, own=.5, preferred="hard"),
    "returner": dict(posts=3, every=3, quality=.55, own=.5, preferred="normal"),
}
BALANCED = (.1, .2, .35, .35)
SCENARIOS = {
    "zero_scores_control": dict(rate=0, weights=BALANCED, all_scores_zero=True),
    "neutral_0": dict(rate=0, weights=BALANCED),
    "balanced_10": dict(rate=.1, weights=BALANCED),
    "balanced_40": dict(rate=.4, weights=BALANCED),
    "balanced_60": dict(rate=.6, weights=BALANCED),
    "emotional_40": dict(rate=.4, weights=(0, 0, .5, .5)),
    "toxic_repeat_60": dict(rate=.6, weights=(.5, .5, 0, 0)),
}


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def file_hash(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def vector(row):
    """Keep saved scores; missing classifier outputs are unavailable, not inferred."""
    previous = row.get("previous")
    emotion = row["emotion"]
    return dict(
        id=row["id"],
        angry_abuse=min(row["toxicity"]["insult"], emotion["anger"])
        if "toxicity" in row else None,
        sad_tone=emotion["sadness"], anxious_tone=emotion["fear"],
        near_copy_cosine=row.get("repeat_similarity"),
        near_copy_nli=row.get("repeat_entailment"),
        numbers_equal=bool(previous is not None and
                           re.findall(r"\d+", row["text"]) == re.findall(r"\d+", previous)),
    )


def detects(v, cause, thresholds):
    if cause == "near_copy":
        return bool(v["near_copy_cosine"] is not None and v["numbers_equal"] and
                    v["near_copy_cosine"] >= thresholds["near_copy_cosine"] and
                    v["near_copy_nli"] >= thresholds["near_copy_nli"])
    return v[cause] is not None and v[cause] >= thresholds[cause]


def final_reason(v, causes):
    for cause in ("angry_abuse", "near_copy"):
        if cause in causes:
            return cause
    if "sad_tone" in causes and "anxious_tone" in causes:
        return "sad_tone" if v["sad_tone"] >= v["anxious_tone"] else "anxious_tone"
    return causes[0] if causes else None


def load_pools(selected, emotions):
    labelled = {
        "angry_abuse": [r for r in selected if r["category"] == "angry_abuse"],
        "near_copy": [r for r in selected if r["category"] == "near_copy"],
        "sad_tone": [r for r in emotions if r["cause"] == "sadness"],
        "anxious_tone": [r for r in emotions if r["cause"] == "fear"],
    }
    pools = {cause: {label: [vector(r) for r in rows if r["expected"] == label]
                     for label in (False, True)} for cause, rows in labelled.items()}
    return labelled, pools


def confusion(labelled, thresholds):
    result = {}
    for difficulty, dramas in thresholds.items():
        result[difficulty] = {}
        for drama, values in dramas.items():
            result[difficulty][drama] = {}
            for cause, rows in labelled.items():
                splits = {}
                for split in sorted({r["split"] for r in rows}):
                    counts = dict(tp=0, fp=0, fn=0, tn=0)
                    mistakes = []
                    for row in (r for r in rows if r["split"] == split):
                        predicted = detects(vector(row), cause, values)
                        expected = row["expected"]
                        counts[("t" if predicted == expected else "f") +
                               ("p" if predicted else "n")] += 1
                        if predicted != expected:
                            mistakes.append(row["id"])
                    counts.update(n=sum(counts.values()), mistakes=mistakes)
                    splits[split] = counts
                result[difficulty][drama][cause] = splits
    return result


def stable_roll(publication_id, purpose):
    value = int.from_bytes(hashlib.sha256(f"{publication_id}:{purpose}".encode()).digest()[:8], "little")
    return value / ((1 << 64) - 1)


def post_stream(config, persona, seed, days):
    person = PERSONAS[persona]
    rng = random.Random(seed)
    energy = config["posts"]["plus_per_day" if persona == "plus" else "free_per_day"]
    count = min(person["posts"], energy // config["energy"]["post_cost"])
    stream = []
    for day in range(1, days + 1):
        events = []
        if (day - 1) % person["every"] == 0:
            for index in range(1, count + 1):
                publication_id = f"issue244:{persona}:{seed}:{day}:{index}"
                events.append(tuple(rng.random() for _ in range(4)) +
                              (stable_roll(publication_id, "viral"), stable_roll(publication_id, "flop")))
        stream.append(events)
    return stream


def weighted_cause(roll, weights):
    cumulative = 0
    for cause, weight in zip(CAUSES, weights):
        cumulative += weight
        if roll < cumulative:
            return cause
    return CAUSES[-1]


def sample_vectors(pools, thresholds, scenario):
    """Precompute decisions; all saved measured signals use OR, one loss per post."""
    prepared = {}
    for cause in CAUSES:
        prepared[cause] = {}
        for label in (False, True):
            prepared[cause][label] = [
                (v, tuple(c for c in CAUSES if detects(v, c, thresholds)))
                for v in pools[cause][label]]
    return prepared


def run_group(config, thresholds, caps, drama_settings, pools, persona, difficulty, drama,
              scenario, seed, days, variants, diary=False, stream=None, decisions=None):
    """Modes share publication IDs and all random draws, even on losing posts."""
    person = PERSONAS[persona]
    setting = SCENARIOS[scenario]
    world = config["difficulties"][difficulty]
    growth = config["growth"]
    viral = config["viral"]
    decisions = decisions or sample_vectors(pools, thresholds[difficulty][drama], scenario)
    stream = stream or post_stream(config, persona, seed, days)
    drama_multiplier = drama_settings[drama]["loss_multiplier"]
    modes = ("baseline",) + tuple(variants)
    states = {mode: dict(subs=growth["start_subs"], milestones={}, loss=0,
                         flops=0, virals=0, negative_days=0, run=0, longest_run=0,
                         negative_day_run=0, longest_negative_day_run=0,
                         posts=0, target_posts=0, causes={c: 0 for c in CAUSES},
                         day30=None, diary=[]) for mode in modes}
    last_post_day = None
    decay = [next(item["x"] for item in growth["decay"] if item["from"] <= i <= item["to"])
             for i in range(1, person["posts"] + 1)]
    for day in range(1, days + 1):
        played = (day - 1) % person["every"] == 0
        rested = played and last_post_day is not None and day - last_post_day - 1 >= growth["rested_after_skip_days"]
        starts = {mode: state["subs"] for mode, state in states.items()}
        daily = {mode: dict(losses=0, virals=0, lost=0, cases=[]) for mode in modes}
        if played:
            for index, event in enumerate(stream[day - 1], 1):
                own_roll, rate_roll, cause_roll, sample_roll, viral_roll, flop_roll = event
                own = own_roll < person["own"]
                intended_cause = weighted_cause(cause_roll, setting["weights"])
                target = rate_roll < setting["rate"]
                pool = decisions[intended_cause][target]
                sampled_vector, causes = pool[min(int(sample_roll * len(pool)), len(pool) - 1)]
                case_id = sampled_vector["id"]
                if last_post_day is None and index == 1:
                    causes = tuple(c for c in causes if c != "near_copy")
                if setting.get("all_scores_zero"):
                    case_id, causes = "counterfactual_all_zero_scores", ()
                reason = final_reason(sampled_vector, causes)
                quality = person["quality"]
                probability = min(1, max(0, (viral["base"] + viral["per_quality"] * quality) * world["viral_x"]))
                can_viral = viral_roll < probability
                common = growth["k"] * world["k"] * (.5 + quality)
                for mode, state in states.items():
                    multiplier = 1
                    if state["subs"] < growth["starter_below_subs"]:
                        multiplier *= growth["starter_x"]
                    if own:
                        multiplier *= growth["own_words_x"]
                    if rested and index <= growth["rested_first_posts"]:
                        multiplier *= growth["rested_x"]
                    is_viral = can_viral and (mode == "baseline" or not causes)
                    losing = (not is_viral and flop_roll < world["flop"]) if mode == "baseline" else bool(causes)
                    if is_viral:
                        multiplier *= viral["x"] * world["viral_x"]
                    amount = common * max(1, state["subs"]) ** growth["power"] * decay[index - 1] * multiplier
                    if losing:
                        loss_x = config["flop"]["loss_x"] if mode == "baseline" else -variants[mode][difficulty] * drama_multiplier
                        delta = min(-1, round(amount * loss_x))
                        if mode != "baseline":
                            delta = -min(-delta, max(1, math.floor(state["subs"] * caps[difficulty] * drama_multiplier)))
                    else:
                        delta = max(1, round(amount))
                    before = state["subs"]
                    state["subs"] = max(0, before + delta)
                    actual_delta = state["subs"] - before
                    state["posts"] += 1
                    state["target_posts"] += int(target)
                    state["flops"] += int(losing)
                    state["virals"] += int(is_viral)
                    state["loss"] += max(0, -actual_delta)
                    state["run"] = state["run"] + 1 if losing else 0
                    state["longest_run"] = max(state["longest_run"], state["run"])
                    daily[mode]["losses"] += int(losing)
                    daily[mode]["virals"] += int(is_viral)
                    daily[mode]["lost"] += max(0, -actual_delta)
                    if mode != "baseline" and causes:
                        for cause in causes:
                            state["causes"][cause] += 1
                        if diary and day <= 10:
                            daily[mode]["cases"].append(dict(id=case_id, causes=causes, final_reason=reason))
                    for milestone in MILESTONES:
                        if state["subs"] >= milestone and milestone not in state["milestones"]:
                            state["milestones"][milestone] = day
            last_post_day = day
        for mode, state in states.items():
            negative = state["subs"] < starts[mode]
            state["negative_days"] += int(negative)
            state["negative_day_run"] = state["negative_day_run"] + 1 if negative else 0
            state["longest_negative_day_run"] = max(state["longest_negative_day_run"], state["negative_day_run"])
            if day == 30:
                state["day30"] = state["subs"]
            if diary and day <= 10:
                state["diary"].append(dict(day=day, played=played, rested=rested,
                                          subscribers=state["subs"], delta=state["subs"] - starts[mode], **daily[mode]))
    return states


def summarize(runs, days):
    med = lambda key: statistics.median(r[key] for r in runs)
    milestone_summary = {}
    for milestone in MILESTONES:
        values = sorted(r["milestones"].get(milestone, math.inf) for r in runs)
        middle = statistics.median(values)
        milestone_summary[str(milestone)] = dict(
            median_day=None if math.isinf(middle) else middle,
            unreached_fraction=sum(math.isinf(v) for v in values) / len(values))
    total_posts = sum(r["posts"] for r in runs)
    return dict(
        seeds=len(runs), milestones=milestone_summary,
        subscribers_day30_median=med("day30"), subscribers_final_median=med("subs"),
        subscribers_final_p10=sorted(r["subs"] for r in runs)[int((len(runs) - 1) * .1)],
        subscribers_final_p90=sorted(r["subs"] for r in runs)[int((len(runs) - 1) * .9)],
        posts_per_seed=med("posts"), target_fraction=sum(r["target_posts"] for r in runs) / total_posts,
        loss_post_fraction=sum(r["flops"] for r in runs) / total_posts,
        viral_post_fraction=sum(r["virals"] for r in runs) / total_posts,
        realized_lost_subscribers_per_calendar_day=statistics.mean(r["loss"] / days for r in runs),
        negative_calendar_days_median=med("negative_days"),
        longest_losing_post_run_median=med("longest_run"),
        longest_losing_post_run_max=max(r["longest_run"] for r in runs),
        losing_post_run_3_fraction=sum(r["longest_run"] >= 3 for r in runs) / len(runs),
        longest_negative_calendar_day_run_median=med("longest_negative_day_run"),
        detected_causes_per_100_posts={c: 100 * sum(r["causes"][c] for r in runs) / total_posts for c in CAUSES})


def execute(config, thresholds, caps, drama_settings, pools, personas, scenarios, seeds,
            days, variants):
    rows = []
    prepared = {(d, drama): sample_vectors(pools, thresholds[d][drama], None)
                for d in DIFFICULTIES for drama in DRAMAS}
    for persona in personas:
        groups = {}
        scenario_variants = {}
        for scenario in scenarios:
            scenario_variants[scenario] = variants if scenario == "balanced_10" else {"selected": variants["selected"]}
            for difficulty in DIFFICULTIES:
                for drama in DRAMAS:
                    groups[scenario, difficulty, drama] = {mode: [] for mode in ("baseline",) + tuple(scenario_variants[scenario])}
        for seed in range(seeds):
            stream = post_stream(config, persona, seed, days)
            for (scenario, difficulty, drama), modes in groups.items():
                result = run_group(config, thresholds, caps, drama_settings, pools, persona,
                                   difficulty, drama, scenario, seed, days, scenario_variants[scenario],
                                   stream=stream, decisions=prepared[difficulty, drama])
                for mode in modes:
                    modes[mode].append(result[mode])
            if (seed + 1) % 50 == 0:
                print(f"progress {persona} {seed + 1}/{seeds} seeds x {len(groups)} cells", flush=True)
        for (scenario, difficulty, drama), modes in groups.items():
            rows.extend(dict(persona=persona, difficulty=difficulty, drama=drama,
                             scenario=scenario, mode=mode, **summarize(runs, days))
                        for mode, runs in modes.items())
    return rows


def format_report(report):
    lines = ["#244: content-driven unsubscribe balance experiment", "",
             "All rates, quality, attendance and own-word shares below are assumptions, not player measurements.",
             "Fixed quality, no skill drift. No fabricated daily/event growth. One world, no comments, invites,",
             "streak bonuses, challenge rewards, or +5 fake-door energy. All unchanged economic numbers come from balance.json.",
             "Baseline retains current random flops; candidates replace them. Cause wins over viral; no random rescue.",
             "Sampling: choose intended cause by mixture, then target/control saved vector by assumed rate;",
             "all measured signals are evaluated together (OR); simultaneous causes cost once. Missing scores are unavailable.",
             "neutral_0 means intended-cause target rate0: labelled controls can carry other measured signals.",
             "zero_scores_control is a separate counterfactual with all classifier scores0, not empirical corpus sampling.",
             "Repeat uses saved cosine AND mutual entailment AND equal ordered numeric tokens.",
             "First post cannot trigger near_copy: no own-post history. Baseline has no added loss cap.",
             "Candidate base loss_x .08/.12/.16; base fraction cap .005/.01/.015 (easy/normal/hard).",
             "Drama off/soft/mid/hot multiplies both loss and cap by .5/.75/1/1.25; it also lowers thresholds.",
             "Candidates cap requested loss at max(1,floor(currentSubscribers * baseCap * dramaMultiplier)).",
             "Minimum1 may exceed the percentage for tiny channels; actual loss never exceeds subscribers (floor0).",
             "Loss/day means gross realized subscriber loss per calendar day, after floor0, not net growth.",
             "Negative days means end-of-day subscribers lower than start; losing run counts consecutive losing posts.",
             "Milestone median includes all seeds with unreached treated as infinity; '-' means censored at horizon.",
             "Classifier samples are tiny synthetic labelled challenges, not a production validation set.", "",
             f"Main: {report['metadata']['main_seeds']} seeds, {report['metadata']['days']} days, balanced10%.",
             "Main compares baseline/selected; balanced10 sensitivity also mild .05/.08/.10 and strong .12/.18/.24.", "",
             "INITIAL LOSS DIAGNOSTIC: S120, quality.5, ownwords, first energy position; uncapped versus capped"]
    for row in report["initial_loss_diagnostic"]:
        lines.append(f"{row['difficulty']:6} {row['drama']:4} rested={row['rested']} G={row['growth']:.2f} " +
                     f"selected uncapped={row['uncapped_loss']} capped={row['capped_loss']}")
    lines.extend(["",
             "CONFUSION (only labelled relevant-cause subsets; TP/FP/FN/TN)"])
    for difficulty, dramas in report["confusion"].items():
        for drama, causes in dramas.items():
            for cause, splits in causes.items():
                for split, row in splits.items():
                    lines.append(f"{difficulty:6} {drama:4} {cause:13} {split:9} " +
                                 "/".join(str(row[k]) for k in ("tp", "fp", "fn", "tn")) +
                                 (" mistakes=" + ",".join(row["mistakes"]) if row["mistakes"] else ""))
    lines.extend(["", "MAIN", "persona difficulty drama mode | d1k d5k d10k d100k d1m (unreached%) | subs30 | lossPosts% | loss/day | negDays | losingRun median/max"])
    for row in report["main"]:
        milestones = []
        for value in row["milestones"].values():
            day = "-" if value["median_day"] is None else f"{value['median_day']:g}"
            milestones.append(f"{day}({100 * value['unreached_fraction']:.0f}%)")
        lines.append(f"{row['persona']:8} {row['difficulty']:6} {row['drama']:4} {row['mode']:8} | " +
                     " ".join(milestones) +
                     f" | {row['subscribers_day30_median']:,.0f} | {100 * row['loss_post_fraction']:.2f}%" +
                     f" | {row['realized_lost_subscribers_per_calendar_day']:,.1f}" +
                     f" | {row['negative_calendar_days_median']:g}" +
                     f" | {row['longest_losing_post_run_median']:g}/{row['longest_losing_post_run_max']}")
    lines.extend(["", "SENSITIVITY: average persona, fewer seeds; balanced10 also compares loss variants",
                  "scenario difficulty drama mode | subs30 | d1m(unreached%) | lossPosts% | negDays | losingRun"])
    for row in report["sensitivity"]:
        value = row["milestones"]["1000000"]
        day = "-" if value["median_day"] is None else f"{value['median_day']:g}"
        lines.append(f"{row['scenario']:19} {row['difficulty']:6} {row['drama']:4} {row['mode']:8}" +
                     f" | {row['subscribers_day30_median']:,.0f} | {day}({100*value['unreached_fraction']:.0f}%)" +
                     f" | {100*row['loss_post_fraction']:.2f}% | {row['negative_calendar_days_median']:g}" +
                     f" | {row['longest_losing_post_run_median']:g}")
    lines.extend(["", "DIARY: seed0, balanced10%, first10days; exact sampled IDs in JSON"])
    for diary in report["diaries"]:
        lines.append(f"{diary['persona']} / {diary['difficulty']} / {diary['drama']}")
        for before, after in zip(diary["baseline"], diary["selected"]):
            lines.append(f"day{before['day']:2} " + ("rest " if after["rested"] else "     ") +
                         f"baseline {before['subscribers']:7,} ({before['delta']:+,}, loss {before['losses']}, viral {before['virals']}); " +
                         f"selected {after['subscribers']:7,} ({after['delta']:+,}, loss {after['losses']}, viral {after['virals']})" +
                         ("; no visit" if not after["played"] else ""))
    return "\n".join(lines) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seeds", type=int, default=200)
    parser.add_argument("--sensitivity-seeds", type=int, default=50)
    parser.add_argument("--days", type=int, default=180)
    args = parser.parse_args()
    config_path = FOLDER.parent / "config" / "balance.json"
    selected_path = FOLDER / "issue-244-selected-causes-results.json"
    emotion_path = FOLDER / "issue-244-emotion-causes-results.json"
    config = read_json(config_path)
    proposed = config.get("unsubscribe", {})
    base_thresholds = proposed.get("thresholds", THRESHOLDS)
    drama_settings = proposed.get("drama", DRAMA_SETTINGS)
    reductions = proposed.get("threshold_reduction_per_step", THRESHOLD_REDUCTION)
    thresholds = {difficulty: {drama: {
        key: value - drama_settings[drama]["threshold_steps"] * reductions[key]
        for key, value in values.items()} for drama in DRAMAS}
        for difficulty, values in base_thresholds.items()}
    caps = proposed.get("max_subscriber_fraction", LOSS_CAPS)
    variants = dict(LOSS_VARIANTS)
    variants["selected"] = proposed.get("loss_x", variants["selected"])
    labelled, pools = load_pools(read_json(selected_path)["cases"], read_json(emotion_path)["cases"])
    report = dict(metadata=dict(
        seed_range=[0, args.seeds - 1], main_seeds=args.seeds,
        sensitivity_seeds=args.sensitivity_seeds, days=args.days,
        source_hashes={p.name: file_hash(p) for p in (config_path, selected_path, emotion_path, Path(__file__))},
        base_thresholds=base_thresholds, effective_thresholds=thresholds,
        drama=drama_settings, threshold_reduction_per_step=reductions,
        thresholds_monotonic=(all(thresholds[DIFFICULTIES[i]][drama][key] >= thresholds[DIFFICULTIES[i+1]][drama][key]
             for i in range(2) for drama in DRAMAS for key in reductions) and
             all(thresholds[d][DRAMAS[i]][key] >= thresholds[d][DRAMAS[i+1]][key]
             for d in DIFFICULTIES for i in range(3) for key in reductions)),
        loss_variants=variants, max_subscriber_fraction=caps,
        personas=PERSONAS, scenarios=SCENARIOS,
        assumptions=["All classifier vectors are synthetic saved inference results; target rates are assumed.",
                     "Emotional rows have no toxicity/repeat output: unavailable signals cannot trigger.",
                     "neutral_0 is intended cause rate0; own-category controls may carry other measured cause signals.",
                     "zero_scores_control deliberately zeroes all classifier scores as a counterfactual control.",
                     "Quality is a constant per persona, with no invented skill-quality bonus.",
                     "One world; no comments, invitation, streak, challenge or fake-door bonuses.",
                     "Activity days are deterministic, every/every-other/every-third day; no retention claims.",
                     "The same seed and publication IDs produce the same draws across modes and scenarios.",
                     "Subscriber floor is0; stored post delta can exceed realized loss near0.",
                     "Candidate loss=min(max(1,round(G*loss_x*drama)),max(1,floor(S*cap*drama))); subscribers floor0.",
                     "Minimum1 may exceed nominal percentage at small S; first publication cannot trigger repeat.",
                     "Rested requires2 skipped days and a previous post, first3 energy positions only."]),
        confusion=confusion(labelled, thresholds))
    report["initial_loss_diagnostic"] = []
    for difficulty in DIFFICULTIES:
        for drama in DRAMAS:
            for rested in (False, True):
                growth = config["growth"]
                multiplier = drama_settings[drama]["loss_multiplier"]
                amount = (growth["k"] * config["difficulties"][difficulty]["k"] * 120 ** growth["power"] *
                          growth["starter_x"] * growth["own_words_x"] * (growth["rested_x"] if rested else 1))
                uncapped = max(1, round(amount * variants["selected"][difficulty] * multiplier))
                report["initial_loss_diagnostic"].append(dict(difficulty=difficulty, drama=drama, rested=rested,
                    growth=amount, uncapped_loss=uncapped,
                    capped_loss=min(uncapped, max(1, math.floor(120 * caps[difficulty] * multiplier)))))
    report["main"] = execute(config, thresholds, caps, drama_settings, pools, PERSONAS, ("balanced_10",),
                             args.seeds, args.days, {"selected": variants["selected"]})
    report["sensitivity"] = execute(config, thresholds, caps, drama_settings, pools, ("average",), SCENARIOS,
                                    args.sensitivity_seeds, args.days, variants)
    report["diaries"] = []
    for persona in ("casual", "average", "weak", "returner"):
        difficulty = PERSONAS[persona]["preferred"]
        drama = "hot"
        result = run_group(config, thresholds, caps, drama_settings, pools, persona, difficulty, drama, "balanced_10", 0,
                           max(30, min(args.days, 180)), {"selected": variants["selected"]}, diary=True)
        report["diaries"].append(dict(persona=persona, difficulty=difficulty, drama=drama,
                                      **{mode: result[mode]["diary"] for mode in result}))
    (FOLDER / "out_issue_244_balance.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (FOLDER / "out_issue_244_balance.txt").write_text(format_report(report), encoding="utf-8")
    print("saved out_issue_244_balance.json and out_issue_244_balance.txt", flush=True)


if __name__ == "__main__":
    main()
