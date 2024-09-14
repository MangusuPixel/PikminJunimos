using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Extensions;

namespace PikminJunimos
{
    internal static class GamePatches
    {
        public static ModConfig Config = ModEntry.Config;
        public static IMonitor Monitor = ModEntry.Instance.Monitor;
        public static List<Texture2D> PikminTextures = ModEntry.Instance.PikminTextures;

        private static Rectangle _lightInteriorRect = new(240, 0, 48, 40);

        internal static bool Apply(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Constructor(typeof(Junimo), new Type[] { typeof(Vector2), typeof(int), typeof(bool) }),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(JunimoPostfix))
                );

                harmony.Patch(
                    original: AccessTools.Constructor(typeof(JunimoHarvester), new Type[] { typeof(GameLocation), typeof(Vector2), typeof(JunimoHut), typeof(int), typeof(Color?) }),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(JunimoHarvesterPostfix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(Junimo), nameof(Junimo.draw), new Type[] { typeof(SpriteBatch), typeof(float) }),
                    prefix: new HarmonyMethod(typeof(GamePatches), nameof(JunimoDrawPrefix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(Junimo), nameof(Junimo.DrawShadow)),
                    prefix: new HarmonyMethod(typeof(GamePatches), nameof(JunimoDrawShadowPrefix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(JunimoHarvester), nameof(JunimoHarvester.draw), new Type[] { typeof(SpriteBatch), typeof(float) }),
                    prefix: new HarmonyMethod(typeof(GamePatches), nameof(JunimoHarvesterDrawPrefix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(JunimoHut), nameof(JunimoHut.draw), new Type[] { typeof(SpriteBatch) }),
                    postfix: new HarmonyMethod(typeof(GamePatches), nameof(JunimoHutDrawPostfix))
                );
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed patching game:\n{ex}", LogLevel.Error);
                return false;
            }
            return true;
        }

        internal static void JunimoPostfix(Junimo __instance)
        {
            try
            {
                __instance.Age = Game1.random.Next(1, 4); // Bud stage
                __instance.Birthday_Day = Game1.random.Next(PikminTextures.Count); // Pikmin variation
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in patch {nameof(JunimoPostfix)}:\n{ex}", LogLevel.Error);
            }
        }

        internal static void JunimoHarvesterPostfix(JunimoHarvester __instance)
        {
            try
            {
                __instance.Age = Game1.random.Next(1, 4);
                __instance.Birthday_Day = Game1.random.Next(PikminTextures.Count);
                __instance.HideShadow = true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in patch {nameof(JunimoHarvesterPostfix)}:\n{ex}", LogLevel.Error);
            }
        }

        internal static bool JunimoDrawPrefix(Junimo __instance, NetFloat ___alpha, NetColor ___bundleColor, SpriteBatch b, float alpha)
        {
            try
            {
                if (__instance.IsInvisible)
                    return false;

                DrawJunimo(b, __instance, ___alpha.Value);

                __instance.Sprite.UpdateSourceRect();

                if (__instance.holdingStar.Value || __instance.holdingBundle.Value)
                {
                    var isBundle = __instance.holdingBundle.Value;
                    var texture = __instance.Sprite.Texture;
                    var pos = Game1.GlobalToLocal(Game1.viewport, __instance.Position + new Vector2(8, -64 * __instance.Scale + (isBundle ? 20 : 4) + __instance.yJumpOffset));
                    var sourceRect = new Rectangle(0, isBundle ? 96 : 109, 16, isBundle ? 13 : 19);
                    var color = (isBundle ? ___bundleColor.Value : Color.White) * ___alpha.Value;
                    var scale = Math.Max(0.2f, __instance.Scale) * 4;
                    var layerDepth = __instance.Position.Y / 10000f + 1E-04f;

                    b.Draw(texture, pos, sourceRect, color, 0, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
                }

                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in patch {nameof(JunimoDrawPrefix)}:\n{ex}", LogLevel.Error);
                return true;
            }
        }

        internal static bool JunimoDrawShadowPrefix(Junimo __instance, NetFloat ___alpha, SpriteBatch b)
        {
            try
            {
                DrawShadow(b, __instance, ___alpha.Value);
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in patch {nameof(JunimoDrawShadowPrefix)}:\n{ex}", LogLevel.Error);
                return true;
            }
        }

        internal static bool JunimoHarvesterDrawPrefix(JunimoHarvester __instance, float ___alpha, SpriteBatch b, float alpha)
        {
            try
            {
                if (___alpha <= 0)
                    return false;

                DrawJunimo(b, __instance, ___alpha);

                if (!__instance.swimming.Value)
                    DrawShadow(b, __instance, ___alpha);

                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in patch {nameof(JunimoHarvesterDrawPrefix)}:\n{ex}", LogLevel.Error);
                return true;
            }
        }

        private static void DrawJunimo(SpriteBatch b, NPC npc, float alpha)
        {
            var texture = PikminTextures[npc.Birthday_Day];
            var shakeOffset = npc.shakeTimer > 0 ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero;
            var pos = npc.getLocalPosition(Game1.viewport) + new Vector2(32, 44 + npc.yJumpOffset - 31 * Config.SpriteScale) + shakeOffset;

            var sourceRect = npc.Sprite.SourceRect.Clone();
            sourceRect.Height = 32;
            sourceRect.Y *= 2;

            var color = Color.White * alpha;
            var origin = new Vector2(32, 0) / 4f;
            var effects = npc.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            var layerDepth = Math.Max(0, npc.drawOnTop ? 0.991f : (npc.StandingPixel.Y + 2) / 10000f);

            b.Draw(texture, pos, sourceRect, color, npc.rotation, origin, Config.SpriteScale, effects, layerDepth);

            sourceRect.X += npc.Age * npc.Sprite.Texture.Width;

            b.Draw(texture, pos, sourceRect, color, npc.rotation, origin, Config.SpriteScale, effects, layerDepth);
        }

        private static void DrawShadow(SpriteBatch b, NPC npc, float alpha)
        {
            var shadowTexture = Game1.shadowTexture;
            var pos = Game1.GlobalToLocal(Game1.viewport, npc.Position + new Vector2(32, 44));
            var sourceRect = new Rectangle?(Game1.shadowTexture.Bounds);
            var color = Color.White * alpha;
            var bounds = Game1.shadowTexture.Bounds;
            var origin = new Vector2(bounds.Center.X, bounds.Center.Y);
            var scale = (4 + npc.yJumpOffset / 40f) * Config.SpriteScale / 4f;
            var layerDepth = Math.Max(0, npc.StandingPixel.Y / 10000f - 1E-06f);

            b.Draw(shadowTexture, pos, sourceRect, color, 0, origin, scale, SpriteEffects.None, layerDepth);
        }

        private static void JunimoHutDrawPostfix(JunimoHut __instance, SpriteBatch b, float ___alpha)
        {
            if (Config.IsOnionTextureEnabled && Config.IsOnionLightEnabled && Game1.timeOfDay >= 2000 && Game1.timeOfDay < 2400 && __instance.wasLit.Value && !__instance.GetParentLocation().IsWinterHere())
            {
                var texture = __instance.texture.Value;
                var pos = Game1.GlobalToLocal(Game1.viewport, new Vector2(__instance.tileX.Value * 64, __instance.tileY.Value * 64 + __instance.tilesHigh.Value * 64 - 128));
                var layerDepth = ((__instance.tileY.Value + __instance.tilesHigh.Value - 1) * 64 + 1) / 10000f - 1E-06f;

                b.Draw(texture, pos, _lightInteriorRect, __instance.color * ___alpha * 0.6f, 0, Vector2.Zero, 4, SpriteEffects.None, layerDepth);
            }
        }
    }
}
