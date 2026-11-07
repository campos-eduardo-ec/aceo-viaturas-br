"""Original vector decals and ambulance artwork, rendered to game textures.
No native texture is modified or redistributed. SVG sources remain editable.
"""
from pathlib import Path
import base64
import cairosvg

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'art'
ART.mkdir(exist_ok=True)
def svg(w,h,content):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">{content}</svg>'
def text(x,y,t,size=32,color='#fff',rotate=0):
    return f'<text x="{x}" y="{y}" text-anchor="middle" fill="{color}" font-family="Arial" font-weight="bold" font-size="{size}" transform="rotate({rotate} {x} {y})">{t}</text>'
def save(name,w,h,content):
    source=svg(w,h,content)
    (ART/f'{name}.svg').write_text(source,encoding='utf-8')
    cairosvg.svg2png(bytestring=source.encode(),write_to=str(ART/f'{name}.png'))
    return content

# Native police SUV: 1516 x 672, front points right. Decals avoid windows,
# original lightbar at x=280 and windshield x=940..1190.
for name,ink,word in [('pf','#e4b83e','POLÍCIA'),('rf','#073c77','RECEITA')]:
    paint='#111417' if name=='pf' else '#f3f4f4'
    shapes=f'<g fill="{paint}"><rect x="370" y="160" width="470" height="305" rx="28"/><rect x="1210" y="145" width="255" height="380" rx="55"/></g>'
    shapes+=f'<g fill="{ink}"><path d="M420 74H900L860 91H420Z"/><path d="M420 581H860L900 599H420Z"/></g>'
    shapes+=text(590,280,word,66,ink)+text(590,360,'FEDERAL',66,ink)
    logo = ART/'logos'/('pf.svg' if name=='pf' else 'rf.png')
    mime = 'image/svg+xml' if name=='pf' else 'image/png'
    data = logo.read_bytes()
    if name=='pf':
        # This source SVG has absolute dimensions but no viewBox. Declare its
        # original coordinate system so nested SVG layout centers it correctly.
        data=data.replace(b'width="165.16212" height="212.42552"', b'width="165.16212" height="212.42552" viewBox="0 0 165.16212 212.42552"', 1)
    uri = 'data:'+mime+';base64,'+base64.b64encode(data).decode()
    # The hood faces right: rotate each authentic mark without distorting it.
    # A 230px-high mark becomes 230px wide on the hood after the 90 degree turn.
    shapes+=f'<g transform="translate(1337 336) rotate(90)"><image xmlns:xlink="http://www.w3.org/1999/xlink" x="-140" y="-115" width="280" height="230" preserveAspectRatio="xMidYMid meet" xlink:href="{uri}"/></g>'
    shapes+=text(590,429,'BRASIL' if name=='pf' else 'ADUANA',25,ink)
    save(name,1516,672,shapes)

# Fire engine: aligned with the native Windows renderer. Crane and hose stay native.
fire='<g fill="#dfed35" stroke="#4e5528" stroke-width="2"><rect x="50" y="24" width="195" height="29" rx="4"/><rect x="50" y="304" width="195" height="29" rx="4"/><rect x="806" y="160" width="100" height="50" rx="5"/></g>'
fire+='<path d="M55 30H240M55 327H240" stroke="#bd252e" stroke-width="7"/>'
fire+=text(148,47,'BOMBEIROS',19,'#182e33')+text(148,325,'AERÓDROMO',17,'#182e33')+text(856,194,'SCI',31,'#243139')
save('fire',1122,358,fire)

# Standalone generic rescue van, matching the native ambulance bounds and direction.
amb='''<defs>
<linearGradient id="body" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#abb3b7"/><stop offset=".15" stop-color="#fbfcfc"/><stop offset=".5" stop-color="#fff"/><stop offset=".85" stop-color="#e4e8e9"/><stop offset="1" stop-color="#a8b0b5"/></linearGradient>
<linearGradient id="glass"><stop stop-color="#152a37"/><stop offset=".5" stop-color="#3b5464"/><stop offset="1" stop-color="#1a2f3c"/></linearGradient></defs>
<g fill="#171d22"><rect x="265" y="16" width="230" height="70" rx="20"/><rect x="265" y="608" width="230" height="70" rx="20"/><rect x="1430" y="16" width="230" height="70" rx="20"/><rect x="1430" y="608" width="230" height="70" rx="20"/></g>
<path d="M60 30Q14 30 14 110V584Q14 664 60 664H1130L1210 620H1700Q1875 610 1880 475V220Q1875 84 1700 74H1210L1130 30Z" fill="url(#body)" stroke="#6a7781" stroke-width="6"/>
<path d="M80 65H1130M80 629H1130" stroke="#c73137" stroke-width="35"/>
<path d="M1390 100Q1455 347 1390 594L1590 594Q1670 347 1590 100Z" fill="url(#glass)" stroke="#131f2b" stroke-width="9"/>
<path d="M1205 75L1365 99L1360 146H1215Z M1205 620L1365 595L1360 548H1215Z" fill="#274555" stroke="#151e25" stroke-width="6"/>
<path d="M1220 76L1320 15L1350 19L1315 76 M1220 619L1320 679L1350 675L1315 619" fill="#202d36"/>
<g stroke="#c2c9cc" stroke-width="5"><path d="M100 180H1080M100 250H1080M100 445H1080M100 515H1080"/><rect x="140" y="120" width="930" height="454" rx="24" fill="none"/></g>
<rect x="1143" y="109" width="23" height="476" rx="8" fill="#34414a"/>
<g fill="#cb252d" stroke="#fe8080" stroke-width="3"><rect x="1147" y="115" width="16" height="142" rx="4"/><rect x="1147" y="437" width="16" height="142" rx="4"/><rect x="1226" y="136" width="130" height="38" rx="12"/><rect x="1226" y="520" width="130" height="38" rx="12"/></g>
<g fill="#f8edd3" stroke="#72858a" stroke-width="3"><path d="M1750 95L1845 170L1858 210L1760 144Z"/><path d="M1750 599L1845 524L1858 484L1760 550Z"/></g>
<path d="M1864 248V446" stroke="#35404a" stroke-width="15"/>
<g transform="translate(420 347)" fill="#225a91"><rect x="-25" y="-100" width="50" height="200"/><rect x="-25" y="-100" width="50" height="200" transform="rotate(60)"/><rect x="-25" y="-100" width="50" height="200" transform="rotate(120)"/><path d="M0 -66V66M-13 -45Q40 -24 0 0Q-35 20 13 44" fill="none" stroke="#fff" stroke-width="9"/></g>
'''
amb+=text(800,325,'RESGATE',73,'#a92931')+text(800,398,'AEROPORTUÁRIO',35,'#2c485c')+text(1710,347,'AMBULÂNCIA',34,'#b12c35',90)
save('ambulance',1893,694,amb)

# Cover authored as vector layout; miniature silhouettes are original artwork.
cover='''<rect width="1024" height="1024" fill="#102c3c"/><path d="M0 0H1024V14H0Z" fill="#ebc640"/>
<circle cx="1020" cy="40" r="460" fill="#17384a"/><path d="M70 920H954" stroke="#52717e"/>
'''
cover+=text(512,90,'AEROPORTOS BR',33,'#e8ca57')+text(512,175,'VIATURAS',85)+text(512,235,'E EMERGÊNCIAS',49,'#d9e8ec')
for x,y,label,body,ink in [(60,290,'POLÍCIA FEDERAL','#20272e','#edc64e'),(530,290,'RECEITA FEDERAL','#e3e9eb','#184a83'),(60,570,'BOMBEIROS','#d6e632','#263c48'),(530,570,'AMBULÂNCIA','#f7f9fa','#b92a34')]:
    cover+=f'<rect x="{x}" y="{y}" width="434" height="252" rx="18" fill="#224453"/>'
    cover+=f'<g transform="translate({x+34} {y+48})"><rect x="12" y="14" width="340" height="98" rx="24" fill="{body}" stroke="#83999f" stroke-width="2"/><path d="M265 24H304L320 63L304 102H265Q283 63 265 24" fill="#385364"/><rect x="248" y="30" width="9" height="65" fill="#d9413e"/>'
    cover+=text(148,75,'PF' if 'POLÍCIA' in label else 'RFB' if 'RECEITA' in label else 'SCI' if label=='BOMBEIROS' else 'RESGATE',35,ink)+'</g>'
    cover+=text(x+217,y+209,label,25)
cover+=text(512,883,'4 PINTURAS • VEÍCULOS DO JOGO',27,'#d9e8ec')+text(512,973,'AIRPORT CEO  /  MOD LOADER',26,'#a8c1cc')
save('Icon',1024,1024,cover)
print('Created 5 SVG sources and 5 PNG textures.')
