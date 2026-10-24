"""Original Cloudline score and sound synthesis. No sampled or external music.
80 BPM, D major, 32 bars / 96 seconds; deterministic stereo periodic reverb.
Run with numpy and ffmpeg. Outputs Unity-ready WAV and a signal QC report.
"""
from pathlib import Path
import json, wave
import numpy as np
SR=44100
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/CloudRail/Audio'
OUT.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(73021)
qc={}
def export(name,x):
    if x.ndim==1:x=np.column_stack((x,x))
    peak=np.max(np.abs(x)); x=x*(.72/max(peak,.00001))
    qc[name]={'seconds':round(len(x)/SR,3),'peak':round(float(np.max(abs(x))),4),'rms':round(float(np.sqrt(np.mean(x*x))),4),'boundary_step':round(float(np.max(abs(x[-1]-x[0]))),6)}
    with wave.open(str(OUT/(name+'.wav')),'wb') as f:
        f.setnchannels(2);f.setsampwidth(2);f.setframerate(SR);f.writeframes((np.clip(x,-1,1)*32767).astype('<i2').tobytes())

def hz(m):return 440*2**((m-69)/12)
def note(m,dur,kind):
    t=np.arange(int(dur*SR))/SR; f=hz(m)
    if kind=='piano':
        x=sum(a*np.sin(2*np.pi*f*k*t+.06*k)*np.exp(-t*(.65+.34*k)) for k,a in [(1,1),(2,.28),(3,.12),(4,.045)])
        x*=np.minimum(t/.009,1)*np.minimum((dur-t)/.25,1)
    elif kind=='bell':
        x=(np.sin(2*np.pi*f*t)*np.exp(-t*1.1)+.17*np.sin(2*np.pi*f*2.002*t)*np.exp(-t*2.2))
        x*=np.minimum(t/.006,1)*np.minimum((dur-t)/.3,1)
    else:
        x=sum(np.sin(2*np.pi*(f+detune)*t+phase) for detune,phase in [(-.35,0),(0,.7),(.35,1.5)])/3
        x*=np.sin(np.pi*np.clip(t/dur,0,1))**1.8
    return x
N=96*SR;score=np.zeros((N,2),np.float64)
def put(m,start,dur,kind,amp,pan=0):
    x=note(m,dur,kind)*amp; ids=(np.arange(len(x))+int(start*SR))%N
    score[ids,0]+=x*np.sqrt((1-pan)/2);score[ids,1]+=x*np.sqrt((1+pan)/2)
chords=[[50,57,61,66,69],[45,52,59,64,69],[47,54,57,62,66],[43,50,57,59,66],
        [50,57,61,66,73],[45,52,57,59,64],[43,50,55,59,66],[45,52,57,62,64]]
melody=[[78,None,76,73],[76,73,None,69],[74,None,73,71],[71,69,66,None],
        [73,74,78,81],[80,None,76,73],[74,73,71,69],[73,None,69,None]]
beat=.75
for bar in range(32):
    c=chords[bar%8];s=bar*3
    put(c[0],s,4.5,'piano',.17,-.12)
    for j,m in enumerate(c[1:]):put(m,s+.08+j*.045,5,'pad',.034,(-.4+j*.25))
    for j,idx in enumerate([1,3,2,4,2,3]):
        put(c[idx],s+j*.5,2.6,'piano',.085 if j%2==0 else .06,(-.3+j*.1))
    # Sparse four-phrase melody; the second half moves to a gentler upper octave.
    for b,m in enumerate(melody[bar%8]):
        if m is not None and not (bar<8 and b%2==1):
            put(m,s+b*beat,3,'bell' if bar//8 in (1,3) else 'piano',.095 if bar//8 in (1,3) else .12,.2)
for delay,gain in [(.23,.14),(.41,.1),(.67,.08),(1.07,.055),(1.63,.035)]:
    score+=np.roll(score[:,::-1],int(SR*delay),axis=0)*gain
export('Cloudline_Theme',score)
def filtered_noise(seconds,cutoff):
    n=int(seconds*SR);noise=rng.standard_normal(n);freq=np.fft.rfftfreq(n,1/SR)
    spectrum=np.fft.rfft(noise);spectrum*=np.exp(-(freq/cutoff)**2)*(1-np.exp(-(freq/55)**2))
    return np.fft.irfft(spectrum,n)
t=np.arange(SR*12)/SR
wind=filtered_noise(12,650);wind*=.65+.2*np.sin(2*np.pi*t/12)+.1*np.sin(2*np.pi*t/4)
export('Cloud_Wind',np.column_stack((wind,np.roll(wind,SR//7))))
t=np.arange(SR*4)/SR
roll=filtered_noise(4,360)*.09
for start in np.arange(0,4,.5):
    for off,amp in [(0,1),(.105,.6)]:
        u=np.arange(int(.13*SR))/SR;x=(rng.standard_normal(len(u))*.12+np.sin(2*np.pi*145*u)*.1)*np.exp(-u*45)*np.minimum(u/.004,1)*amp
        ids=(np.arange(len(x))+int((start+off)*SR))%len(roll);roll[ids]+=x
export('Rail_Rhythm',roll)
t=np.arange(int(2.8*SR))/SR;env=np.minimum(t/.17,1)*np.minimum(np.maximum(2.8-t,0)/.8,1)
w=sum(a*np.sin(2*np.pi*f*t+np.sin(t*8)*.09) for f,a in [(440,.55),(554.365,.32),(659.255,.24),(880,.08)])
w*=env*(.96+.04*np.sin(t*24));export('Departure_Whistle',w)
f=np.zeros((SR*7,2));
for i,m in enumerate([62,66,69,74,78]):
    x=note(m,5,'bell')*.22;start=int(i*.35*SR);f[start:start+len(x),:]+=x[:,None]*np.array([.7,.7])
export('Clock_Awakening',f)
(ROOT/'Documentation/里程碑03_音频信号验证.json').write_text(json.dumps(qc,indent=2,ensure_ascii=False))
print(json.dumps(qc,indent=2))
