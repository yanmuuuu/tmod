using InnoVault;
using InnoVault.Models3D.Runtime;
using InnoVaultExample.Common;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

//Models3D 演示：Assets/Models 里那份手写的 OBJ 文本模型经 [VaultLoaden] 加载成
//Vault3DModel，握着投影器时由 Model3DRenderer 画一颗在头顶缓慢旋转的水晶。
//
//The Models3D demo: the hand-written OBJ text model under Assets/Models loads
//into a Vault3DModel via [VaultLoaden], and while the projector is held,
//Model3DRenderer draws a crystal slowly spinning above the player's head.
//https://innovault.wiki/cn/advanced/models3d/

namespace InnoVaultExample.Content.Advanced.Models3D
{
    internal static class CrystalModelAssets
    {
        //路径省略扩展名，Model3DLoadenHandle 会按 .obj、.gltf 的顺序探测文件。
        //Vault3DModel 属于 VaultLoaden 的"自定义类型"，靠框架里注册的
        //Model3DLoadenHandle 处理。资源扫描只在客户端跑，专用服务器上这个属性
        //始终是 null；客户端加载失败时拿到的才是 Vault3DModel.Empty——所以
        //使用前 null 和 IsValid 都要检查。写成带 setter 的属性而不是字段，
        //免得编译器对"只被反射赋值"的字段报警。
        //
        //The extension is omitted; Model3DLoadenHandle probes .obj then .gltf.
        //Vault3DModel is one of VaultLoaden's "custom types", handled by the
        //Model3DLoadenHandle registered inside the framework. The asset scan is
        //client-only, so on a dedicated server this property stays null; a load
        //failure on the client yields Vault3DModel.Empty instead - check both
        //null and IsValid before use. A property with a setter is used instead
        //of a field so the compiler does not warn about the value being written
        //by reflection only.
        [VaultLoaden("Assets/Models/LowPolyCrystal")]
        public static Vault3DModel Crystal { get; set; }
    }

    internal class CrystalProjector : ModItem
    {
        public override string Texture => "InnoVaultExample/Assets/Emblem/Seal";

        //常驻实例只建一个，跟随本地玩家。选 RegisterPersistent 而不是每 tick Submit，
        //是因为临时队列每个绘制帧都会清空：144fps 下 60hz 的逻辑帧喂不满绘制帧，
        //Submit 路线会闪烁，常驻实例则每帧都在。
        //
        //One persistent instance, following the local player. RegisterPersistent
        //is used instead of Submit-per-tick because the transient queue is
        //cleared every draw frame: at 144fps a 60hz logic tick cannot feed every
        //draw frame and the Submit route flickers, while a persistent instance
        //is simply always there.
        private static Model3DInstance instance;

        public override void SetDefaults() {
            Item.width = 30;
            Item.height = 30;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 80);
        }

        //由 CrystalProjectorPlayer 每逻辑 tick 调用（已保证客户端 + 本地玩家）。
        //职责只有两件事：第一次持握时创建并注册实例，之后按是否持握切换 Visible。
        //
        //Called once per logic tick by CrystalProjectorPlayer (already guaranteed
        //client-side + local player). It only does two things: create and
        //register the instance on first hold, then toggle Visible afterwards.
        internal static void RefreshInstance(bool holding) {
            Vault3DModel model = CrystalModelAssets.Crystal;
            if (model == null || !model.IsValid) {
                return;
            }

            if (instance == null) {
                if (!holding) {
                    return;
                }
                instance = new Model3DInstance(model) {
                    //AfterPlayers：画在玩家精灵之后，悬浮物不会被玩家身体遮住
                    //
                    //AfterPlayers: drawn after player sprites, so the floating
                    //crystal is never hidden behind the player's body
                    Layer = Model3DLayer.AfterPlayers,
                    //模型本体高 2.8 个单位，缩放是"每单位多少像素"，26 → 约 73px 高
                    //
                    //The model is 2.8 units tall; scale means pixels per unit,
                    //so 26 gives roughly 73px on screen
                    Scale = new Vector3(26f),
                    //纯色 Kd 材质配合 BasicEffect 方向光，棱面明暗立刻可见；
                    //光照参数用 Model3DRenderer.GlobalLighting 的默认值就够了
                    //
                    //Flat Kd materials plus BasicEffect directional lights make
                    //the facets pop; the default Model3DRenderer.GlobalLighting
                    //is good enough here
                    LightingEnabled = true,
                    //变换放在 PreDrawInstance 里刷新而不是逻辑 tick：这个回调
                    //每个渲染帧都会走一次，高刷屏上跟随和自转依旧丝滑
                    //
                    //The transform is refreshed in PreDrawInstance rather than in
                    //the logic tick: this callback runs once per render frame, so
                    //following and spinning stay silky on high-refresh monitors
                    PreDrawInstance = static (in Model3DDrawContext ctx) => {
                        Player owner = Main.LocalPlayer;
                        float time = Main.GlobalTimeWrappedHourly;
                        ctx.Instance.Position = owner.Top + new Vector2(0f, -46f + MathF.Sin(time * 2f) * 5f);
                        ctx.Instance.Rotation = new Vector3(0f, time * 1.1f, 0f);
                    },
                };
                Model3DRenderer.RegisterPersistent(instance);
            }

            instance.Visible = holding;
        }

        public override void Unload() {
            //卸载时注销常驻实例。框架的 Model3DSystem 兜底也会清，但谁注册谁注销
            //是更好的习惯
            //
            //Unregister the persistent instance on unload. Model3DSystem clears
            //leftovers as a safety net, but whoever registers should unregister
            if (instance != null) {
                Model3DRenderer.UnregisterPersistent(instance);
                instance = null;
            }
        }
    }

    internal class CrystalProjectorPlayer : ModPlayer
    {
        //纯客户端视觉：只处理本地玩家，别的客户端看不到你的水晶，也不改任何游戏状态。
        //想让所有人都看到，就为每个持握玩家各建一个实例，思路相同。
        //
        //Client-side visual only: only the local player is handled, other clients
        //never see your crystal and no game state changes. To make it visible to
        //everyone, keep one instance per holding player; the idea is identical.
        public override void PostUpdate() {
            if (Main.dedServ || Player.whoAmI != Main.myPlayer) {
                return;
            }
            bool holding = Player.Alives() && Player.HeldItem.type == ModContent.ItemType<CrystalProjector>();
            CrystalProjector.RefreshInstance(holding);
        }
    }

    internal class CrystalModelEntry : IDemoEntry
    {
        public string Id => "CrystalModel";
        public string Chapter => "Advanced";
        public string WikiSlug => "advanced/models3d";
        public string SourcePath => "Content/Advanced/Models3D/CrystalModel.cs";
        public int DemoItemType => ModContent.ItemType<CrystalProjector>();
        public int SortIndex => 10;
    }
}
