# Measure a WAV/MP3 the way this project levels sounds: length, momentary loudness (max 400 ms RMS, dB),
# lead/marker (first 10 ms window >= 70% of the loudest window = DefaultMarker) and leading-silence onset.
# usage: python3 measure_audio.py file1 [file2 ...]   (needs ffmpeg + numpy)
import subprocess, sys, numpy as np
for f in sys.argv[1:]:
    x = np.frombuffer(subprocess.run(['ffmpeg','-loglevel','error','-i',f,'-ac','1','-ar','48000','-f','s16le','-'],capture_output=True).stdout, np.int16) / 32768.
    c = np.convolve(x*x, np.ones(19200)/19200, 'valid'); mom = 10*np.log10(c.max()+1e-12)
    e = np.array([np.sqrt((x[i:i+480]**2).mean()) for i in range(0, len(x)-480, 480)])
    lead = np.argmax(e >= e.max()*0.7) * 10
    m = np.abs(x); onset = np.argmax(m > 0.01*1) * 1000 // 48000
    print('%-24s %5.2fs  momentary %6.1f dB  marker %4d ms  onset %4d ms' % (f.split('/')[-1], len(x)/48000, mom, lead, onset))
# Gain % for a target momentary level T (UserLayerGain): g = 10**((T - mom)/20); percent = g*100 if g <= 1 else sqrt(g)*100
