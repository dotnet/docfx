// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.LearnValidation;

public class AchievementValidator : ValidatorBase
{
    public AchievementValidator(List<LegacyManifestItem> manifestItems, string basePath, LearnValidationLogger logger)
        : base(manifestItems, basePath, logger)
    {
    }

    public override bool Validate(Dictionary<string, IValidateModel> fullItemsDict)
    {
        foreach (var item in Items)
        {
            item.IsValid = true;
        }

        return true;
    }

    protected override void ExtractItems()
    {
        if (ManifestItems == null)
        {
            return;
        }

        Items = ManifestItems.SelectMany(m =>
        {
            var path = Path.Combine(BathPath, m?.Output?.TocOutput?.RelativePath ?? "");
            if (!File.Exists(path))
            {
                path = m?.Output?.MetadataOutput?.LinkToPath ?? "";
            }

            var achievements = JsonConvert.DeserializeObject<List<AchievementValidateModel>>(File.ReadAllText(path)) ?? new();

            achievements.ForEach(achievement => achievement.SourceRelativePath = m?.SourceRelativePath!);

            return achievements;
        }).Cast<IValidateModel>().ToList();
    }
}
