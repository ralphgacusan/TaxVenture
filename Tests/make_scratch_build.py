"""Builds a scratch csproj of the game scripts (Assembly-CSharp) that references
Unity's prebuilt package DLLs in Library/ScriptAssemblies instead of building
the packages (which the plain .NET compiler cannot)."""
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
os.chdir(ROOT)
src = open('Assembly-CSharp.csproj', encoding='utf-8').read()


def ref(m):
    name = os.path.splitext(os.path.basename(m.group(1)))[0]
    dll = os.path.join(ROOT, 'Library', 'ScriptAssemblies', name + '.dll')
    if not os.path.exists(dll):
        return ''
    return '<Reference Include="%s"><HintPath>%s</HintPath></Reference>' % (name, dll)


src = re.sub(r'<ProjectReference Include="([^"]+)"\s*/>', ref, src)
src = re.sub(r'<ProjectReference Include="([^"]+)"\s*>.*?</ProjectReference>', ref, src, flags=re.S)
src = src.replace('Include="Assets', 'Include="' + ROOT.replace('\\', '/') + '/Assets')

# Scripts deleted since Unity last regenerated the csproj.
src = re.sub(r'\s*<Compile Include="([^"]+)" />',
             lambda m: m.group(0) if os.path.exists(m.group(1).replace('&amp;', '&')) else '', src)

# Scripts created since Unity last regenerated the csproj (not imported yet):
# every runtime .cs under Assets outside Editor folders and asmdef folders.
known = set(m.replace('&amp;', '&').lower().replace('\\', '/')
            for m in re.findall(r'<Compile Include="([^"]+)"', src))
extra = []
for base, dirs, names in os.walk(os.path.join(ROOT, 'Assets')):
    if any(n.endswith('.asmdef') for n in names):
        dirs[:] = []
        continue
    if 'Editor' in base.replace('\\', '/').split('/'):
        continue
    for n in names:
        if n.endswith('.cs'):
            path = os.path.join(base, n).replace('\\', '/')
            if path.lower() not in known:
                extra.append(path)
if extra:
    items = ''.join('    <Compile Include="%s" />\n' % p.replace('&', '&amp;') for p in sorted(extra))
    src = src.replace('<ItemGroup>', '<ItemGroup>\n' + items, 1)
    print(len(extra), 'new script(s) not yet imported by Unity added')
os.makedirs(os.path.join(ROOT, 'Temp', 'ScratchBuild'), exist_ok=True)
out = os.path.join(ROOT, 'Temp', 'ScratchBuild', 'Scratch.csproj')
open(out, 'w', encoding='utf-8').write(src)
print(src.count('<Compile'), 'files ->', out)
