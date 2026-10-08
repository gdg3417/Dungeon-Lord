"""Original, deterministic offline stonework and chrome. Requires Pillow; no runtime generator."""
from pathlib import Path
import math, random
from PIL import Image, ImageDraw, ImageFilter

out = Path(__file__).resolve().parents[2] / 'Assets/_Project/UI/ProductionDungeon/Art'
out.mkdir(parents=True, exist_ok=True)
random.seed(71008)

def save(name, im): im.save(out / (name + '.png'))
def stone(name, base, corridor=False):
    im = Image.new('RGBA', (128,128), (*base,255)); d = ImageDraw.Draw(im)
    for y in range(128):
        for x in range(128):
            n=random.randrange(-9,10); light=int(5*math.sin(x/17)+3*math.cos(y/13))
            d.point((x,y),fill=tuple(max(0,min(255,c+n+light)) for c in base)+(255,))
    for row in range(2):
        offset = 0 if corridor else (row%2)*32
        for x in range(-offset,128,64):
            box=(x+2,row*64+2,x+62,row*64+62)
            d.rounded_rectangle(box,3,outline=(30,35,39),width=2)
            d.line((x+5,row*64+4,x+59,row*64+4),fill=(134,133,122,255),width=1)
            d.line((x+5,row*64+6,x+5,row*64+59),fill=(110,114,111,255))
            if random.random()<.6:
                a=x+random.randrange(12,42); b=row*64+8
                d.line((a,b,a+4,b+14,a-2,b+27),fill=(44,48,49),width=1)
    save(name,im)

stone('room-stone-a',(87,91,90)); stone('room-stone-b',(99,96,86)); stone('room-stone-c',(76,85,89))
stone('corridor-stone',(66,72,76),True)
im=Image.new('RGBA',(128,128),(15,23,29,255)); d=ImageDraw.Draw(im)
for i in range(90):
    x,y=random.randrange(128),random.randrange(128); r=random.randrange(3,15)
    d.polygon([(x,y),(x+r,y-3),(x+r+3,y+r),(x+2,y+r+3)],fill=(random.randrange(19,29),random.randrange(28,38),random.randrange(34,43),255))
save('surrounding-rock',im.filter(ImageFilter.GaussianBlur(.5)))
for mask in range(16):
    im=Image.new('RGBA',(128,128)); d=ImageDraw.Draw(im)
    for bit,box in [(1,(0,0,127,10)),(2,(117,0,127,127)),(4,(0,117,127,127)),(8,(0,0,10,127))]:
        if mask&bit:
            d.rectangle(box,fill=(35,40,43,240),outline=(122,119,102,255),width=2)
            if bit in (1,4):
                for x in range(24,128,32): d.line((x,box[1],x+2,box[3]),fill=(10,18,23),width=2)
            else:
                for y in range(24,128,32): d.line((box[0],y,box[2],y+2),fill=(10,18,23),width=2)
    save('boundary-'+str(mask),im)
    im=Image.new('RGBA',(128,128)); d=ImageDraw.Draw(im)
    for bit,line in [(1,(0,3,127,3)),(2,(124,0,124,127)),(4,(0,124,127,124)),(8,(3,0,3,127))]:
        if mask&bit: d.line(line,fill=(255,255,255,255),width=5)
    save('selected-edge-'+str(mask),im)
for name,invalid in [('selection',False),('invalid',True),('grid',False),('anchor',False)]:
    im=Image.new('RGBA',(128,128)); d=ImageDraw.Draw(im)
    if name=='anchor': d.polygon([(64,8),(120,64),(64,120),(8,64)],outline='white',width=9); d.ellipse((54,54,74,74),fill='white')
    elif name=='grid': d.rectangle((0,0,127,127),outline=(160,213,230,55),width=1)
    else:
        d.rounded_rectangle((4,4,123,123),8,outline='white',width=5)
        if invalid: d.line((40,40,88,88),fill=(255,255,255,180),width=6); d.line((88,40,40,88),fill=(255,255,255,180),width=6)
        else:
            for x,y in [(4,4),(106,4),(4,106),(106,106)]: d.rectangle((x,y,x+17,y+17),fill=(255,255,255,200))
    save(name,im)

for name,color in [('panel',(17,26,34)),('action',(32,45,55))]:
    im=Image.new('RGBA',(128,128),(*color,255)); d=ImageDraw.Draw(im)
    for i in range(1000):
        x,y=random.randrange(128),random.randrange(128)
        d.point((x,y),fill=tuple(c+random.randrange(0,8) for c in color)+(255,))
    d.rounded_rectangle((1,1,126,126),9,outline=(121,112,88,255),width=2)
    d.rounded_rectangle((5,5,122,122),6,outline=(40,55,65,255),width=1)
    for x,y in [(10,10),(118,10),(10,118),(118,118)]: d.ellipse((x-2,y-2,x+2,y+2),fill=(153,132,82))
    save(name,im)

for name,color in [('mana',(60,192,240)),('usable',(158,127,238)),('rate',(97,217,207)),('heat',(246,146,57)),('floor',(213,179,106)),('focus',(220,196,137)),('collapse',(219,186,113))]:
    im=Image.new('RGBA',(96,96)); d=ImageDraw.Draw(im)
    if name in ('mana','usable','rate'):
        d.polygon([(48,6),(75,34),(67,76),(48,90),(29,76),(21,34)],fill=(*color,255),outline=(220,235,243),width=3)
        d.polygon([(48,10),(48,84),(27,36)],fill=(color[0]//2,color[1]//2,color[2]//2))
        d.line((48,14,65,37,48,84),fill=(236,249,253),width=3)
        if name=='rate': d.line((14,72,14,37,7,44,14,37,21,44),fill=(223,247,247),width=4)
    elif name=='heat':
        d.polygon([(46,7),(53,36),(66,24),(77,55),(69,79),(46,90),(23,77),(19,56),(35,33)],fill=(*color,255),outline=(255,213,142),width=2)
        d.polygon([(47,40),(59,66),(48,81),(36,66)],fill=(255,224,129))
    elif name=='floor':
        for y,w in [(25,64),(45,52),(65,40)]: d.polygon([(48, y-14),(48+w//2,y),(48,y+14),(48-w//2,y)],fill=(*color,255),outline=(45,55,61),width=2)
    elif name=='focus':
        d.ellipse((17,17,79,79),outline=(*color,255),width=5)
        for a,b in [((48,6),(48,30)),((48,66),(48,90)),((6,48),(30,48)),((66,48),(90,48))]: d.line((a,b),fill=(*color,255),width=5)
        d.ellipse((42,42,54,54),fill=(*color,255))
    else: d.line((18,60,48,32,78,60),fill=(*color,255),width=8)
    save('ui-'+name,im)

# Unmapped traps and loot are category emblems rather than falsely specific option art.
for name in ['trap-fallback','loot-fallback']:
    im=Image.new('RGBA',(128,128)); d=ImageDraw.Draw(im)
    d.ellipse((12,12,116,116),fill=(29,41,49),outline=(190,167,110),width=4)
    if name=='trap-fallback':
        d.polygon([(64,22),(101,96),(27,96)],outline=(230,200,139),width=5); d.line((64,44,64,72),fill=(230,200,139),width=6); d.ellipse((61,81,67,87),fill=(230,200,139))
    else:
        d.polygon([(30,47),(98,47),(94,91),(34,91)],fill=(124,92,49),outline=(237,195,103),width=4)
        d.line((33,47,47,31,82,31,98,47),fill=(237,195,103),width=4); d.rectangle((59,57,69,76),fill=(245,211,129))
    save(name,im)
print('Authored', len(list(out.glob('*.png'))), 'original offline surfaces/icons')
