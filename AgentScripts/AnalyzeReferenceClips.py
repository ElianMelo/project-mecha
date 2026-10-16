"""Decode user-supplied reference recordings into ordered, numbered visual studies.
Requires PyAV and Pillow. This is media tooling, not a gameplay test runner.
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

# Optional temporary decoder installation used by this workspace; no game dependency.
local_decoder = Path(__file__).resolve().parent.parent / 'Temp' / 'media-python'
if local_decoder.exists():
    sys.path.insert(0, str(local_decoder))
import av
from PIL import Image, ImageDraw


def sheets(folder, destination, rate):
    files = sorted(folder.glob('*.jpg'))
    destination.mkdir(parents=True, exist_ok=True)
    for start in range(0, len(files), 60):
        sheet = Image.new('RGB', (1860, 1920), (12, 15, 20))
        draw = ImageDraw.Draw(sheet)
        for slot, path in enumerate(files[start:start + 60]):
            # Crop the recorded game window, removing the surrounding black desktop.
            with Image.open(path) as source:
                frame = source.crop((170, 92, 798, 450)).resize((310, 177))
            x, y = slot % 6 * 310, slot // 6 * 192
            sheet.paste(frame, (x, y))
            index = int(path.stem)
            draw.text((x + 3, y + 177), f'{index} | {index / rate:.3f}s', fill='white')
        sheet.save(destination / f'{start:06}.jpg', quality=85)


def extract(clip, name, output):
    folder = output / name
    folder.mkdir(parents=True, exist_ok=True)
    records = []
    with av.open(str(clip)) as container:
        stream = container.streams.video[0]
        rate = float(stream.average_rate)
        metadata = {'source': str(clip.resolve()), 'sha256': hashlib.file_digest(clip.open('rb'), 'sha256').hexdigest(),
                    'width': stream.width, 'height': stream.height, 'fps': str(stream.average_rate),
                    'duration': float(stream.duration * stream.time_base)}
        for index, frame in enumerate(container.decode(stream)):
            picture = frame.to_image()
            picture.thumbnail((960, 540))
            picture.save(folder / f'{index:06}.jpg', quality=80)
            records.append({'frame': index, 'time': float(frame.time)})
    metadata['frames'] = len(records)
    (output / f'{name}.json').write_text(json.dumps(records), encoding='utf-8')
    (output / f'{name}-metadata.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
    sheets(folder, output / f'{name}-all', rate)
    print(json.dumps(metadata), flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('movement', type=Path)
    parser.add_argument('combat', type=Path)
    parser.add_argument('--output', type=Path, default=Path('Temp/ReferenceFrames'))
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    extract(args.movement, 'movement', args.output)
    extract(args.combat, 'combat', args.output)


if __name__ == '__main__':
    main()
