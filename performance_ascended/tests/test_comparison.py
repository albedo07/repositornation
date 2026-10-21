import importlib.util
from pathlib import Path
p=Path(__file__).resolve().parents[1]/'tools/compare_reports.py'
spec=importlib.util.spec_from_file_location('compare_reports',p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
context='Scenario=base\nDetailedDiagnostics=True; RenderingCounters=True\nUnity=6000; CPU=CPU; GPU=GPU\nResolution=1920x1080; VSync=0; TargetFPS=-1\nGraphicsAPI=Direct3D11; QualityLevel=1; QualityShadowDistance=60\nLoaded BepInEx plugins (inventory, not measured cost):\nplugin.a 1.0\nFrames=100; '
a=context+'AverageFPS=40; OnePercentLowFPS=20\nP99_ms=50; Worst_ms=100; Over100ms=0\n'
b=context+'AverageFPS=50; OnePercentLowFPS=25\nP99_ms=40; Worst_ms=80; Over100ms=0\n'
s=m.compare(a,b);assert '40 -> 50 (+25.00%)' in s and '50 -> 40 (-20.00%)' in s and 'CHECK' not in s
s=m.compare(a,b.replace('Scenario=base','Scenario=combat'));assert 'CHECK Scenario' in s
assert 'inventories differ' in m.compare(a,b.replace('plugin.a 1.0','plugin.b 1.0'))
assert 'unavailable' in m.compare('','')
assert '(n/a)' in m.compare(a,b)
assert 'CHECK: WARNING: Building discovery' in m.compare(a,b+'WARNING: Building discovery/applying incomplete\n')
print('PASS: offline comparison handles improvements, mismatched scenarios/mods, missing data and zero baselines.')
