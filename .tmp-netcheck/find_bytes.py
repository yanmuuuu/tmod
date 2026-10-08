import sys
f = open(r'E:\steam\steamapps\common\tModLoader\tModLoader.dll', 'rb')
data = f.read()

for needle in sys.argv[1:]:
    nb = needle.encode('utf-8')
    utf16 = needle.encode('utf-16-le')
    print('---', needle, 'utf8:', data.count(nb), 'utf16:', data.count(utf16))
    start = 0
    n = 0
    while n < 10:
        i = data.find(nb, start)
        if i < 0:
            break
        print('   off=%x ctx=%r' % (i, data[max(0, i - 40):i + 60]))
        start = i + 1
        n += 1
