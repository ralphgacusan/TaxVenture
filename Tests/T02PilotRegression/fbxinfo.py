"""Milestone T02: read-only binary FBX reader (stdlib only) used by export_source_meshes.py."""
import struct, zlib, sys, math, collections
def read(path):
    data=open(path,'rb').read()
    assert data[:20]==b'Kaydara FBX Binary  '
    ver=struct.unpack_from('<I',data,23)[0]
    wide=ver>=7500
    def node(off):
        if wide: end,np,pl=struct.unpack_from('<QQQ',data,off); off+=24
        else: end,np,pl=struct.unpack_from('<III',data,off); off+=12
        nl=data[off]; off+=1
        if end==0: return None,off
        name=data[off:off+nl].decode('latin1'); off+=nl
        props=[]
        for _ in range(np):
            t=chr(data[off]); off+=1
            if t in 'YCILFD':
                fmt={'Y':'<h','C':'<?','I':'<i','L':'<q','F':'<f','D':'<d'}[t]
                props.append(struct.unpack_from(fmt,data,off)[0]); off+=struct.calcsize(fmt)
            elif t in 'fdlibc':
                n,enc,cl=struct.unpack_from('<III',data,off); off+=12
                raw=data[off:off+cl]; off+=cl
                if enc==1: raw=zlib.decompress(raw)
                fmt={'f':'f','d':'d','l':'q','i':'i','b':'?','c':'?'}[t]
                props.append(list(struct.unpack('<%d%s'%(n,fmt),raw)))
            elif t in 'SR':
                n=struct.unpack_from('<I',data,off)[0]; off+=4
                props.append(data[off:off+n]); off+=n
        kids=[]
        while off<end:
            k,off=node(off)
            if k is None: break
            kids.append(k)
        return (name,props,kids),end
    off=27; top=[]
    while off<len(data)-160:
        n,off=node(off)
        if n is None: break
        top.append(n)
    return ver,top
def find(n,name): return [k for k in n[2] if k[0]==name]
def nm(p): return p.split(b'\x00\x01')[0].decode('latin1') if isinstance(p,bytes) else p
def analyze(path):
    ver,top=read(path)
    objs=[t for t in top if t[0]=='Objects'][0]
    out={'version':ver,'meshes':[],'bones':[],'materials':[]}
    for o in objs[2]:
        if o[0]=='Model' and nm(o[1][2])=='LimbNode': out['bones'].append(nm(o[1][1]))
        if o[0]=='Material': out['materials'].append(nm(o[1][1]))
        if o[0]!='Geometry' or nm(o[1][2])!='Mesh': continue
        V=find(o,'Vertices')[0][1][0]; P=find(o,'PolygonVertexIndex')[0][1][0]
        polys=[];cur=[]
        for i in P:
            if i<0: cur.append(~i); polys.append(cur); cur=[]
            else: cur.append(i)
        pts=[tuple(V[i*3:i*3+3]) for i in range(len(V)//3)]
        tris=sum(len(p)-2 for p in polys)
        ln=find(o,'LayerElementNormal')
        info={'name':nm(o[1][1]),'controlPoints':len(pts),'polygons':len(polys),'triangles':tris,'quadsOrMore':sum(1 for p in polys if len(p)>3)}
        if ln:
            mapping=find(ln[0],'MappingInformationType')[0][1][0]; ref=find(ln[0],'ReferenceInformationType')[0][1][0]
            N=find(ln[0],'Normals')[0][1][0]
            idx=find(ln[0],'NormalsIndex'); idx=idx[0][1][0] if idx else None
            info['normalMapping']=mapping.decode(); info['normalRef']=ref.decode()
            if mapping==b'ByPolygonVertex':
                def nrm(k):
                    j=idx[k] if idx else k
                    return N[j*3:j*3+3]
                # per control point: compare normals of its polygon-vertices; compare to face normals
                byPoint=collections.defaultdict(list); k=0; facetish=0; total=0
                for poly in polys:
                    a,b,c=[pts[i] for i in poly[:3]]
                    u=[b[i]-a[i] for i in range(3)]; v=[c[i]-a[i] for i in range(3)]
                    fn=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]; l=math.sqrt(sum(x*x for x in fn)) or 1
                    fn=[x/l for x in fn]
                    for i in poly:
                        n=nrm(k); k+=1; byPoint[i].append(n)
                        total+=1
                        if sum(n[j]*fn[j] for j in range(3))>0.9995: facetish+=1
                split=0; maxdev=[]
                for i,ns in byPoint.items():
                    dev=0
                    for n in ns[1:]:
                        d=max(-1,min(1,sum(n[j]*ns[0][j] for j in range(3)))); dev=max(dev,math.degrees(math.acos(d)))
                    if dev>1: split+=1
                    maxdev.append(dev)
                info['polyVertsEqualFaceNormal']=round(facetish/total,3)
                info['pointsWithSplitNormals']=round(split/len(byPoint),3)
                info['splitAngleDeg_p50_p90']=(round(sorted(maxdev)[len(maxdev)//2],1),round(sorted(maxdev)[int(len(maxdev)*.9)],1))
        # dihedral angle distribution of shared edges (by control point index)
        edges=collections.defaultdict(list)
        for pi,poly in enumerate(polys):
            for j in range(len(poly)):
                e=tuple(sorted((poly[j],poly[(j+1)%len(poly)]))); edges[e].append(pi)
        def fnorm(poly):
            a,b,c=[pts[i] for i in poly[:3]]
            u=[b[i]-a[i] for i in range(3)]; v=[c[i]-a[i] for i in range(3)]
            fn=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]; l=math.sqrt(sum(x*x for x in fn)) or 1
            return [x/l for x in fn]
        fns=[fnorm(p) for p in polys]; angs=[]
        for e,ps in edges.items():
            if len(ps)==2:
                d=max(-1,min(1,sum(fns[ps[0]][j]*fns[ps[1]][j] for j in range(3)))); angs.append(math.degrees(math.acos(d)))
        angs.sort()
        if angs:
            info['dihedral_p50_p90_p99']=(round(angs[len(angs)//2],1),round(angs[int(len(angs)*.9)],1),round(angs[int(len(angs)*.99)],1))
            info['edges15to60deg']=round(sum(1 for a in angs if 15<=a<60)/len(angs),3)
            info['openEdges']=sum(1 for ps in edges.values() if len(ps)==1)
        xs=[p[0] for p in pts];ys=[p[1] for p in pts];zs=[p[2] for p in pts]
        info['boundsSize']=(round(max(xs)-min(xs),3),round(max(ys)-min(ys),3),round(max(zs)-min(zs),3))
        uv=find(o,'LayerElementUV'); info['uvLayers']=len(uv)
        out['meshes'].append(info)
    return out
if __name__=='__main__':
    for p in sys.argv[1:]:
        r=analyze(p); print('==',p.split('/')[-1],'FBX',r['version'])
        for m in r['meshes']: print('  mesh',m)
        print('  materials',r['materials'])
        b=r['bones']; print('  bones',len(b),[x for x in b if 'Hand' in x][:40])
