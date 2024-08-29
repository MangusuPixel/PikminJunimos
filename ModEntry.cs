using GenericModConfigMenu;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace PikminJunimos
{
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Public properties
        *********/

        public static ModEntry Instance { get; private set; }
        public static new IModHelper Helper { get; private set; }
        public static ModConfig Config { get; private set; }

        public List<Texture2D> PikminTextures { get; set; } = new();

        /******
         * Public methods
         ******/

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Helper = helper;
            Config = helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            var harmony = new Harmony(ModManifest.UniqueID);
            GamePatches.Apply(harmony);
        }

        /******
         * Private methods
         ******/

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            LoadCustomTextures();
            LoadConfigOptions();
        }

        private void LoadCustomTextures()
        {
            TryLoadTexture("red_pikmin");
            TryLoadTexture("blue_pikmin");
            TryLoadTexture("yellow_pikmin");
            TryLoadTexture("purple_pikmin");
            TryLoadTexture("white_pikmin");
            TryLoadTexture("rock_pikmin");
            TryLoadTexture("pink_pikmin");
            TryLoadTexture("ice_pikmin");
            TryLoadTexture("glow_pikmin");
        }

        private void TryLoadTexture(string textureName)
        {
            if (File.Exists(Path.Combine(Helper.DirectoryPath, "assets", $"{textureName}.png")))
                PikminTextures.Add(Helper.ModContent.Load<Texture2D>($"assets/{textureName}.png"));
            else
                Monitor.Log($"Missing texture 'assets/{textureName}.png'. Skipping.", LogLevel.Warn);
        }

        private void LoadConfigOptions()
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Sprite scale",
                tooltip: () => "Scale multiplier for sprites. Normally 4x but set to 2x by default to fit the smaller junimo sizes.",
                getValue: () => ModConfig.SpriteScale,
                setValue: (value) => ModConfig.SpriteScale = value,
                min: 1,
                max: 4,
                interval: 1
            );
        }
    }
}
