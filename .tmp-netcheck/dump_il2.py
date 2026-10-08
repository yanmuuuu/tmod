import sys
import struct
import dnfile

DLL = r'E:\steam\steamapps\common\tModLoader\tModLoader.dll'

# ECMA-335 opcode table: code -> (name, operand kind)
# kinds: '', 'i1', 'i2', 'i4', 'i8', 'r4', 'r8', 'tok', 'br', 'switch'
def build():
    t = {}
    def add(code, name, kind):
        t[code] = (name, kind)
    # 0x00-0x0D
    for i, n in enumerate(['nop', 'break', 'ldarg.0', 'ldarg.1', 'ldarg.2', 'ldarg.3',
                           'ldloc.0', 'ldloc.1', 'ldloc.2', 'ldloc.3', 'stloc.0', 'stloc.1',
                           'stloc.2', 'stloc.3']):
        add(i, n, '')
    for i, n in enumerate(['ldarg.s', 'ldarga.s', 'starg.s', 'ldloc.s', 'ldloca.s', 'stloc.s']):
        add(0x0E + i, n, 'i1')
    add(0x14, 'ldnull', '')
    for i, n in enumerate(['ldc.i4.m1', 'ldc.i4.0', 'ldc.i4.1', 'ldc.i4.2', 'ldc.i4.3',
                           'ldc.i4.4', 'ldc.i4.5', 'ldc.i4.6', 'ldc.i4.7', 'ldc.i4.8']):
        add(0x15 + i, n, '')
    add(0x1F, 'ldc.i4.s', 'i1')
    add(0x20, 'ldc.i4', 'i4')
    add(0x21, 'ldc.i8', 'i8')
    add(0x22, 'ldc.r4', 'r4')
    add(0x23, 'ldc.r8', 'r8')
    add(0x25, 'dup', '')
    add(0x26, 'pop', '')
    add(0x27, 'jmp', 'tok')
    add(0x28, 'call', 'tok')
    add(0x29, 'calli', 'tok')
    add(0x2A, 'ret', '')
    for i, n in enumerate(['br.s', 'brfalse.s', 'brtrue.s', 'beq.s', 'bge.s', 'bgt.s', 'ble.s',
                           'blt.s', 'bne.un.s', 'bge.un.s', 'bgt.un.s', 'ble.un.s', 'blt.un.s',
                           'br', 'brfalse', 'brtrue', 'beq', 'bge', 'bgt', 'ble', 'blt',
                           'bne.un', 'bge.un', 'bgt.un', 'ble.un', 'blt.un']):
        code = 0x2B + i
        add(code, n, 'br' if code > 0x37 else 'br')
    # fix: 0x2B-0x37 short (i1), 0x38-0x44 long (i4)
    for code in range(0x2B, 0x38):
        add(code, t[code][0], 'i1br')
    for code in range(0x38, 0x45):
        add(code, t[code][0], 'i4br')
    add(0x45, 'switch', 'switch')
    names46 = ['ldind.i1', 'ldind.u1', 'ldind.i2', 'ldind.u2', 'ldind.i4', 'ldind.u4', 'ldind.i8',
               'ldind.i', 'ldind.r4', 'ldind.r8', 'ldind.ref']
    for i, n in enumerate(names46):
        add(0x46 + i, n, '')
    add(0x51, 'stind.ref', '')
    for i, n in enumerate(['stind.i1', 'stind.i2', 'stind.i4', 'stind.i8', 'stind.r4', 'stind.r8']):
        add(0x52 + i, n, '')
    for i, n in enumerate(['add', 'sub', 'mul', 'div', 'div.un', 'rem', 'rem.un', 'and', 'or',
                           'xor', 'shl', 'shr', 'shr.un', 'neg', 'not']):
        add(0x58 + i, n, '')
    for i, n in enumerate(['conv.i1', 'conv.i2', 'conv.i4', 'conv.i8', 'conv.r4', 'conv.r8',
                           'conv.u4', 'conv.u8']):
        add(0x67 + i, n, '')
    add(0x6F, 'callvirt', 'tok')
    for i, n in enumerate(['cpobj', 'ldobj', 'ldstr', 'newobj', 'castclass', 'isinst']):
        add(0x70 + i, n, 'tok')
    add(0x76, 'conv.r.un', '')
    add(0x79, 'unbox', 'tok')
    add(0x7A, 'throw', '')
    add(0x7B, 'ldfld', 'tok')
    add(0x7C, 'ldflda', 'tok')
    add(0x7D, 'stfld', 'tok')
    add(0x7E, 'ldsfld', 'tok')
    add(0x7F, 'ldsflda', 'tok')
    add(0x80, 'stsfld', 'tok')
    add(0x81, 'stobj', 'tok')
    for i, n in enumerate(['conv.ovf.i1.un', 'conv.ovf.i2.un', 'conv.ovf.i4.un', 'conv.ovf.i8.un',
                           'conv.ovf.u1.un', 'conv.ovf.u2.un', 'conv.ovf.u4.un', 'conv.ovf.u8.un',
                           'conv.ovf.i.un', 'conv.ovf.u.un']):
        add(0x82 + i, n, '')
    add(0x8C, 'box', 'tok')
    add(0x8D, 'newarr', 'tok')
    add(0x8E, 'ldlen', '')
    add(0x8F, 'ldelema', 'tok')
    for i, n in enumerate(['ldelem.i1', 'ldelem.u1', 'ldelem.i2', 'ldelem.u2', 'ldelem.i4',
                           'ldelem.u4', 'ldelem.i8', 'ldelem.i', 'ldelem.r4', 'ldelem.r8',
                           'ldelem.ref']):
        add(0x90 + i, n, '')
    for i, n in enumerate(['stelem.i', 'stelem.i1', 'stelem.i2', 'stelem.i4', 'stelem.i8',
                           'stelem.r4', 'stelem.r8', 'stelem.ref']):
        add(0x9B + i, n, '')
    add(0xA3, 'ldelem', 'tok')
    add(0xA4, 'stelem', 'tok')
    add(0xA5, 'unbox.any', 'tok')
    for i, n in enumerate(['conv.ovf.i1', 'conv.ovf.u1', 'conv.ovf.i2', 'conv.ovf.u2',
                           'conv.ovf.i4', 'conv.ovf.u4', 'conv.ovf.i8', 'conv.ovf.u8']):
        add(0xB3 + i, n, '')
    add(0xC2, 'refanyval', 'tok')
    add(0xC3, 'ckfinite', '')
    add(0xC6, 'mkrefany', 'tok')
    add(0xD0, 'ldtoken', 'tok')
    for i, n in enumerate(['conv.u2', 'conv.u1', 'conv.i']):
        add(0xD1 + i, n, '')
    add(0xD4, 'conv.ovf.i', '')
    add(0xD5, 'conv.ovf.u', '')
    for i, n in enumerate(['add.ovf', 'add.ovf.un', 'mul.ovf', 'mul.ovf.un', 'sub.ovf',
                           'sub.ovf.un']):
        add(0xD6 + i, n, '')
    add(0xDC, 'endfinally', '')
    add(0xDD, 'leave', 'i4br')
    add(0xDE, 'leave.s', 'i1br')
    add(0xDF, 'stind.i', '')
    add(0xE0, 'conv.u', '')
    # 0xFE-prefixed (two-byte opcodes)
    for i, n in enumerate(['arglist', 'ceq', 'cgt', 'cgt.un', 'clt', 'clt.un', 'ldftn',
                           'ldvirtftn']):
        add(0xFE00 + i, n, 'tok' if i >= 6 else '')
    add(0xFE09, 'ldarg', 'i2')
    add(0xFE0A, 'ldarga', 'i2')
    add(0xFE0B, 'starg', 'i2')
    add(0xFE0C, 'ldloc', 'i2')
    add(0xFE0D, 'ldloca', 'i2')
    add(0xFE0E, 'stloc', 'i2')
    add(0xFE0F, 'localloc', '')
    add(0xFE11, 'endfilter', '')
    add(0xFE12, 'unaligned.', 'i1')
    add(0xFE13, 'volatile.', '')
    add(0xFE14, 'tail.', '')
    add(0xFE15, 'initobj', 'tok')
    add(0xFE16, 'constrained.', 'tok')
    add(0xFE17, 'cpblk', '')
    add(0xFE18, 'initblk', '')
    add(0xFE19, 'no.', 'i1')
    add(0xFE1A, 'rethrow', '')
    add(0xFE1C, 'sizeof', 'tok')
    add(0xFE1D, 'refanytype', '')
    add(0xFE1E, 'readonly.', '')
    return t

OPS = build()


def il_body(f, rva):
    off = f.get_offset_from_rva(rva)
    data = f.__data__
    first = data[off]
    if first & 0x3 == 0x2:
        size = first >> 2
        return data[off + 1:off + 1 + size]
    flags = int.from_bytes(data[off:off + 2], 'little')
    hsize = (flags >> 12) & 0xF
    size = int.from_bytes(data[off + 4:off + 8], 'little')
    return data[off + hsize * 4:off + hsize * 4 + size]


def disasm(body):
    pos = 0
    out = []
    while pos < len(body):
        start = pos
        b = body[pos]
        pos += 1
        if b == 0xFE:
            code = 0xFE00 | body[pos]
            pos += 1
        else:
            code = b
        entry = OPS.get(code)
        if entry is None:
            out.append((start, 'unknown_%02X' % code, ''))
            continue
        name, kind = entry
        operand = ''
        if kind == 'i1':
            operand = str(struct.unpack_from('<b', body, pos)[0]); pos += 1
        elif kind == 'i1br':
            dpos = pos
            operand = 'IL_%04X' % (pos + 1 + struct.unpack_from('<b', body, pos)[0]); pos += 1
        elif kind == 'i2':
            operand = str(struct.unpack_from('<h', body, pos)[0]); pos += 2
        elif kind == 'i4':
            operand = str(struct.unpack_from('<i', body, pos)[0]); pos += 4
        elif kind == 'i4br':
            operand = 'IL_%04X' % (pos + 4 + struct.unpack_from('<i', body, pos)[0]); pos += 4
        elif kind == 'i8':
            operand = str(struct.unpack_from('<q', body, pos)[0]); pos += 8
        elif kind == 'r4':
            operand = str(struct.unpack_from('<f', body, pos)[0]); pos += 4
        elif kind == 'r8':
            operand = str(struct.unpack_from('<d', body, pos)[0]); pos += 8
        elif kind == 'tok':
            operand = '0x%08X' % int.from_bytes(body[pos:pos + 4], 'little'); pos += 4
        elif kind == 'switch':
            n = int.from_bytes(body[pos:pos + 4], 'little'); pos += 4
            base = pos + 4 * n
            targs = ['IL_%04X' % (base + struct.unpack_from('<i', body, pos + 4 * i)[0]) for i in range(n)]
            pos = base
            operand = ' '.join(targs)
        out.append((start, name, operand))
    return out


def find_methods(f, type_name, method_name):
    out = []
    for t in f.net.mdtables.TypeDef.rows:
        ns = str(t.TypeNamespace)
        full = (ns + '.' + str(t.TypeName)) if ns else str(t.TypeName)
        if full == type_name or str(t.TypeName) == type_name:
            for m in t.MethodList:
                if m.row is None or str(m.row.Name) != method_name:
                    continue
                out.append(m.row)
    return out


def main():
    f = dnfile.dnPE(DLL)
    for spec in sys.argv[1:]:
        type_name, method_name = spec.split('::')
        for m in find_methods(f, type_name, method_name):
            if m.Rva is None:
                continue
            body = il_body(f, m.Rva)
            print('===== %s::%s  ilsize=%d' % (type_name, method_name, len(body)))
            for start, name, operand in disasm(body):
                print('  IL_%04X: %-16s %s' % (start, name, operand))


main()
