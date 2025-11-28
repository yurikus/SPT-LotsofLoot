using LotsofLoot.Helpers;
using LotsofLoot.Services;
using LotsofLoot.Utilities;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Templates;
using SPTarkov.Server.Core.Servers;

namespace LotsofLoot.OnLoad
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + LotsofLootLoadPriority.LotsofLootPriorityOffset)]
    public class PostDBLoad(
        ModificationHelper modificationHelper,
        LazyLoadHandlerService lazyLoadHandlerService,
        ItemHelper itemHelper,
        DatabaseServer databaseServer,
        ConfigServer configServer,
        ConfigService configService,
        LotsOfLootLogger logger
    ) : IOnLoad
    {
        private readonly LocationConfig _locationConfig = configServer.GetConfig<LocationConfig>();

        public Task OnLoad()
        {
            Templates? databaseTemplates = databaseServer.GetTables().Templates;

            if (databaseTemplates is null || databaseTemplates.Items is null || databaseTemplates.Prices is null)
            {
                logger.Critical("Database templates are null, aborting!");

                return Task.CompletedTask;
            }

            if (configService.LotsofLootPresetConfig.General.RemoveBackpackRestrictions)
            {
                modificationHelper.RemoveBackpackRestrictions();
            }

            foreach ((string map, double multiplier) in configService.LotsofLootPresetConfig.LooseLootMultiplier)
            {
                // When allow loot overlay is disabled, amplify the loose loot ever so slightly so more items spawn in other spawn points.
                if (!configService.LotsofLootPresetConfig.General.AllowLootOverlay)
                {
                    _locationConfig.LooseLootMultiplier[map] = Math.Round(multiplier * 1.5);
                }
                else
                {
                    _locationConfig.LooseLootMultiplier[map] = multiplier;
                }

                _locationConfig.StaticLootMultiplier[map] = configService.LotsofLootPresetConfig.StaticLootMultiplier[map];
                _locationConfig.ContainerRandomisationSettings.Enabled = configService.LotsofLootPresetConfig.General.LootContainersRandom;

                if (logger.IsDebug())
                {
                    logger.Debug($"Loose loot multiplier {map}: {_locationConfig.LooseLootMultiplier[map]}");
                    logger.Debug($"Static loot multiplier {map}: {configService.LotsofLootPresetConfig.StaticLootMultiplier[map]}");
                }
            }

            lazyLoadHandlerService.OnPostDBLoad();

            if (configService.LotsofLootPresetConfig.General.DisableFleaRestrictions)
            {
                foreach ((_, TemplateItem template) in databaseTemplates.Items)
                {
                    if (itemHelper.IsValidItem(template.Id))
                    {
                        template.Properties.CanRequireOnRagfair = true;
                        template.Properties.CanSellOnRagfair = true;
                    }
                }
            }

            foreach ((MongoId itemId, long adjustedPrice) in configService.LotsofLootPresetConfig.General.PriceCorrection)
            {
                databaseTemplates.Prices[itemId] = adjustedPrice;
            }

            return Task.CompletedTask;
        }
    }
}
