import html
import re

src = r"E:\开发\.tmp-research\wiki\https___innovault_wiki_en_advanced_models3d_.html"
raw = open(src, encoding="utf-8", errors="replace").read()

# Isolate the <main> content if present
m = re.search(r"<main[\s\S]*?</main>", raw)
body = m.group(0) if m else raw

# Preserve code blocks
body = re.sub(r"<pre[^>]*>", "\n```\n", body)
body = re.sub(r"</pre>", "\n```\n", body)
body = re.sub(r"<code[^>]*>", "`", body)
body = re.sub(r"</code>", "`", body)
body = re.sub(r"<h([1-6])[^>]*>", lambda x: "\n\n" + "#" * int(x.group(1)) + " ", body)
body = re.sub(r"</h[1-6]>", "\n", body)
body = re.sub(r"<li[^>]*>", "\n- ", body)
body = re.sub(r"</p>", "\n\n", body)
body = re.sub(r"<br\s*/?>", "\n", body)
body = re.sub(r"<[^>]+>", "", body)

text = html.unescape(body)
text = re.sub(r"[ \t]+", " ", text)
text = re.sub(r"\n{3,}", "\n\n", text)

out = r"E:\开发\.tmp-research\wiki\models3d_en.txt"
open(out, "w", encoding="utf-8").write(text)
print("chars:", len(text))
print(text[:9000])
