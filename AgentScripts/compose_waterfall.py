# Original deterministic waterfall synthesis; generated for Cloudline.
from pathlib import Path
import numpy as np, wave
sr=44100;n=sr*8;rng=np.random.default_rng(813);freq=np.fft.rfftfreq(n,1/sr)
x=np.fft.irfft(np.fft.rfft(rng.standard_normal(n))*np.exp(-(freq/5200)**2)*(1-np.exp(-(freq/120)**2)),n)
t=np.arange(n)/sr;x*=.8+.12*np.sin(2*np.pi*t/8)+.06*np.sin(2*np.pi*t/2)
x=np.column_stack((x,np.roll(x,927)));x*=.6/abs(x).max()
with wave.open(str(Path(__file__).resolve().parents[1]/'Assets/CloudRail/Audio/Waterfall.wav'),'wb') as f:
 f.setnchannels(2);f.setsampwidth(2);f.setframerate(sr);f.writeframes((x*32767).astype('<i2').tobytes())
