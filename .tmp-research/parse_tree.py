import json
import re

path = r"C:\Users\p老师\AppData\Local\Temp\dsh-spill-7mdqlH\session-314482940dd1\0b19399f8d25-web_fetch.txt"
raw = open(path, encoding="utf-8", errors="replace").read()

# The spill has a wrapper; find the first '{' and parse the JSON object
start = raw.find('{"sha"')
obj = json.loads(raw[start:])
tree = obj["tree"]
print("TOTAL PATHS:", len(tree))

keys = ("Models3D", "docs", "doc/", ".md", "wiki", "Example", "CHANGELOG", "Rigs2D/Tml")
for item in tree:
    p = item["path"]
    if any(k.lower() in p.lower() for k in keys):
        print("%-9s %-70s %s" % (item["type"], p, item.get("size", "")))
