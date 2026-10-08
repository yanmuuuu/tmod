import sys
import dnfile

DLL = r'E:\steam\steamapps\common\tModLoader\tModLoader.dll'

f = dnfile.dnPE(DLL)
needle = sys.argv[1].encode('utf-8') if len(sys.argv) > 1 else b'Read underflow'
data = f.__data__
us = f.net.metadata.streams.get(b'#US')
heap = us.__data__ if us is not None else b''
print('US heap len', len(heap))

found = []
start = 0
while True:
    i = heap.find(needle, start)
    if i < 0:
        break
    # back up to the compressed length prefix
    j = i
    while j > 0 and heap[j - 1] != 0:
        j -= 1
    found.append((i, heap[j:i + 80]))
    start = i + 1
for i, s in found[:20]:
    print(i, s)
