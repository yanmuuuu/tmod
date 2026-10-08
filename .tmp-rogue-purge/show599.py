import io
P = r'E:\开发\tools\sync_cn_translation.py'
lines = io.open(P, encoding='utf-8').read().split('\n')
for i in range(588, 610):
    print(i + 1, '|', lines[i])
