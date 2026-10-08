import io
import os
import zipfile
import zlib


def read_7bit(data, pos):
    b0 = data[pos]
    if b0 & 0x80 == 0:
        return b0, pos + 1
    if b0 & 0xC0 == 0x80:
        return ((b0 & 0x3F) << 8) | data[pos + 1], pos + 2
    return ((b0 & 0x1F) << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3], pos + 4


src = os.path.expandvars(r'%USERPROFILE%\Documents\My Games\Terraria\tModLoader\Mods\WastelandSoul.tmod')
data = open(src, 'rb').read()
print('size', len(data), 'magic', data[:4])
pos = 4
fmt, pos = read_7bit(data, pos)
count, pos = read_7bit(data, pos)
print('format', fmt, 'count', count)
for i in range(count):
    ln, pos = read_7bit(data, pos)
    name = data[pos:pos + ln].decode('utf-8')
    pos += ln
    vlen, pos = read_7bit(data, pos)
    ver = data[pos:pos + vlen].decode('utf-8')
    pos += vlen
    size = int.from_bytes(data[pos:pos + 4], 'little', signed=True)
    pos += 4
    print('   %-14s ver=%s size=%d' % (name, ver, size))

body = data[pos:]
print('body offset', pos, 'head', body[:8].hex(), 'tail', body[-8:].hex())
out = r'E:\开发\.tmp-netcheck\tmod_extract'
os.makedirs(out, exist_ok=True)
raw = None
for wbits in (-15, 15, 31, 47):
    try:
        raw = zlib.decompress(body, wbits)
        print('zlib wbits', wbits, 'ok ->', len(raw))
        break
    except Exception as exc:
        print('zlib wbits', wbits, 'fail', exc)
if raw is None:
    raw = body
    print('using raw body')
try:
    zf = zipfile.ZipFile(io.BytesIO(raw))
    zf.extractall(out)
    print('extracted', len(zf.namelist()))
    for n in zf.namelist()[:20]:
        print('   ', n)
except Exception as exc:
    print('zip fail', exc)
    open(os.path.join(out, 'raw.bin'), 'wb').write(raw)
