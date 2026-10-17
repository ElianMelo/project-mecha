"""Read-only soundtrack energy/rhythm inspection; writes a local report."""
from pathlib import Path
import struct, json
import numpy as np

def read_window(path):
    with path.open('rb') as f:
        f.seek(12)
        fmt = None
        while True:
            tag = f.read(4)
            if not tag:
                raise ValueError('No data chunk')
            size = struct.unpack('<I', f.read(4))[0]
            if tag == b'fmt ':
                raw = f.read(size)
                fmt = struct.unpack_from('<HHIIHH', raw)
            elif tag == b'data':
                encoding, channels, rate, _, align, bits = fmt
                count = min(size // align, rate * 45)
                f.seek(min(size // align // 4, rate * 15) * align, 1)
                raw = f.read(count * align)
                if encoding == 3:
                    values = np.frombuffer(raw, dtype='<f4')
                elif bits == 16:
                    values = np.frombuffer(raw, dtype='<i2').astype(np.float32) / 32768
                elif bits == 24:
                    b = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 3)
                    v = b[:,0].astype(np.int32) | b[:,1].astype(np.int32)<<8 | b[:,2].astype(np.int32)<<16
                    values = ((v ^ 8388608) - 8388608).astype(np.float32) / 8388608
                elif bits == 32:
                    values = np.frombuffer(raw, dtype='<i4').astype(np.float32) / 2147483648
                else:
                    raise ValueError((encoding, bits))
                return values.reshape(-1, channels).mean(axis=1)[::4], rate / 4
            else:
                f.seek(size, 1)
            if size % 2:
                f.seek(1, 1)

paths = list(Path('Assets/8 Sci fi Tracks/Loops').glob('*.wav')) + list(Path('Assets/_Project/Audio/Music').glob('*.wav'))
rows = []
for path in sorted(paths):
    x, rate = read_window(path)
    frames = np.lib.stride_tricks.sliding_window_view(x, 1024)[::256]
    spectrum = np.abs(np.fft.rfft(frames * np.hanning(1024)))
    flux = np.maximum(np.diff(spectrum, axis=0), 0).sum(axis=1)
    centred = flux - flux.mean()
    lo, hi = int(rate/256*60/180), int(rate/256*60/90)
    correlations = [float(np.dot(centred[lag:], centred[:-lag])) for lag in range(lo, hi+1)]
    lag = lo + int(np.argmax(correlations))
    rms = float(np.sqrt(np.mean(x*x)))
    row = dict(track=path.name, rms_db=round(20*np.log10(max(1e-9,rms)),1), estimated_bpm=round(rate/256*60/lag),
               attack_density=round(float(np.mean(flux > flux.mean()+flux.std())),3), spectral_motion=round(float(flux.mean()),2))
    rows.append(row)
Path('Logs/soundtrack-inspection.json').write_text(json.dumps(rows, indent=2))
print(json.dumps(rows, indent=2))
