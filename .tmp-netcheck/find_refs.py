import dnfile

DLL = r'E:\steam\steamapps\common\tModLoader\tModLoader.dll'
US_INDEX = 0x1FAB1  # from heap scan (candidate 0x1FAB2 - 1)

f = dnfile.dnPE(DLL)
data = f.__data__

# find token bytes 0x70 | index ; little-endian 4-byte operand after 0x72 (ldstr)
operand = (0x70000000 | US_INDEX).to_bytes(4, 'little')
hits = []
start = 0
while True:
    i = data.find(b'\x72' + operand, start)
    if i < 0:
        break
    hits.append(i)
    start = i + 1
print('ldstr hits:', [hex(h) for h in hits])

# map file offset -> rva
def off_to_rva(off):
    for sec in f.sections:
        begin = sec.PointerToRawData
        end = begin + sec.SizeOfRawData
        if begin <= off < end:
            return sec.VirtualAddress + (off - begin)
    return None

# build method body ranges: rva -> (typedef, method)
bodies = []
for t in f.net.mdtables.TypeDef.rows:
    ns = str(t.TypeNamespace)
    tname = (ns + '.' + str(t.TypeName)) if ns else str(t.TypeName)
    for m in t.MethodList:
        if m.row is None or m.row.Rva is None:
            continue
        bodies.append((m.row.Rva, tname, str(m.row.Name)))

for h in hits:
    rva = off_to_rva(h)
    owner = None
    for mrva, tname, mname in bodies:
        if mrva <= rva and (owner is None or mrva > owner[0]):
            owner = (mrva, tname, mname)
    print('hit %s rva=%s owner=%s' % (hex(h), hex(rva) if rva else None, owner))
