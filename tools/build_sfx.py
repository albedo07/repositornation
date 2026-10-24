# v0.25.227 (user rule): skill SFX are the supplied recordings exactly as given. This only decodes the MP3s to WAV
# (same sample rate and channels, 16-bit) because the mod loads WAV. No trimming, leveling, filtering or fades.
# Usage: python3 -I tools/build_sfx.py docs/source_audio/aa_sfx ImmortalHeroesAssets/SFX
import subprocess, sys, os
src, out = sys.argv[1], sys.argv[2]
for n in ['HeavySwing', 'HeavySwing2', 'SwordSwing', 'HeavyPunch', 'SmallBoom', 'BigWhoosh', 'BigWhoosh2', 'BassDrop', 'Buff', 'SpinningSwing', 'Rumbling', 'SwordSlash', 'SwordSlash2', 'SolidBoom', 'MidBoom', 'SwordSwing2', 'BoulderCrash', 'Tornado', 'Ahhhh', 'Bell', 'Bell2', 'Buff2', 'Electric', 'Esmite', 'Holy', 'Judgement', 'Thud', 'Whoosh', 'LightningStorm']:
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', os.path.join(src, n + '.mp3'), '-c:a', 'pcm_s16le', os.path.join(out, n + '.wav')], check=True)
# BladeStorm.wav is copied byte-for-byte from docs/source_audio/aa_sfx/SM_BladeStorm.wav.
