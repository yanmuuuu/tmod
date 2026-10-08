import dnfile

DLL = r'E:\steam\steamapps\common\tModLoader\tModLoader.dll'
f = dnfile.dnPE(DLL)
md = f.net.mdtables

for want in ['Terraria.ModLoader.IO.BinaryIO', 'Terraria.ModLoader.ModNet']:
    for t in md.TypeDef.rows:
        ns = str(t.TypeNamespace)
        full = (ns + '.' + str(t.TypeName)) if ns else str(t.TypeName)
        if full != want:
            continue
        print('=====', full)
        for m in t.MethodList:
            r = m.row
            if r is None:
                continue
            sig = r.Signature
            blob = f.net.metadata.streams[b'#Blob'].__data__
            # resolve signature blob
            off = r.Signature.value if hasattr(r.Signature, 'value') else None
            print('   %-24s rva=%s' % (str(r.Name), hex(r.Rva) if r.Rva else None))
