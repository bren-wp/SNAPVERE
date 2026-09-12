from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "store" / "assets"
OUT.mkdir(parents=True, exist_ok=True)
REG = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
BOLD = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"

def f(size, bold=False):
    try:
        return ImageFont.truetype(BOLD if bold else REG, size)
    except OSError:
        return ImageFont.load_default()

def txt(d, xy, s, size, fill, bold=False, anchor=None):
    d.text(xy, s, font=f(size, bold), fill=fill, anchor=anchor)

def logo(size):
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.rounded_rectangle((3, 3, size-3, size-3), radius=int(size*.24), fill="#151126", outline="#5a45b8", width=max(2, size//32))
    pts = [(int(x*size), int(y*size)) for x, y in [(0.24,.80),(.31,.54),(.47,.31),(.78,.17),(.72,.41),(.57,.65),(.24,.83),(.35,.64),(.50,.50),(.66,.35),(.48,.46),(.35,.61)]]
    d.polygon(pts, fill="#8c5cff")
    d.line([(int(.27*size), int(.82*size)), (int(.60*size), int(.49*size))], fill="#f5efff", width=max(2, size//28))
    return im

def scene(mode="popup"):
    w, h = 1280, 800
    im = Image.new("RGBA", (w, h), "#0b0e19"); d = ImageDraw.Draw(im)
    box = (55, 42, 1225, 758)
    d.rounded_rectangle(box, radius=20, fill="#111522", outline="#433a5f")
    d.rounded_rectangle((55,42,1225,100), radius=20, fill="#151928")
    d.rectangle((55,80,1225,100), fill="#151928")
    for i in range(3): d.ellipse((73+i*17,65,83+i*17,75), fill="#454a60")
    d.rounded_rectangle((140,53,1163,89), radius=10, fill="#0d1120", outline="#2e3143")
    txt(d,(154,71),"https://example.local/page",13,"#82889e",anchor="lm")
    im.alpha_composite(logo(32),(1178,55))
    d.rectangle((55,100,1225,758), fill="#f6f7fa")
    txt(d,(121,157),"EXAMPLE WORKSPACE",13,"#7653e6",True)
    txt(d,(121,194),"Capture exactly what matters.",40,"#25283a",True)
    txt(d,(121,247),"SNAPVERE keeps screenshot processing local to your browser.",17,"#5d6376")
    txt(d,(121,275),"Choose the visible area, full page, or an exact selected region.",17,"#5d6376")
    cards=[("Visible area","Capture the current viewport."),("Full page","Scroll and stitch locally."),("Selected region","Drag over an exact area.")]
    for i,(a,b) in enumerate(cards):
        x=121+i*352; d.rounded_rectangle((x,388,x+334,538),radius=18,fill="white",outline="#e6e7ee")
        txt(d,(x+16,416),a,15,"#272a3c",True); txt(d,(x+16,448),b,12,"#777d90")
    if mode == "popup":
        px, py, pw, ph = 805, 119, 363, 481
        sh=Image.new("RGBA",im.size,(0,0,0,0)); sd=ImageDraw.Draw(sh); sd.rounded_rectangle((px+8,py+10,px+pw+12,py+ph+14),18,fill=(0,0,0,105)); sh=sh.filter(ImageFilter.GaussianBlur(16)); im=Image.alpha_composite(im,sh); d=ImageDraw.Draw(im)
        d.rounded_rectangle((px,py,px+pw,py+ph),radius=18,fill="#0e1120",outline="#5b4882")
        im.alpha_composite(logo(32),(px+18,py+18)); txt(d,(px+62,py+23),"SNAPVERE",15,"#f7f4ff",True); txt(d,(px+62,py+44),"Capture. Edit. Done.",10,"#9fa4bd")
        actions=[("Capture visible area","Save the current viewport as PNG"),("Capture full page","Scroll, stitch and save locally"),("Select region","Drag over the exact area you need")]
        y=191
        for title,sub in actions:
            d.rounded_rectangle((px+18,y,px+pw-18,y+76),radius=13,fill="#141628",outline="#493b69")
            d.rounded_rectangle((px+31,y+21,px+54,y+55),radius=6,outline="#b68cff",width=2)
            txt(d,(px+70,y+17),title,12,"#f7f4ff",True); txt(d,(px+70,y+40),sub,9,"#a7adc4")
            y += 85
        txt(d,(px+20,453),"Ready to capture",9,"#c4b3ff")
        d.line((px+18,528,px+pw-18,528),fill="#323446")
        d.rounded_rectangle((px+18,540,px+pw-18,575),radius=10,fill="#141628",outline="#493b69")
        txt(d,(px+pw//2,558),"Settings",10,"#efecfa",anchor="mm")
    elif mode == "region":
        over=Image.new("RGBA",im.size,(0,0,0,0)); od=ImageDraw.Draw(over); od.rectangle((55,100,1225,758),fill=(9,10,19,115)); od.rectangle((270,225,850,510),outline="#b68cff",width=3); im=Image.alpha_composite(im,over); d=ImageDraw.Draw(im)
        d.rounded_rectangle((270,522,575,555),radius=9,fill="#111426"); txt(d,(282,539),"Drag to select an area. Press Esc to cancel.",10,"#f7f4ff",anchor="lm")
    else:
        d.rectangle((55,100,1225,758),fill="#0d1120"); x,y=285,150
        d.rounded_rectangle((x,y,995,640),radius=20,fill="#121426",outline="#493b69")
        im.alpha_composite(logo(36),(x+32,y+30)); txt(d,(x+82,y+36),"SNAPVERE",15,"#f7f4ff",True); txt(d,(x+82,y+58),"Capture settings",10,"#9fa4bd")
        txt(d,(x+32,y+105),"Capture settings",26,"#f7f4ff",True); txt(d,(x+32,y+146),"Configure local filename and download behavior.",12,"#9fa4bd")
        txt(d,(x+32,y+200),"Filename prefix",11,"#f7f4ff",True); d.rounded_rectangle((x+32,y+222,x+678,y+268),11,fill="#0d1120",outline="#4a3c6a"); txt(d,(x+47,y+245),"snapvere",11,"#e9e3ff",anchor="lm")
        d.rounded_rectangle((x+32,y+292,x+52,y+312),5,fill="#8058ff"); txt(d,(x+68,y+302),"Ask where to save visible and region captures",11,"#c5c9da",anchor="lm")
        d.rounded_rectangle((x+32,y+336,x+678,y+382),11,fill="#744ef6"); txt(d,(x+355,y+359),"Save settings",11,"white",True,anchor="mm")
        d.rounded_rectangle((x+32,y+405,x+678,y+463),11,fill="#1c1936",outline="#493b69"); txt(d,(x+48,y+426),"Screenshots are processed locally. No upload, analytics or telemetry.",10,"#b9bfd3")
    return im.convert("RGB")

def save(name, img): img.save(OUT/name, optimize=True)

capture=scene("popup"); region=scene("region"); settings=scene("settings")
save("screenshot-capture-1280x800.png", capture); save("screenshot-region-1280x800.png", region); save("screenshot-settings-1280x800.png", settings)
for name,img in [("opera-screenshot-capture-612x408.png",capture),("opera-screenshot-region-612x408.png",region)]:
    base=Image.new("RGB",(612,408),"white"); resized=img.resize((588,368),Image.Resampling.LANCZOS); base.paste(resized,(12,20)); save(name,base)
small=Image.new("RGBA",(440,280),"#0d1120"); sh=Image.new("RGBA",small.size,(0,0,0,0)); sd=ImageDraw.Draw(sh); sd.ellipse((210,-70,500,220),fill=(140,92,255,75)); sh=sh.filter(ImageFilter.GaussianBlur(28)); small=Image.alpha_composite(small,sh); small.alpha_composite(logo(150),(145,65)); save("promo-small-440x280.png",small.convert("RGB"))
mar=Image.new("RGBA",(1400,560),"#0d1120"); sh=Image.new("RGBA",mar.size,(0,0,0,0)); sd=ImageDraw.Draw(sh); sd.ellipse((730,-180,1400,490),fill=(140,92,255,65)); sh=sh.filter(ImageFilter.GaussianBlur(55)); mar=Image.alpha_composite(mar,sh); mar.alpha_composite(logo(230),(165,165)); d=ImageDraw.Draw(mar); txt(d,(470,205),"SNAPVERE",54,"#f7f4ff",True); txt(d,(470,285),"Local-first screenshot capture",25,"#cfc5e2"); txt(d,(470,328),"Visible area  •  Full page  •  Selected region",18,"#a6abc4"); save("promo-marquee-1400x560.png",mar.convert("RGB"))
print(f"Generated {len(list(OUT.glob('*.png')))} store assets in {OUT}")
