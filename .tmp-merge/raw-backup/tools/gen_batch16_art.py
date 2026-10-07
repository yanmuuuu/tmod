"""为本轮新内容生成像素贴图。配乐在 gen_music.py。"""
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MOD = os.path.join(ROOT, "WastelandSoul")


def save(image, relative):
    path = os.path.join(MOD, relative.replace("/", os.sep))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    image.save(path)
    print("art", relative, image.size)


def blank(w, h, color=(0, 0, 0, 0)):
    return Image.new("RGBA", (w, h), color)


def pix(draw, x, y, color):
    draw.point((int(x), int(y)), fill=color)


def blob(draw, cx, cy, rx, ry, color):
    draw.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=color)


def frame_canvas(width, height, frames):
    return blank(width, height * frames), height


def paint_ash_heart():
    width, frame_h, frames = 110, 110, 4
    image, _ = frame_canvas(width, frame_h, frames)
    draw = ImageDraw.Draw(image)
    for i in range(frames):
        y0 = i * frame_h
        bob = (0, -3, 0, 3)[i]
        cx, cy = 55, y0 + 58 + bob
        blob(draw, cx, cy + 8, 28, 22, (40, 18, 12, 255))
        blob(draw, cx, cy, 24, 26, (28, 12, 10, 255))
        # cracks
        draw.line((cx - 10, cy - 16, cx - 2, cy + 4, cx - 14, cy + 18), fill=(255, 140, 40, 255), width=2)
        draw.line((cx + 8, cy - 18, cx + 2, cy, cx + 16, cy + 14), fill=(255, 90, 20, 255), width=2)
        core = 8 + (i % 2) * 2
        blob(draw, cx, cy, core + 4, core + 4, (255, 80, 20, 255))
        blob(draw, cx, cy, core, core, (255, 220, 120, 255))
        # wings
        flap = (-8, 6, 10, -4)[i]
        draw.polygon([(cx - 18, cy), (cx - 46, cy - 16 + flap), (cx - 40, cy + 10), (cx - 16, cy + 8)], fill=(180, 60, 16, 255))
        draw.polygon([(cx + 18, cy), (cx + 46, cy - 10 - flap), (cx + 40, cy + 12), (cx + 16, cy + 8)], fill=(160, 48, 12, 255))
        blob(draw, cx, cy - 30, 6, 4, (255, 160, 60, 255))
    save(image, "Content/NPCs/Bosses/AshHeart/AshHeart.png")
    head = blank(34, 34)
    hd = ImageDraw.Draw(head)
    blob(hd, 17, 18, 12, 13, (32, 14, 10, 255))
    blob(hd, 17, 18, 5, 5, (255, 200, 90, 255))
    save(head, "Content/NPCs/Bosses/AshHeart/AshHeart_Head_Boss.png")


def paint_wisp():
    image, frame_h = frame_canvas(30, 26, 4)
    draw = ImageDraw.Draw(image)
    for i in range(4):
        y0 = i * frame_h
        blob(draw, 15, y0 + 12, 8, 7, (255, 120, 30, 255))
        blob(draw, 15, y0 + 12, 4, 3, (255, 230, 150, 255))
        draw.polygon([(15, y0 + 16), (8, y0 + 24), (22, y0 + 24 - (i % 2))], fill=(200, 70, 16, 255))
    save(image, "Content/NPCs/Bosses/AshHeart/AshWisp.png")


def paint_guardian():
    image, frame_h = frame_canvas(110, 110, 6)
    draw = ImageDraw.Draw(image)
    for i in range(6):
        y0 = i * frame_h
        bob = (0, -2, 0, 2, 1, -1)[i]
        cx, cy = 55, y0 + 62 + bob
        # cape
        draw.polygon([(cx - 16, cy - 10), (cx - 28, cy + 34), (cx + 28, cy + 34), (cx + 16, cy - 10)], fill=(28, 36, 58, 255))
        # body
        draw.rounded_rectangle((cx - 16, cy - 18, cx + 16, cy + 24), 4, fill=(186, 198, 214, 255))
        blob(draw, cx, cy - 2, 7, 8, (120, 190, 255, 255))
        blob(draw, cx, cy - 2, 3, 4, (240, 250, 255, 255))
        # helm
        draw.rounded_rectangle((cx - 12, cy - 40, cx + 12, cy - 16), 3, fill=(150, 164, 184, 255))
        draw.rectangle((cx - 8, cy - 30, cx + 8, cy - 26), fill=(40, 70, 110, 255))
        # arms
        arm = (-6, 4, 8, 2, -4, 6)[i]
        draw.rectangle((cx - 24, cy - 8 + arm, cx - 16, cy + 16), fill=(170, 182, 198, 255))
        draw.rectangle((cx + 16, cy - 4 - arm, cx + 24, cy + 18), fill=(170, 182, 198, 255))
        # legs
        draw.rectangle((cx - 10, cy + 22, cx - 4, cy + 38), fill=(120, 132, 150, 255))
        draw.rectangle((cx + 4, cy + 22, cx + 10, cy + 38), fill=(120, 132, 150, 255))
    save(image, "Content/NPCs/Bosses/FireplaceGuardian/FireplaceGuardian.png")
    head = blank(34, 34)
    hd = ImageDraw.Draw(head)
    hd.rounded_rectangle((6, 4, 28, 30), 3, fill=(160, 174, 194, 255))
    hd.rectangle((10, 14, 24, 18), fill=(40, 80, 120, 255))
    blob(hd, 17, 24, 3, 3, (140, 210, 255, 255))
    save(head, "Content/NPCs/Bosses/FireplaceGuardian/FireplaceGuardian_Head_Boss.png")


def paint_crawler():
    image, frame_h = frame_canvas(40, 30, 4)
    draw = ImageDraw.Draw(image)
    for i in range(4):
        y0 = i * frame_h
        step = (0, 2, 0, -2)[i]
        draw.rounded_rectangle((6, y0 + 8, 34, y0 + 22), 3, fill=(90, 96, 102, 255))
        blob(draw, 28, y0 + 12, 3, 3, (255, 80, 40, 255))
        for leg, x in enumerate((8, 16, 24)):
            dy = step if leg % 2 == i % 2 else -step
            draw.line((x, y0 + 22, x - 2, y0 + 28 + dy), fill=(60, 64, 70, 255), width=2)
    save(image, "Content/NPCs/Wildlife/ScrapCrawler.png")


def paint_moth():
    image, frame_h = frame_canvas(36, 36, 4)
    draw = ImageDraw.Draw(image)
    for i in range(4):
        y0 = i * frame_h
        flap = (8, 2, 8, 14)[i]
        draw.polygon([(18, y0 + 18), (2, y0 + flap), (8, y0 + 28), (18, y0 + 20)], fill=(214, 220, 230, 255))
        draw.polygon([(18, y0 + 18), (34, y0 + flap), (28, y0 + 28), (18, y0 + 20)], fill=(180, 196, 220, 255))
        blob(draw, 18, y0 + 18, 3, 6, (40, 48, 70, 255))
        pix(draw, 16, y0 + 14, (80, 140, 255, 255))
    save(image, "Content/NPCs/Wildlife/IndexMoth.png")


def paint_stalker():
    image, frame_h = frame_canvas(48, 48, 4)
    draw = ImageDraw.Draw(image)
    for i in range(4):
        y0 = i * frame_h
        bob = (0, -2, 0, 2)[i]
        blob(draw, 24, y0 + 28 + bob, 10, 14, (48, 20, 12, 255))
        blob(draw, 24, y0 + 14 + bob, 8, 8, (70, 24, 12, 255))
        blob(draw, 21, y0 + 13 + bob, 2, 2, (255, 160, 40, 255))
        blob(draw, 27, y0 + 13 + bob, 2, 2, (255, 160, 40, 255))
        draw.line((16, y0 + 24, 8, y0 + 36 + bob), fill=(90, 30, 14, 255), width=2)
        draw.line((32, y0 + 24, 40, y0 + 36 - bob), fill=(90, 30, 14, 255), width=2)
    save(image, "Content/NPCs/Wildlife/AshStalker.png")


def paint_warden():
    image, frame_h = frame_canvas(44, 60, 4)
    draw = ImageDraw.Draw(image)
    for i in range(4):
        y0 = i * frame_h
        bob = (0, -2, 0, 2)[i]
        draw.polygon([(22, y0 + 10 + bob), (8, y0 + 28), (14, y0 + 48), (30, y0 + 48), (36, y0 + 28)], fill=(40, 52, 78, 255))
        draw.rectangle((16, y0 + 8 + bob, 28, y0 + 22 + bob), fill=(170, 184, 204, 255))
        blob(draw, 22, y0 + 30, 4, 5, (140, 210, 255, 255))
    save(image, "Content/NPCs/Wildlife/HearthWarden.png")


def icon(relative, painter, size=22):
    image = blank(size, size)
    painter(ImageDraw.Draw(image), size)
    save(image, relative)


def paint_items():
    def chip(d, s):
        d.rounded_rectangle((3, 6, s - 4, s - 7), 2, fill=(40, 160, 90, 255))
        d.rectangle((6, 9, s - 7, 12), fill=(180, 255, 200, 255))

    def seal(d, s):
        blob(d, s // 2, s // 2, 8, 8, (255, 210, 90, 255))
        blob(d, s // 2, s // 2, 3, 3, (255, 255, 220, 255))

    def name(d, s):
        d.polygon([(4, s - 6), (s // 2, 4), (s - 4, s - 6)], fill=(220, 140, 170, 255))

    def charm(d, s, color):
        blob(d, s // 2, s // 2 + 1, 7, 8, color)
        d.rectangle((s // 2 - 2, 3, s // 2 + 2, 8), fill=(230, 210, 120, 255))

    icon("Content/Items/Materials/Chip.png", chip, 18)
    icon("Content/Items/Story/DawnSeal.png", seal, 26)
    icon("Content/Items/Story/UnburnedName.png", name, 26)

    colors = {
        "Scavenger": (140, 146, 150, 255),
        "Archivist": (210, 214, 220, 255),
        "AshHeart": (200, 80, 30, 255),
        "Fireplace": (150, 180, 210, 255),
    }
    classes = ["Warrior", "Mage", "Ranger", "Summoner", "Rogue"]
    for boss, color in colors.items():
        for kind in classes:
            icon(
                "Content/Items/Accessories/%s%sCharm.png" % (boss, kind),
                lambda d, s, c=color: charm(d, s, c),
                24,
            )

    def weapon(d, s, color, kind):
        if kind == "Warrior":
            d.polygon([(6, s - 6), (s - 8, 6), (s - 4, 4), (8, s - 4)], fill=color)
        elif kind == "Mage":
            d.rectangle((s // 2 - 2, 4, s // 2 + 2, s - 4), fill=color)
            blob(d, s // 2, 6, 5, 5, color)
        elif kind == "Ranger":
            d.rectangle((4, s // 2 - 2, s - 4, s // 2 + 2), fill=color)
        elif kind == "Summoner":
            d.rectangle((s // 2 - 1, 8, s // 2 + 1, s - 4), fill=color)
            blob(d, s // 2, 7, 4, 4, color)
        else:
            d.polygon([(4, 8), (s - 6, s // 2), (4, s - 8)], fill=color)

    for boss, color in colors.items():
        for kind in classes:
            icon(
                "Content/Items/Weapons/CLine/%sC%s.png" % (boss, kind),
                lambda d, s, c=color, k=kind: weapon(d, s, c, k),
                32,
            )


def paint_projectiles():
    def orb(d, s):
        blob(d, s // 2, s // 2, 7, 7, (255, 90, 20, 255))
        blob(d, s // 2, s // 2, 3, 3, (255, 230, 140, 255))

    def pool(d, s):
        blob(d, 24, 16, 20, 8, (80, 30, 12, 255))
        blob(d, 24, 14, 10, 4, (255, 120, 30, 180))

    def cinder(d, s):
        d.polygon([(6, 2), (10, 2), (8, 18), (4, 18)], fill=(255, 140, 40, 255))

    def pulse(d, s):
        blob(d, 32, 32, 24, 24, (255, 100, 30, 90))
        blob(d, 32, 32, 10, 10, (255, 220, 140, 160))

    def bolt(d, s):
        d.rectangle((1, 2, 14, 6), fill=(180, 220, 255, 255))

    def shard(d, s):
        d.polygon([(7, 1), (13, 7), (7, 13), (1, 7)], fill=(190, 230, 255, 255))

    def wall(d, s):
        d.rectangle((4, 2, 16, 46), fill=(120, 190, 255, 220))

    def spark(d, s):
        blob(d, 8, 8, 6, 6, (255, 255, 255, 230))
        blob(d, 8, 8, 2, 2, (255, 255, 255, 255))

    def tiny(d, s):
        blob(d, 6, 6, 4, 4, (140, 190, 255, 255))

    icon("Content/Projectiles/AshHeartBoss/AshEmberOrb.png", orb, 18)
    image = blank(48, 24)
    pool(ImageDraw.Draw(image), 48)
    save(image, "Content/Projectiles/AshHeartBoss/AshPool.png")
    icon("Content/Projectiles/AshHeartBoss/AshCinder.png", cinder, 12)
    # cinder canvas is 12 wide but painter uses height 20; redraw properly
    cin = blank(12, 20)
    cinder(ImageDraw.Draw(cin), 12)
    save(cin, "Content/Projectiles/AshHeartBoss/AshCinder.png")
    image = blank(64, 64)
    pulse(ImageDraw.Draw(image), 64)
    save(image, "Content/Projectiles/AshHeartBoss/AshPulse.png")
    image = blank(16, 8)
    bolt(ImageDraw.Draw(image), 16)
    save(image, "Content/Projectiles/FireplaceBoss/HearthBolt.png")
    icon("Content/Projectiles/FireplaceBoss/HearthRingShard.png", shard, 14)
    image = blank(20, 48)
    wall(ImageDraw.Draw(image), 20)
    save(image, "Content/Projectiles/FireplaceBoss/HearthWall.png")
    icon("Content/Projectiles/Vfx/WastelandSpark.png", spark, 16)
    icon("Content/Projectiles/CompanionSpark.png", tiny, 12)


def paint_tiles():
    def gate(d):
        d.rectangle((8, 8, 46, 46), fill=(70, 42, 28, 255))
        d.rectangle((16, 16, 38, 44), fill=(255, 120, 40, 255))
        blob(d, 27, 34, 4, 4, (255, 220, 120, 255))

    def terminal(d):
        d.rectangle((6, 18, 48, 44), fill=(40, 58, 78, 255))
        d.rectangle((12, 10, 42, 24), fill=(20, 30, 48, 255))
        for x in (16, 24, 32):
            blob(d, x, 16, 2, 2, (80, 220, 255, 255))

    def exit_tile(d):
        d.rectangle((10, 8, 44, 46), fill=(70, 78, 90, 255))
        d.polygon([(18, 28), (36, 16), (36, 40)], fill=(200, 220, 235, 255))

    for name, painter in (("FireplaceGate", gate), ("FireplaceTerminal", terminal), ("FireplaceExit", exit_tile)):
        image = blank(54, 48)
        painter(ImageDraw.Draw(image))
        save(image, "Content/Tiles/%s.png" % name)


def main():
    paint_ash_heart()
    paint_wisp()
    paint_guardian()
    paint_crawler()
    paint_moth()
    paint_stalker()
    paint_warden()
    paint_items()
    paint_projectiles()
    paint_tiles()


if __name__ == "__main__":
    main()
