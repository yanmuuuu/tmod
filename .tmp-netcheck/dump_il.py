import sys
import dnfile

DLL = r'E:\steam\steamapps\common\tModLoader\tModLoader.dll'

# opcode -> (name, operand kind)
# kinds: '', 'i1', 'i4', 'i8', 'r4', 'r8', 'tok', 'br', 'switch'
OPS = {
    0x00: ('nop', ''), 0x01: ('break', ''), 0x02: ('ldarg.0', ''), 0x03: ('ldarg.1', ''),
    0x04: ('ldarg.2', ''), 0x05: ('ldarg.3', ''), 0x06: ('ldloc.0', ''), 0x07: ('ldloc.1', ''),
    0x08: ('ldloc.2', ''), 0x09: ('ldloc.3', ''), 0x0A: ('stloc.0', ''), 0x0B: ('stloc.1', ''),
    0x0C: ('stloc.2', ''), 0x0D: ('stloc.3', ''), 0x0E: ('ldarg.s', 'i1'), 0x0F: ('ldarga.s', 'i1'),
    0x10: ('starg.s', 'i1'), 0x11: ('ldloc.s', 'i1'), 0x12: ('ldloca.s', 'i1'), 0x13: ('stloc.s', 'i1'),
    0x14: ('ldnull', ''), 0x15: ('ldc.i4.m1', ''), 0x16: ('ldc.i4.0', ''), 0x17: ('ldc.i4.1', ''),
    0x18: ('ldc.i4.2', ''), 0x19: ('ldc.i4.3', ''), 0x1A: ('ldc.i4.4', ''), 0x1B: ('ldc.i4.5', ''),
    0x1C: ('ldc.i4.6', ''), 0x1D: ('ldc.i4.7', ''), 0x1E: ('ldc.i4.8', ''), 0x1F: ('ldc.i4.s', 'i1'),
    0x20: ('ldc.i4', 'i4'), 0x21: ('ldc.i8', 'i8'), 0x22: ('ldc.r4', 'r4'), 0x23: ('ldc.r8', 'r8'),
    0x25: ('dup', ''), 0x26: ('pop', ''), 0x27: ('jmp', 'tok'), 0x28: ('call', 'tok'),
    0x29: ('calli', 'tok'), 0x2A: ('ret', ''), 0x2B: ('br.s', 'br'), 0x2C: ('brfalse.s', 'br'),
    0x2D: ('brtrue.s', 'br'), 0x2E: ('beq.s', 'br'), 0x2F: ('bge.s', 'br'), 0x30: ('bgt.s', 'br'),
    0x31: ('ble.s', 'br'), 0x32: ('blt.s', 'br'), 0x33: ('bne.un.s', 'br'), 0x34: ('bge.un.s', 'br'),
    0x35: ('bgt.un.s', 'br'), 0x36: ('ble.un.s', 'br'), 0x37: ('blt.un.s', 'br'), 0x38: ('br', 'br'),
    0x39: ('brfalse', 'br'), 0x3A: ('brtrue', 'br'), 0x3B: ('beq', 'br'), 0x3C: ('bge', 'br'),
    0x3D: ('bgt', 'br'), 0x3E: ('ble', 'br'), 0x3F: ('blt', 'br'), 0x40: ('bne.un', 'br'),
    0x41: ('bge.un', 'br'), 0x42: ('bgt.un', 'br'), 0x43: ('ble.un', 'br'), 0x44: ('blt.un', 'br'),
    0x45: ('switch', 'switch'), 0x46: ('ldind.i1', ''), 0x47: ('ldind.u1', ''), 0x48: ('ldind.i2', ''),
    0x49: ('ldind.u2', ''), 0x4A: ('ldind.i4', ''), 0x4B: ('ldind.u4', ''), 0x4C: ('ldind.i8', ''),
    0x4D: ('ldind.i', ''), 0x4E: ('ldind.r4', ''), 0x4F: ('ldind.r8', ''), 0x50: ('ldind.ref', ''),
    0x51: ('stind.ref', ''), 0x52: ('stind.i1', ''), 0x53: ('stind.i2', ''), 0x54: ('stind.i4', ''),
    0x55: ('stind.i8', ''), 0x56: ('stind.r4', ''), 0x57: ('stind.r8', ''), 0x58: ('add', ''),
    0x59: ('sub', ''), 0x5A: ('mul', ''), 0x5B: ('div', ''), 0x5C: ('div.un', ''), 0x5D: ('rem', ''),
    0x5E: ('rem.un', ''), 0x5F: ('and', ''), 0x60: ('or', ''), 0x61: ('xor', ''), 0x62: ('shl', ''),
    0x63: ('shr', ''), 0x64: ('shr.un', ''), 0x65: ('neg', ''), 0x66: ('not', ''), 0x67: ('conv.i1', ''),
    0x68: ('conv.i2', ''), 0x69: ('conv.i4', ''), 0x6A: ('conv.i8', ''), 0x6B: ('conv.r4', ''),
    0x6C: ('conv.r8', ''), 0x6D: ('conv.u4', ''), 0x6E: ('conv.u8', ''), 0x6F: ('callvirt', 'tok'),
    0x70: ('cpobj', 'tok'), 0x71: ('ldobj', 'tok'), 0x72: ('ldstr', 'tok'), 0x73: ('newobj', 'tok'),
    0x74: ('castclass', 'tok'), 0x75: ('isinst', 'tok'), 0x76: ('conv.r.un', ''), 0x79: ('unbox', 'tok'),
    0x7A: ('throw', ''), 0x7B: ('ldfld', 'tok'), 0x7C: ('ldflda', 'tok'), 0x7D: ('stfld', 'tok'),
    0x7E: ('ldsfld', 'tok'), 0x7F: ('ldsflda', 'tok'), 0x80: ('stsfld', 'tok'), 0x81: ('stobj', 'tok'),
    0x82: ('conv.ovf.i1.un', ''), 0x83: ('conv.ovf.i2.un', ''), 0x84: ('conv.ovf.i4.un', ''),
    0x85: ('conv.ovf.i8.un', ''), 0x86: ('conv.ovf.u1.un', ''), 0x87: ('conv.ovf.u2.un', ''),
    0x88: ('conv.ovf.u4.un', ''), 0x89: ('conv.ovf.u8.un', ''), 0x8A: ('conv.ovf.i.un', ''),
    0x8B: ('conv.ovf.u.un', ''), 0x8C: ('box', 'tok'), 0x8D: ('newarr', 'tok'), 0x8E: ('ldlen', ''),
    0x8F: ('ldelema', 'tok'), 0x90: ('ldelem.i1', ''), 0x91: ('ldelem.u1', ''), 0x92: ('ldelem.i2', ''),
    0x93: ('ldelem.u2', ''), 0x94: ('ldelem.i4', ''), 0x95: ('ldelem.u4', ''), 0x96: ('ldelem.i8', ''),
    0x97: ('ldelem.i', ''), 0x98: ('ldelem.r4', ''), 0x99: ('ldelem.r8', ''), 0x9A: ('ldelem.ref', ''),
    0x9B: ('stelem.i', ''), 0x9C: ('stelem.i1', ''), 0x9D: ('stelem.i2', ''), 0x9E: ('stelem.i4', ''),
    0x9F: ('stelem.i8', ''), 0xA0: ('stelem.r4', ''), 0xA1: ('stelem.r8', ''), 0xA2: ('stelem.ref', ''),
    0xA3: ('ldelem', 'tok'), 0xA4: ('stelem', 'tok'), 0xA5: ('unbox.any', 'tok'),
    0xB3: ('conv.ovf.i1', ''), 0xB4: ('conv.ovf.u1', ''), 0xB5: ('conv.ovf.i2', ''),
    0xB6: ('conv.ovf.u2', ''), 0xB7: ('conv.ovf.i4', ''), 0xB8: ('conv.ovf.u4', ''),
    0xB9: ('conv.ovf.i8', ''), 0xBA: ('conv.ovf.u8', ''), 0xC2: ('refanyval', 'tok'),
    0xC3: ('ckfinite', ''), 0xC6: ('mkrefany', 'tok'), 0xD0: ('ldtoken', 'tok'),
    0xD1: ('conv.u2', ''), 0xD2: ('conv.u1', ''), 0xD3: ('conv.i', ''), 0xD4: ('conv.ovf.i', ''),
    0xD5: ('conv.ovf.u', ''), 0xD6: ('add.ovf', ''), 0xD7: ('add.ovf.un', ''), 0xD8: ('mul.ovf', ''),
    0xD9: ('mul.ovf.un', ''), 0xDA: ('sub.ovf', ''), 0xDB: ('sub.ovf.un', ''), 0xDC: ('endfinally', ''),
    0xDD: ('leave', 'br'), 0xDE: ('leave.s', 'br'), 0xDF: ('stind.i', ''), 0xE0: ('conv.u', ''),
    0xFE00: ('arglist', ''), 0xFE01: ('ceq', ''), 0xFE02: ('cgt', ''), 0xFE03: ('cgt.un', ''),
    0xFE04: ('clt', ''), 0xFE05: ('clt.un', ''), 0xFE06: ('ldftn', 'tok'), 0xFE07: ('ldvirtftn', 'tok'),
    0xFE09: ('ldarg', 'i2'), 0xFE0A: ('ldarga', 'i2'), 0xFE0B: ('starg', 'i2'), 0xFE0C: ('ldloc', 'i2'),
    0xFE0D: ('ldloca', 'i2'), 0xFE0E: ('stloc', 'i2'), 0xFE0F: ('localloc', ''),
    0xFE11: ('endfilter', ''), 0xFE12: ('unaligned.', 'i1'), 0xFE13: ('volatile.', ''),
    0xFE14: ('tail.', ''), 0xFE15: ('initobj', 'tok'), 0xFE16: ('constrained.', 'tok'),
    0xFE17: ('cpblk', ''), 0xFE18: ('initblk', ''), 0xFE19: ('no.', 'i1'), 0xFE1A: ('rethrow', ''),
    0xFE1C: ('sizeof', 'tok'), 0xFE1D: ('refanytype', ''), 0xFE1E: ('readonly.', ''),
}

OPNAMES = {name: code for code, (name, _) in OPS.items()}


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


def find_methods(f, type_name, method_name):
    out = []
    for t in f.net.mdtables.TypeDef.rows:
        ns = str(t.TypeNamespace)
        full = ns + '.' + str(t.TypeName) if ns else str(t.TypeName)
        if full == type_name or str(t.TypeName) == type_name:
            for m in t.MethodList:
                if m.row is None or str(m.row.Name) != method_name:
                    continue
                out.append(m.row)
    return out


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
            operand = str(body[pos]); pos += 1
        elif kind == 'i2':
            operand = str(int.from_bytes(body[pos:pos + 2], 'little', signed=True)); pos += 2
        elif kind == 'i4':
            v = int.from_bytes(body[pos:pos + 4], 'little', signed=True)
            operand = str(v); pos += 4
        elif kind == 'i8':
            operand = str(int.from_bytes(body[pos:pos + 8], 'little', signed=True)); pos += 8
        elif kind == 'r4':
            import struct
            operand = str(struct.unpack('<f', body[pos:pos + 4])[0]); pos += 4
        elif kind == 'r8':
            import struct
            operand = str(struct.unpack('<d', body[pos:pos + 8])[0]); pos += 8
        elif kind == 'tok':
            operand = '0x%08X' % int.from_bytes(body[pos:pos + 4], 'little'); pos += 4
        elif kind == 'br':
            operand = 'IL_%04X' % (pos + 4 + int.from_bytes(body[pos:pos + 4], 'little', signed=True)); pos += 4
        elif kind == 'switch':
            n = int.from_bytes(body[pos:pos + 4], 'little'); pos += 4
            base = pos + 4 * n
            targs = []
            for _ in range(n):
                targs.append('IL_%04X' % (base + int.from_bytes(body[pos:pos + 4], 'little', signed=True)))
                pos += 4
            operand = ','.join(targs)
        out.append((start, name, operand))
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
                print('  IL_%04X: %-14s %s' % (start, name, operand))


main()
