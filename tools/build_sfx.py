# Builds the equal-loudness SFX bank. Usage: python3 -I build_sfx.py RAW_DIR OUT_DIR
import numpy as np, subprocess, sys, re, os, wave
RAW, OUT = sys.argv[1], sys.argv[2]; SR = 44100; os.makedirs(OUT, exist_ok=True)
TARGET = -9.0      # max momentary loudness (LUFS) every sound is matched to
CEIL = 10 ** (-0.3 / 20)
def load(n): return np.frombuffer(subprocess.run(['ffmpeg','-v','error','-i',f'{RAW}/{n}.wav','-f','f32le','-'],capture_output=True).stdout,np.float32).astype(np.float64)
def seg(x, a, b=None): return x[int(a*SR): None if b is None else int(b*SR)].copy()
def env(x, w=441): n=len(x)//w; return np.sqrt((x[:n*w].reshape(n,w)**2).mean(1)+1e-12)
def autotrim(x, lead=0.005, onset=0.0316):
    e = env(x); pk = e.max(); on = np.argmax(e > pk*onset)          # default -30 dB onset
    end = len(e) - np.argmax(e[::-1] > pk*0.001)                    # -60 dB tail
    return x[max(0, on*441 - int(lead*SR)): end*441]
def fade(x, fin=0.003, fout=0.04):
    x = x.copy(); a=int(fin*SR); b=min(len(x),int(fout*SR))
    if a: x[:a] *= np.linspace(0,1,a)
    x[len(x)-b:] *= np.linspace(1,0,b); return x
def cut(x, length, fout): return fade(x[:int(length*SR)], 0.003, fout)
def hp(x, f):  # one-pole high-pass
    a = np.exp(-2*np.pi*f/SR); y=np.zeros_like(x); px=py=0.0
    for i,v in enumerate(x): py = a*(py + v - px); px = v; y[i]=py
    return y
def lp(x, f):
    a = 1-np.exp(-2*np.pi*f/SR); y=np.zeros_like(x); p=0.0
    for i,v in enumerate(x): p += a*(v-p); y[i]=p
    return y
def mloud(x):
    pad = np.concatenate([x, np.zeros(SR//2)]).astype(np.float32).tobytes()
    r = subprocess.run(['ffmpeg','-hide_banner','-nostats','-v','verbose','-f','f32le','-ar',str(SR),'-ac','1','-i','-','-af','ebur128=framelog=verbose','-f','null','-'],input=pad,capture_output=True).stderr.decode()
    m = [float(v) for v in re.findall(r' M:\s*(-?[\d.]+)', r)]
    return max(m) if m else -70.0
def limit(x):  # soft knee above -6 dB of the ceiling, never past CEIL
    k = CEIL*0.5; y = x.copy(); m = np.abs(y) > k
    y[m] = np.sign(y[m]) * (k + (CEIL-k) * np.tanh((np.abs(y[m])-k)/(CEIL-k)))
    return y
def level(x):
    for _ in range(4):
        x = limit(x * 10 ** ((TARGET - mloud(x)) / 20))
    return x
def save(n, x):
    x = level(fade(x - x.mean()))
    with wave.open(f'{OUT}/{n}.wav','wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((np.clip(x,-1,1)*32767).astype('<i2').tobytes())
    print(f'{n:18s} {len(x)/SR:5.2f}s  M {mloud(x):6.1f}  pk {20*np.log10(abs(x).max()):5.1f}  peakAt {np.argmax(env(x))*0.01:.2f}s')
A = autotrim
for n in ['HeavyPunch','Buff']: save(n, A(load(n)))
# Played at the contact frame: start close to the swing's peak (-15 dB onset, 30 ms run-in) so the whoosh lands on the hit.
for n in ['HeavySwing','HeavySwing2','SwordSwing','MagicSlash','MagicSlash2']: save(n, fade(A(load(n), 0.03, 0.178), 0.02, 0.04))
save('SmallBoom', cut(A(load('SmallBoom')), 1.4, 0.5))
def claudeslash():
    # v223 (user: steel, not a laser; very high like Judgement Cut): GPTslash raised 4 semitones (shorter, brighter)
    # + a crisp 3-14 kHz steel hiss on the edge + a faint high glassy ring. Dry, no reverb.
    g = A(load('old_SM_GPTslash')); f = 2 ** (4 / 12)
    g = np.interp(np.arange(0, len(g) - 1, f), np.arange(len(g)), g)
    rng = np.random.default_rng(11); n = len(g) + int(0.03 * SR); t = np.arange(n) / SR
    base = np.zeros(n); base[:len(g)] = g / np.abs(g).max()
    hiss = rng.standard_normal(n); hiss = hiss - lp(hiss, 3000); hiss = lp(hiss, 14000)
    hiss *= np.clip(t / 0.003, 0, 1) * np.exp(-t / 0.05)
    ring = sum(a * np.sin(2 * np.pi * fr * t + rng.uniform(0, 6.3)) * (1 + 0.3 * np.sin(2 * np.pi * rng.uniform(25, 60) * t))
               for fr, a in ((6400, 1), (8900, .7), (11700, .5)))
    ring *= np.clip(t / 0.004, 0, 1) * np.exp(-t / 0.04)
    return base + hiss / np.abs(hiss).max() * 0.45 + ring / np.abs(ring).max() * 0.12
save('Claudeslash', claudeslash())
bw = load('BigWhoosh')
save('BigWhooshClimb', fade(seg(bw, 0.42, 0.98), 0.003, 0.01))
save('BigWhooshDrop', cut(seg(bw, 0.98), 0.8, 0.35))
save('BigWhoosh2', cut(A(seg(load('BigWhoosh2'), 0.20)), 1.2, 0.4))
bass = seg(load('BassDrop'), 0.36)
save('BassDrop', cut(bass, 1.2, 0.5))
# Blade Storm: the approved recording, rumble/hiss trimmed, with the BassDrop body underneath it (bass above the slashes).
bs = lp(hp(load('BladeStorm'), 35), 11000)
b2 = cut(bass, len(bs)/SR, 0.35)
bs = bs/np.abs(bs).max()*0.7 + b2/np.abs(b2).max()*0.85
save('BladeStorm', bs)
save('SM_CCRumble', A(load('old_SM_CCRumble')))
# Circle Swing (Mercenary): one turn of the spinning swing; low debris from the rumble recording.
save('SpinningSwing', fade(seg(load('SpinningSwing'), 0.0, 0.95), 0.003, 0.12))
save('Rumbling', cut(lp(lp(seg(load('Rumbling'), 2.0, 3.4), 700), 700), 1.2, 0.5))
