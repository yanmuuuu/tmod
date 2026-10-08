import dnfile

DLL = r'E:\steam\steamapps\common\tModLoader\tModLoader.dll'
f = dnfile.dnPE(DLL)
us = f.net.metadata.streams[b'#US'].__data__


def read_us(index):
    # index is the 1-based heap index; strings are 7-bit-encoded length + UTF-16LE
    pos = index
    b0 = us[pos]
    if b0 & 0x80 == 0:
        ln = b0
        pos += 1
    elif b0 & 0xC0 == 0x80:
        ln = ((b0 & 0x3F) << 8) | us[pos + 1]
        pos += 2
    else:
        ln = ((b0 & 0x1F) << 24) | (us[pos + 1] << 16) | (us[pos + 2] << 8) | us[pos + 3]
        pos += 4
    raw = us[pos:pos + ln]
    if ln % 2 == 1 and raw:
        raw = raw[:-1]
    return raw.decode('utf-16-le', 'replace')


for idx in [0x1FAB1, 0x18E8E, 0x1F91D, 0x1FAD0, 0x1FB0C]:
    try:
        print(hex(idx), repr(read_us(idx)))
    except Exception as exc:
        print(hex(idx), 'ERR', exc)

# memberref table dump for tokens we care about
mr = f.net.mdtables.MemberRef
print('memberref count', len(mr.rows))
for want in [0x0A000273, 0x0A000423, 0x0A000564, 0x0A000566, 0x0A0005A8, 0x0A000303]:
    rid = want & 0xFFFFFF
    row = mr.rows[rid - 1]
    cls = row.Class
    print(hex(want), str(row.Name), '| class:', cls.row.TypeNamespace if cls.row else '?', '.', cls.row.TypeName if cls.row else '?')
