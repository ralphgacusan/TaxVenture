"""Read-only Unity v22 cached model reader for visibility regression diagnostics. No Unity process or writes to Library. Only selected type-tree objects are decoded."""
from pathlib import Path
import struct
COMMON={0:'AABB',49:'Array',55:'Base',76:'bool',81:'char',106:'data',161:'float',172:'GameObject',222:'int',245:'Matrix4x4f',349:'m_Enabled',374:'m_GameObject',427:'m_Name',434:'m_ObjectHideFlags',519:'m_Type',795:'size',800:'SInt16',814:'SInt64',840:'string',884:'Transform',907:'UInt16',928:'UInt8',934:'unsigned int',981:'vector',997:'Vector3f',1093:'m_CorrespondingSourceObject',1121:'m_PrefabInstance',1138:'m_PrefabAsset'}
class Reader:
 def __init__(self,d,o=0):self.d=d;self.o=o
 def get(self,f):
  v=struct.unpack_from('<'+f,self.d,self.o);self.o+=struct.calcsize('<'+f);return v[0] if len(v)==1 else v
 def align(self):self.o=(self.o+3)//4*4
 def string(self):
  n=self.get('i');v=self.d[self.o:self.o+n].decode(errors='replace');self.o+=n;return v
 def zero(self):
  i=self.d.index(0,self.o);v=self.d[self.o:i].decode();self.o=i+1;return v

def load(path,classes=(1,4,137,43,95,90)):
 d=Path(path).read_bytes(); _,_,version,_,endian,ms,fs,base,_=struct.unpack_from('>IIIIB3xIQQQ',d)
 assert version==22 and endian==0
 r=Reader(d,48);r.zero();r.get('i');assert r.get('?');types=[]
 for _ in range(r.get('i')):
  cid=r.get('i');r.get('?');r.get('h');r.o+=16
  n,b=r.get('ii');nodes=[r.get('HBBIIiiiQ') for i in range(n)];strings=d[r.o:r.o+b];r.o+=b
  def s(i):return COMMON.get(i&0x7fffffff,'@'+str(i&0x7fffffff)) if i>>31 else strings[i:].split(b'\0')[0].decode()
  tree=[];stack=[]
  for v in nodes:
   node={'type':s(v[3]),'name':s(v[4]),'size':v[5],'flags':v[7],'kids':[]}
   while len(stack)>v[1]:stack.pop()
   if stack:stack[-1]['kids'].append(node)
   else:tree.append(node)
   stack.append(node)
  types.append((cid,tree[0]));deps=r.get('i');r.o+=4*deps
 objects=[]
 for _ in range(r.get('i')):
  r.align();fid=r.get('q');off=r.get('q');size=r.get('I');tid=r.get('i');objects.append((fid,base+off,size,tid))
 def read(node,rr):
  t=node['type'];kids=node['kids']

  if t=='string':v=rr.string();rr.align()
  elif t in ('Array','@894'):
   count=rr.get('i');assert 0<=count<=10000000,(count,rr.o)
   v=[read(kids[1],rr) for _ in range(count)]
  elif kids:
   if len(kids)==1 and kids[0]['type']=='Array':v=read(kids[0],rr)
   else:v={k['name']:read(k,rr) for k in kids}
  else:
   fm={'bool':'?','char':'B','float':'f','double':'d','int':'i','unsigned int':'I','UInt8':'B','UInt16':'H','SInt16':'h','SInt64':'q','UInt64':'Q'}
   if t in fm:v=rr.get(fm[t])
   else:v=rr.d[rr.o:rr.o+node['size']];rr.o+=node['size']
  if node['flags']&0x4000:rr.align()
  return v
 out={}
 for fid,off,size,tid in objects:
  cid,tree=types[tid]
  if cid not in classes:
   out[fid]=(cid,None);continue
  rr=Reader(d,off);v=read(tree,rr);assert rr.o==off+size,(cid,rr.o,off+size)
  out[fid]=(cid,v)
 return out

if __name__ == '__main__':
 import sys
 failed = False
 for path in sys.argv[1:]:
  objects = load(path)
  assert objects[919132149155446097][0] == 1, 'model root reference does not resolve'
  skins = [v for c,v in objects.values() if c == 137]
  meshes = [v for c,v in objects.values() if c == 43]
  vertices = sum(m['m_VertexData']['m_VertexCount'] for m in meshes)
  triangles = sum(s['indexCount']//3 for m in meshes for s in m['m_SubMeshes'])
  problems = []
  for skin in skins:
   mesh = objects.get(skin['m_Mesh']['m_PathID'])
   if mesh is None or mesh[0] != 43 or mesh[1]['m_VertexData']['m_VertexCount'] == 0:
    problems.append('empty/missing mesh')
   if not skin['m_Enabled'] or not objects[skin['m_GameObject']['m_PathID']][1]['m_IsActive']:
    problems.append('disabled renderer')
   for bone in skin['m_Bones']:
    if bone['m_FileID'] or bone['m_PathID'] not in objects or objects[bone['m_PathID']][0] != 4:
     problems.append('invalid bone reference')
  animators = [v for c,v in objects.values() if c == 95]
  if not animators or any(objects.get(a['m_Avatar']['m_PathID'], (0,))[0] != 90 for a in animators):
   problems.append('missing avatar reference')
  for animator in animators:
   avatar = objects.get(animator['m_Avatar']['m_PathID'])
   if avatar and avatar[0] == 90:
    human = avatar[1]['m_Avatar']['m_Human']['data']
    if human['m_Scale'] <= 0 or any(i < 0 for i in human['m_HumanBoneIndex'][:22]):
     problems.append('invalid required Humanoid mapping')
  if objects.get(-203655887218126122, (0,))[0] != 74: problems.append('clip reference not found')
  if not vertices or not triangles: problems.append('zero geometry')
  failed |= bool(problems)
  print(f'{path}: {vertices} vertices / {triangles} triangles; {len(skins)} skins; ' + (', '.join(problems) if problems else 'references populated (not rendered acceptance)'))
 sys.exit(1 if failed else 0)
