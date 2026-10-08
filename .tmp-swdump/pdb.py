import re
d = open(r"E:\开发\.tmp-swdump\SubworldLibrary.pdb","rb").read()
strs = set(re.findall(rb"[\x20-\x7e]{5,300}", d))
for s in sorted(strs):
    t = s.decode("ascii","replace")
    if ".cs" in t or "http" in t or "github" in t:
        print(t)
