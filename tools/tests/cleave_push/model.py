# v0.25.170 Crescent Cleave push/damage model (logic only, no Unity). Mirrors CrescentCleaveWave + CleavePullStep along the wave axis.
# Usage: python3 tools/tests/cleave_push/model.py
RANGE, TRAVEL, TICK, HALF_DEPTH, FRONT_GAP, DT = 20.0, 5.5, 0.5, 1.25, 0.9, 1/60
SPEED = (RANGE - 0.35) / TRAVEL
def run(start, pct, gate):
    x, t, count, next_at, pushed_no_dmg, ticks, dmg_times = start, 0.0, 0, -1.0, 0.0, 0, []
    while t <= TRAVEL:
        p = 0.35 + (RANGE - 0.35) * min(1, t / TRAVEL)
        inside = abs(x - p) <= HALF_DEPTH + 0.4          # body radius 0.4
        if inside:
            can = count < 2
            if (can or not gate):
                step = min(SPEED * pct * DT, (p + FRONT_GAP) - x)
                if step > 0:
                    x += step
                    if not can: pushed_no_dmg += step
            if can and t >= next_at:
                count += 1; next_at = t + TICK; dmg_times.append(round(t, 2))
        t += DT
    return count, pushed_no_dmg, x - start, dmg_times
fails = 0
for name, pct in (("small", 1.0), ("big", 0.65), ("boss", 0.35)):
    for start in (2.0, 6.0, 12.0):
        c0, nd0, d0, _ = run(start, pct, False)
        c1, nd1, d1, times = run(start, pct, True)
        ok = c1 == c0 == 2 and nd1 == 0.0
        fails += not ok
        print("%-5s start %4.1f m | old: ticks %d, pushed %.1f m (%.1f m with no damage) | new: ticks %d %s, pushed %.1f m, %.1f m with no damage %s"
              % (name, start, c0, d0, nd0, c1, times, d1, nd1, "OK" if ok else "FAIL"))
print("ALL PASS" if fails == 0 else "%d FAIL" % fails)
