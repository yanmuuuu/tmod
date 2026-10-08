import dnfile

f = dnfile.dnPE(r'E:\steam\steamapps\common\tModLoader\tModLoader.dll')
needle = 'Read underflow'.encode('utf-16-le')
for name, st in f.net.metadata.streams.items():
    d = st.__data__
    print(name, type(st).__name__, len(d))
    i = d.find(needle)
    if i >= 0:
        j = i
        while j > 0 and d[j - 1] != 0:
            j -= 1
        print('   found at heap off', i, repr(d[j:i + 200]))
        # find the string-heap index: 1 + j (user string heap indices are 1-based)
        print('   candidate US index: 0x%X' % (j + 1))
