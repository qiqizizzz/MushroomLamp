/*
* ┌──────────────────────────────────┐
* │  描    述: 道具详情浮层展示数据，负责从材料运行时数据提取字段
* │  类    名: ItemTooltipData.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Module.Cook;
using Module.Material;
using Module.Player;
using Module.Recycle;
using Module.Shop;
using UnityEngine;

namespace Module.Item
{
    // 道具详情浮层展示数据，负责从材料运行时数据提取字段
    public class ItemTooltipData
    {
        public const string FIELD_BASIC_SCORE = "BasicScore";
        public const string FIELD_STATE = "State";
        public const string FIELD_COOK_PROGRESS = "CookProgress";
        public const string FIELD_CAN_PROCESS = "CanProcess";
        public const string FIELD_PROCESS_METHOD = "ProcessMethod";
        public const string FIELD_TRIGGER_CONDITION = "TriggerCondition";
        public const string FIELD_EFFECT = "Effect";
        public const string FIELD_MULTIPLIER = "Multiplier";
        public const string FIELD_PROCESS_RESULT = "ProcessResult";
        public const string FIELD_SHOP_CATEGORY = "ShopCategory";
        public const string FIELD_SHOP_RARITY = "ShopRarity";
        public const string FIELD_SHOP_EFFECT = "ShopEffect";
        public const string FIELD_SHOP_TRIGGER = "ShopTrigger";
        public const string FIELD_SHOP_DURATION = "ShopDuration";
        public const string FIELD_SHOP_RESET_RULE = "ShopResetRule";
        public const string FIELD_SHOP_STACKABLE = "ShopStackable";
        public const string FIELD_SHOP_BOX_COUNT = "ShopBoxCount";
        public const string FIELD_SHOP_BOX_PICK_COUNT = "ShopBoxPickCount";
        private const string EMPTY_TEXT = "无";

        public ItemTooltipMode Mode;
        public string Name;
        public string Subtitle;
        public string PriceText;
        public string Desc;
        public string ProcessText;
        public string EffectText;
        public Sprite Icon;
        public readonly List<string> Tags = new();
        public readonly List<ItemTooltipFieldData> Fields = new();

        // 从材料配置构建 Tooltip（无运行时烹饪状态，用于商店/背包预览）
        public static ItemTooltipData FromMaterialConfig(MaterialJsonData config, ItemTooltipMode mode = ItemTooltipMode.Full)
        {
            if (config == null) return null;

            Sprite icon = ArtAssetLoader.LoadSprite(config.iconPath, logOnFail: false);
            CookMaterialData material = new CookMaterialData(0, config, icon);
            return FromMaterial(material, mode);
        }

        // 从烹饪材料构建 Tooltip 展示数据
        public static ItemTooltipData FromMaterial(CookMaterialData material, ItemTooltipMode mode)
        {
            MaterialJsonData config = material?.Config;
            ItemTooltipData data = new ItemTooltipData
            {
                Name = config?.name ?? "未知材料",
                Icon = material?.Icon,
                Subtitle = buildSubtitle(config),
                Desc = config?.desc,
                Mode = mode
            };

            if (mode != ItemTooltipMode.Cook && config != null)
                data.PriceText = $"价格 {config.price}";

            addTags(data, material, config);
            addBasicFields(data, material, config);
            data.ProcessText = string.Empty;
            data.EffectText = string.Empty;
            return data;
        }

        // 从回收材料构建 Tooltip 展示数据
        public static ItemTooltipData FromRecycleOffer(RecycleOfferData offer)
        {
            if (offer == null)
                return null;

            ItemTooltipData data = new ItemTooltipData
            {
                Mode = ItemTooltipMode.Full,
                Name = string.IsNullOrWhiteSpace(offer.name) ? "未知材料" : offer.name,
                Subtitle = string.IsNullOrWhiteSpace(offer.category) ? "可回收材料" : $"可回收材料 / {offer.category}",
                Desc = offer.description,
                PriceText = $"回收 ￥{offer.price}",
                Icon = ArtAssetLoader.LoadSprite(offer.iconPath, logOnFail: false)
            };

            if (!string.IsNullOrWhiteSpace(offer.category))
                data.Tags.Add(offer.category);
            data.Tags.Add("回收");
            data.AddField(FIELD_BASIC_SCORE, "回收收益", $"￥{offer.price}");
            data.AddField(FIELD_STATE, "类别", string.IsNullOrWhiteSpace(offer.category) ? "材料" : offer.category);
            data.AddField(FIELD_EFFECT, "用途", "卖出后立即获得金币");
            return data;
        }

        // 从商店槽位构建 Tooltip 展示数据
        public static ItemTooltipData FromShopSlot(ShopSlotData slotData)
        {
            if (slotData == null)
                return null;

            ItemTooltipData data = new ItemTooltipData
            {
                Mode = ItemTooltipMode.Shop,
                Name = string.IsNullOrWhiteSpace(slotData.name) ? "未知商品" : slotData.name,
                Desc = slotData.description,
                PriceText = $"￥{slotData.price}",
                Icon = ArtAssetLoader.LoadSprite(slotData.iconPath, logOnFail: false)
            };

            if (slotData.isBox)
                addShopBoxFields(data, slotData);
            else
                addShopItemFields(data, slotData);

            return data;
        }

        // 添加字段
        public void AddField(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            Fields.Add(new ItemTooltipFieldData(label, value));
        }

        // 添加带固定字段标识的字段
        public void AddField(string key, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            Fields.Add(new ItemTooltipFieldData(key, label, value));
        }

        // 构建副标题
        private static string buildSubtitle(MaterialJsonData config)
        {
            if (config == null) return string.Empty;

            List<string> parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(config.quality)) parts.Add(config.quality);
            if (!string.IsNullOrWhiteSpace(config.category)) parts.Add(config.category);
            if (!string.IsNullOrWhiteSpace(config.faction)) parts.Add(config.faction);
            return string.Join(" / ", parts);
        }

        // 添加运行时标签与配置标签
        private static void addTags(ItemTooltipData data, CookMaterialData material, MaterialJsonData config)
        {
            if (!string.IsNullOrWhiteSpace(material?.TagText))
            {
                string[] runtimeTags = material.TagText.Split('/');
                for (int i = 0; i < runtimeTags.Length; i++)
                    if (!string.IsNullOrWhiteSpace(runtimeTags[i]) && !data.Tags.Contains(runtimeTags[i]))
                        data.Tags.Add(runtimeTags[i]);
            }

            if (config?.tags == null) return;

            for (int i = 0; i < config.tags.Length; i++)
                if (!string.IsNullOrWhiteSpace(config.tags[i]) && !data.Tags.Contains(config.tags[i]))
                    data.Tags.Add(config.tags[i]);
        }

        // 添加基础状态字段
        private static void addBasicFields(ItemTooltipData data, CookMaterialData material, MaterialJsonData config)
        {
            if (material != null)
            {
                data.AddField(FIELD_BASIC_SCORE, "基础分值", (config?.baseValue ?? material.CurrentValue).ToString());
                data.AddField(FIELD_STATE, "状态", material.IsProcessed ? "已研磨" : string.IsNullOrWhiteSpace(config?.initialState) ? "原始材料" : config.initialState);
                data.AddField(FIELD_COOK_PROGRESS, "熟度", material.CookProgressText);
            }

            if (config == null) return;

            data.AddField(FIELD_CAN_PROCESS, "是否可加工", config.canProcess ? "可加工" : "不可加工");
            data.AddField(FIELD_PROCESS_METHOD, "加工方式", formatOptionalText(config.processMethods));
            data.AddField(FIELD_TRIGGER_CONDITION, "触发条件", formatOptionalText(config.triggerCondition));
            data.AddField(FIELD_EFFECT, "效果", buildEffectSummary(config));
            data.AddField(FIELD_MULTIPLIER, "倍率", formatOptionalText(config.multiplierParam));
            data.AddField(FIELD_PROCESS_RESULT, "加工结果", formatOptionalText(config.processResult));
        }

        // 添加普通商品字段
        private static void addShopItemFields(ItemTooltipData data, ShopSlotData slotData)
        {
            ItemParamJsonData config = ShopCatalog.GetItemConfig(slotData.id);
            if (config == null)
            {
                data.AddField(FIELD_SHOP_CATEGORY, "商品类型", "道具");
                return;
            }

            data.Subtitle = buildShopSubtitle(config);
            data.Desc = string.IsNullOrWhiteSpace(config.description) ? data.Desc : config.description;
            data.AddField(FIELD_SHOP_CATEGORY, "类别", formatOptionalText(config.itemCategory));
            data.AddField(FIELD_SHOP_RARITY, "稀有度", formatRarity(config.rarity));
            data.AddField(FIELD_SHOP_EFFECT, "效果参数", buildShopEffectSummary(config));
            data.AddField(FIELD_SHOP_TRIGGER, "触发方式", formatOptionalText(config.triggerType));
            data.AddField(FIELD_SHOP_DURATION, "持续时间", formatOptionalText(config.durationType));
            data.AddField(FIELD_SHOP_RESET_RULE, "重置规则", formatOptionalText(config.resetRule));
            data.AddField(FIELD_SHOP_STACKABLE, "叠加规则", config.stackable ? "可叠加" : "不可叠加");
        }

        // 添加材料箱商品字段
        private static void addShopBoxFields(ItemTooltipData data, ShopSlotData slotData)
        {
            ShopBoxCatalogEntryJson entry = ShopCatalog.GetShopEntry(slotData.id);
            ShopBoxPoolJsonConfig pool = ShopCatalog.LoadBoxPoolByBoxId(slotData.id);
            int materialCount = pool?.materialIds == null ? 0 : pool.materialIds.Length;

            data.Subtitle = "材料箱";
            if (!string.IsNullOrWhiteSpace(entry?.description))
                data.Desc = entry.description;

            data.AddField(FIELD_SHOP_CATEGORY, "商品类型", "材料箱");
            data.AddField(FIELD_SHOP_BOX_COUNT, "材料池", materialCount > 0 ? $"{materialCount} 种材料" : EMPTY_TEXT);
            if (entry != null && (entry.minMaterialCount > 0 || entry.maxMaterialCount > 0))
                data.AddField(FIELD_SHOP_BOX_PICK_COUNT, "可选数量", buildBoxPickCount(entry));
        }

        // 构建商品副标题
        private static string buildShopSubtitle(ItemParamJsonData config)
        {
            List<string> parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(config.rarity)) parts.Add(formatRarity(config.rarity));
            if (!string.IsNullOrWhiteSpace(config.itemCategory)) parts.Add(config.itemCategory);
            return string.Join(" / ", parts);
        }

        // 构建商品效果字段
        private static string buildShopEffectSummary(ItemParamJsonData config)
        {
            List<string> parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(config.effectType)) parts.Add(config.effectType);
            if (!string.IsNullOrWhiteSpace(config.effectTarget)) parts.Add(config.effectTarget);
            parts.Add(config.effectValue.ToString("0.##"));
            return parts.Count > 0 ? string.Join(" / ", parts) : EMPTY_TEXT;
        }

        // 格式化材料箱抽取数量
        private static string buildBoxPickCount(ShopBoxCatalogEntryJson entry)
        {
            if (entry.minMaterialCount > 0 && entry.maxMaterialCount > 0 && entry.minMaterialCount != entry.maxMaterialCount)
                return $"{entry.minMaterialCount}-{entry.maxMaterialCount}";

            int count = Mathf.Max(entry.minMaterialCount, entry.maxMaterialCount);
            return count > 0 ? count.ToString() : EMPTY_TEXT;
        }

        // 格式化稀有度展示文本
        private static string formatRarity(string rarity)
        {
            if (string.IsNullOrWhiteSpace(rarity)) return EMPTY_TEXT;

            return rarity switch
            {
                "common" => "普通",
                "rare" => "稀有",
                "epic" => "史诗",
                "legendary" => "传说",
                _ => rarity
            };
        }

        // 构建效果变量展示文本
        private static string buildEffectSummary(MaterialJsonData config)
        {
            string effectType = formatOptionalText(config.effectType);
            string effectTarget = formatOptionalText(config.effectTarget);
            string effectParam = formatOptionalText(config.effectParam);

            if (effectType == EMPTY_TEXT && effectTarget == EMPTY_TEXT && effectParam == EMPTY_TEXT)
                return EMPTY_TEXT;

            if (effectParam != EMPTY_TEXT && effectTarget != EMPTY_TEXT)
            {
                if (effectType == "加分" || effectType == "减分")
                    return $"{effectTarget}分数 {effectParam}";

                return $"{effectTarget} {effectParam}";
            }

            if (effectType != EMPTY_TEXT && effectTarget != EMPTY_TEXT)
                return $"{effectTarget} / {effectType}";

            if (effectParam != EMPTY_TEXT)
                return effectParam;

            return effectType;
        }

        // 统一把空值显示为无
        private static string formatOptionalText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? EMPTY_TEXT : value;
        }
    }
}
