using System.Reflection;
using LotsofLoot.Models.Config;
using LotsofLoot.Models.Preset;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace LotsofLoot.Services
{
    [Injectable(InjectionType.Singleton)]
    public class ConfigService(ModHelper modHelper, JsonUtil jsonUtil, ISptLogger<ConfigService> logger)
    {
        public string ModPath { get; init; } = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        public LotsofLootModMetadata ModMetadata { get; init; } = new();
        public LotsofLootConfig LotsofLootConfig { get; private set; } = new();

        public string CurrentlyLoadedPreset { get; private set; } = string.Empty;

        /// <summary>
        /// The current loaded preset
        /// 
        /// We set it to default here, as it isn't used until after SPT's database is loaded in which case it shouldn't be null anymore
        /// </summary>
        public LotsofLootPresetConfig LotsofLootPresetConfig { get; private set; } = default!;

        public string GetConfigPath()
        {
            return Path.Combine(ModPath, "config.json");
        }

        public string GetPresetPath(string preset)
        {
            return Path.Combine(ModPath, "Presets", preset);
        }

        public List<string> GetPresets()
        {
            var presetPath = Path.Combine(ModPath, "Presets");

            if (!Directory.Exists(presetPath))
            {
                return [];
            }

            return Directory.GetFiles(presetPath, "*.json").Select(Path.GetFileNameWithoutExtension).OfType<string>().ToList();
        }

        public async Task LoadAsync()
        {
            string configPath = GetConfigPath();

            LotsofLootConfig? loadedConfig = await jsonUtil.DeserializeFromFileAsync<LotsofLootConfig>(configPath);

            if(loadedConfig is not null)
            {
                LotsofLootConfig = loadedConfig;
            }
            else
            {
                logger.Warning("[Lots of Loot Redux] Could not load config! Using default settings");

                // Write the default config file back, for some reason it's missing
                await WriteConfig();
            }

            LotsofLootPresetConfig? loadedPresetConfig = await LoadPresetConfig(LotsofLootConfig.PresetName);

            if (loadedPresetConfig is not null)
            {
                LotsofLootPresetConfig = loadedPresetConfig;
                logger.Success($"[Lots of Loot Redux] Preset {LotsofLootConfig.PresetName} successfully loaded");
            }
            else
            {
                if (LotsofLootConfig.PresetName != "default")
                {
                    logger.Warning(
                        $"[Lots of Loot Redux] Preset '{LotsofLootConfig.PresetName}' could not be loaded! Attempting to load default preset"
                    );

                    loadedPresetConfig = await LoadPresetConfig("default");

                    if (loadedPresetConfig is null)
                    {
                        throw new InvalidOperationException(
                            $"[Lots of Loot Redux] Failed to load preset '{LotsofLootConfig.PresetName}'." +
                            "Also failed to load the default preset, please re-install this mod as the default preset does not exist anymore!"
                        );
                    }

                    LotsofLootPresetConfig = loadedPresetConfig;
                    logger.Success("[Lots of Loot Redux] Default preset loaded successfully.");

                    // Set the preset back to default, the user might have removed the other preset
                    // Or something else occured, anyway this requires user intervention
                    LotsofLootConfig.PresetName = "default";
                    await WriteConfig();
                }
                else
                {
                    throw new InvalidOperationException(
                        "[Lots of Loot Redux] Failed to load the default preset, please re-install this mod as the default preset does not exist anymore!"
                    );
                }
            }
        }

        public async Task<LotsofLootPresetConfig?> LoadPresetConfig(string preset)
        {
            try
            {
                var loadedPreset = await jsonUtil.DeserializeFromFileAsync<LotsofLootPresetConfig>(GetPresetPath(preset + ".json"));
                CurrentlyLoadedPreset = preset;

                return loadedPreset;
            }
            catch(Exception ex)
            {
                logger.Error($"Failed to load preset '{preset}'", ex);
                return null;
            }
        }

        public async Task WriteConfig()
        {
            await File.WriteAllTextAsync(GetConfigPath(), jsonUtil.Serialize(LotsofLootConfig, true));
        }

        public async Task WritePresetConfig(string preset)
        {
            var presetPath = GetPresetPath(preset + ".json");
            var presetDir = Path.GetDirectoryName(presetPath)!;

            if (!Directory.Exists(presetDir))
            {
                Directory.CreateDirectory(presetDir);
            }

            await File.WriteAllTextAsync(GetPresetPath(preset + ".json"), jsonUtil.Serialize(LotsofLootPresetConfig, true));
        }
    }
}
