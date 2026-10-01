import base64, copy, hashlib, json, pathlib, struct, zlib

root = pathlib.Path(r'D:\0-ProCare\radiation-shielding-software\unity\LinacRoomStudio')
out = root / 'work' / 'req56-acceptance'
out.mkdir(exist_ok=True)
template = json.loads((root / 'work/req5/REQ5-color-fixture.json').read_text(encoding='utf-8-sig'))

def png(width, height, color):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind+data)&0xffffffff)
    raw = b''.join(b'\0'+b''.join(bytes(color(x,y)) for x in range(width)) for y in range(height))
    return b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR',struct.pack('>IIBBBBB',width,height,8,6,0,0,0)) + chunk(b'IDAT',zlib.compress(raw)) + chunk(b'IEND',b'')

def rule(key, color, thickness=150):
    rgba = dict(zip(('r','g','b','a'),color))
    return dict(id=key,name=key,classification='Wall',colorSpace='RGB',material='Concrete',enabled=True,priority=0,minAreaPixels=3,tolerance=.01,minLengthPixels=3,height=3,baseElevation=0,thicknessMm=thickness,densityKgM3=2350,target=rgba,display=rgba)

def make(name,width,height,color,rules,x,z,rotation):
    data=png(width,height,color)
    (out/(name+'.png')).write_bytes(data)
    d=copy.deepcopy(template)
    d.update(name=name,items=[],linkWallsToRoom=False,width=4,depth=4,height=3,sourceJson='',sourceProjectionJson='',importSummary='')
    for key in ('generationBatches','wallJunctions'): d[key]=[]
    d.pop('ct',None)
    guide=d['floorPlan']
    guide.update(sourceName=name+'.png',sourcePath='',imageBase64=base64.b64encode(data).decode(),pixelWidth=width,pixelHeight=height,metersPerPixel=.01,widthMeters=width*.01,heightMeters=height*.01,x=x,z=z,rotation=rotation,opacity=.5)
    guide['authoring'].update(id=name,sourceFingerprint=hashlib.sha256(data).hexdigest(),pixelWidth=width,pixelHeight=height,rules=rules,calibration=dict(confirmed=True,method='Manual',unit='m/px',value=.01,metresPerPixel=.01,distanceMetres=1,a=dict(x=0,y=0),b=dict(x=100,y=0)))
    (out/(name+'.json')).write_text(json.dumps(d,indent=2),encoding='utf-8')
    print(name, len(data), width*height, 'pixels',len(rules),'rules')

def network(x,y):
    if ((y in (10,90) and 10<=x<=110) or (x in (10,60,110) and 10<=y<=90)):return (255,0,255,255)
    if x==145 and 10<=y<=100:return (0,255,0,255)
    return (255,255,255,255)
make('REQ5-network',160,120,network,[rule('Network',(1,0,1,1)),rule('Parallel',(0,1,0,1),100)],.237,-.1237,37)
rules=[rule('Network',(1,0,1,1))]+[rule('Unused-'+str(i),(i/32,.2,.4,1)) for i in range(1,32)]
make('REQ5-max4mp32rules',2048,2048,lambda x,y:(255,0,255,255) if ((y in (20,2020) and 20<=x<=2020) or (x in (20,2020) and 20<=y<=2020)) else (255,255,255,255),rules,0,0,0)
