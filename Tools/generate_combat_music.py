"""Render an original 16-bar industrial synth loop. No samples or third-party recordings."""
import math, random, struct, wave
from pathlib import Path
RATE=22050
BPM=112
BEAT=60/BPM
DURATION=64*BEAT
rng=random.Random(139)
audio=[0.0]*round(DURATION*RATE)
def note(at,duration,midi,gain,voice):
    freq=440*2**((midi-69)/12)
    for i in range(round(duration*RATE)):
        t=i/RATE; phase=math.tau*freq*t
        env=min(1,t/.012)*min(1,(duration-t)/.08)
        if voice=='bass': value=(math.sin(phase)+.24*math.sin(phase*2))*.7
        elif voice=='bell': value=(math.sin(phase)+.23*math.sin(phase*3.01))*math.exp(-t*5)
        else: value=(math.sin(phase)+.15*math.sin(phase*2)+.08*math.sin(phase*3))*.6
        audio[(round(at*RATE)+i)%len(audio)]+=gain*env*value
def drum(at,kind):
    duration=.22 if kind=='kick' else .12
    for i in range(round(duration*RATE)):
        t=i/RATE
        if kind=='kick': value=math.sin(math.tau*(49*t+2.4*(1-math.exp(-t*30))))*math.exp(-t*19)*.23
        elif kind=='snare': value=(rng.uniform(-1,1)*.6+math.sin(math.tau*185*t)*.4)*math.exp(-t*32)*.13
        else: value=rng.uniform(-1,1)*math.exp(-t*85)*.045
        audio[(round(at*RATE)+i)%len(audio)]+=value
for bar in range(16):
    root=[38,34,41,36][bar//4]
    for beat in range(4):
        at=(bar*4+beat)*BEAT
        note(at,BEAT*.75,root+(12 if beat==3 else 0),.17,'bass')
        drum(at,'kick' if beat%2==0 else 'snare')
        for sub in range(2):drum(at+sub*BEAT/2,'hat')
    for pitch in [root+24,root+31,root+36]:note(bar*4*BEAT,4*BEAT,pitch,.035,'pad')
    motif=[0,7,12,10,7,3,5,7]
    for step,interval in enumerate(motif):note((bar*4+step/2)*BEAT,BEAT*.4,root+24+interval,.06 if bar<8 else .085,'bell')
out=Path(__file__).resolve().parents[1]/'Assets/Resources/Audio/SalvageDrive.wav'
with wave.open(str(out),'wb') as wav:
    wav.setnchannels(1);wav.setsampwidth(2);wav.setframerate(RATE)
    wav.writeframes(b''.join(struct.pack('<h',round(math.tanh(v)*26000)) for v in audio))
print(out, len(audio)/RATE, 'seconds')
