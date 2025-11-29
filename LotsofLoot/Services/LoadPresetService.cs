using LotsofLoot.Utilities;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Templates;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace LotsofLoot.Services
{
    [Injectable(InjectionType.Singleton)]
    public sealed class LoadPresetService(DatabaseServer databaseServer, DatabaseService databaseService, ConfigServer configServer, ConfigService configService, ItemHelper itemHelper, LazyLoadHandlerService lazyLoadHandlerService, LotsOfLootLogger logger)
    {
        private readonly LocationConfig _locationConfig = configServer.GetConfig<LocationConfig>();

        public bool LazyLoadingInitialized { get; private set; } = false;

        public Task OnLoad()
        {
            Templates? databaseTemplates = databaseServer.GetTables().Templates;

            if (configService.LotsofLootPresetConfig.General.RemoveBackpackRestrictions)
            {
                RemoveBackpackRestrictions();
            }

            foreach ((string map, double multiplier) in configService.LotsofLootPresetConfig.LooseLootMultiplier)
            {
                _locationConfig.LooseLootMultiplier[map] = multiplier;

                _locationConfig.StaticLootMultiplier[map] = configService.LotsofLootPresetConfig.StaticLootMultiplier[map];
                _locationConfig.ContainerRandomisationSettings.Enabled = configService.LotsofLootPresetConfig.General.LootContainersRandom;

                if (logger.IsDebug())
                {
                    logger.Debug($"Loose loot multiplier {map}: {_locationConfig.LooseLootMultiplier[map]}");
                    logger.Debug($"Static loot multiplier {map}: {configService.LotsofLootPresetConfig.StaticLootMultiplier[map]}");
                }
            }

            // This only needs initialisation once, it will get the current values out of the config service when a raid is loaded
            if (!LazyLoadingInitialized)
            {
                lazyLoadHandlerService.OnPostDBLoad();
                LazyLoadingInitialized = true;
            }

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

        public Task OnReload()
        {
            //Todo: This method needs to undo all the the changes of backpack restirctions, flea, as well as price correction and then re-apply them

            return Task.CompletedTask;
        }

        private void RemoveBackpackRestrictions()
        {
            Dictionary<MongoId, TemplateItem>? items = databaseService.GetTables().Templates.Items;

            foreach ((MongoId _, TemplateItem item) in items)
            {
                // Filter out the 'Slim Field Med Pack' bag that can only contain medical items
                if (item.Id == "5e4abc6786f77406812bd572")
                {
                    continue;
                }

                // If the parent is anything else than the 'Backpack' ( 5448e53e4bdc2d60728b4567)
                if (item.Parent != "5448e53e4bdc2d60728b4567")
                {
                    continue;
                }

                if (item.Properties?.Grids?.Any() == true)
                {
                    foreach (var grid in item.Properties.Grids)
                    {
                        if (grid.Properties?.Filters is null)
                        {
                            continue;
                        }
                        else
                        {
                            grid.Properties.Filters = [];
                        }
                    }
                }
            }
        }
    }
}
