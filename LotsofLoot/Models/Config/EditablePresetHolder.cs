using LotsofLoot.Models.Preset;

namespace LotsofLoot.Models.Config;

public sealed class EditablePresetHolder(LotsofLootPresetConfig config)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public LotsofLootPresetConfig presetConfig { get; init; } = FastCloner.FastCloner.DeepClone(config) ?? throw new InvalidOperationException("Could not clone config!");
    public HashSet<string> PendingChanges { get; init; } = [];
}
