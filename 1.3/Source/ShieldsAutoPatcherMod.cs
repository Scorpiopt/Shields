using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using VFECore;

namespace ShieldsAutoPatcher
{
    [StaticConstructorOnStartup]
    public static class ShieldsAutoPatcherStartup
    {
        static ShieldsAutoPatcherStartup()
        {
            if (ShieldsAutoPatcherSettings.usableWithShieldsWeapons is null)
            {
                ShieldsAutoPatcherSettings.usableWithShieldsWeapons = new Dictionary<string, bool>();
            }
            ShieldsAutoPatcherSettings.allWeapons = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon && !x.destroyOnDrop).ToList();
            SetValues();
        }

        public static void SetValues()
        {
            foreach (var thingDef in ShieldsAutoPatcherSettings.allWeapons)
            {
                var extension = thingDef.GetModExtension<ThingDefExtension>();
                if (extension != null)
                {
                    if (!ShieldsAutoPatcherSettings.usableWithShieldsWeapons.TryGetValue(thingDef.defName, out var weapon))
                    {
                        ShieldsAutoPatcherSettings.usableWithShieldsWeapons[thingDef.defName] = weapon = extension.usableWithShields;
                    }
                }
                else
                {
                    if (thingDef.IsRangedWeapon)
                    {
                        if (!ShieldsAutoPatcherSettings.usableWithShieldsWeapons.ContainsKey(thingDef.defName))
                        {
                            ShieldsAutoPatcherSettings.usableWithShieldsWeapons[thingDef.defName] = false;
                        }
                    }
                    else if (thingDef.IsMeleeWeapon)
                    {
                        if (!ShieldsAutoPatcherSettings.usableWithShieldsWeapons.ContainsKey(thingDef.defName))
                        {
                            ShieldsAutoPatcherSettings.usableWithShieldsWeapons[thingDef.defName] = true;
                        }
                    }
                }

                if (ShieldsAutoPatcherSettings.usableWithShieldsWeapons.TryGetValue(thingDef.defName, out var usableWithShields))
                {
                    if (usableWithShields)
                    {
                        if (extension is null)
                        {
                            if (thingDef.modExtensions is null)
                            {
                                thingDef.modExtensions = new List<DefModExtension>();
                            }
                            extension = new ThingDefExtension();
                            thingDef.modExtensions.Add(extension);
                        }
                    }
                    if (extension != null)
                    {
                        extension.usableWithShields = usableWithShields;
                    }
                }
            }
        }
    }
    public class ShieldsAutoPatcherMod : Mod
    {
        public static ShieldsAutoPatcherSettings settings;
        public ShieldsAutoPatcherMod(ModContentPack pack) : base(pack)
        {
            settings = GetSettings<ShieldsAutoPatcherSettings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return this.Content.Name;
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            ShieldsAutoPatcherStartup.SetValues();
        }
    }

    public class ShieldsAutoPatcherSettings : ModSettings
    {
        public static Dictionary<string, bool> usableWithShieldsWeapons = new Dictionary<string, bool>();
        public static List<ThingDef> allWeapons = new List<ThingDef>();
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref usableWithShieldsWeapons, "usableWithShieldsWeapons", LookMode.Value, LookMode.Value);
        }

        string searchKey;
        public bool showMeleeWeapons = true;
        public bool showRangedWeapons = true;
        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            var searchLabel = new Rect(rect.x + 5, rect.y, 60, 24);
            Widgets.Label(searchLabel, "Shields.Search".Translate());
            var searchRect = new Rect(searchLabel.xMax + 5, searchLabel.y, 200, 24f);
            searchKey = Widgets.TextField(searchRect, searchKey);
            Text.Anchor = TextAnchor.UpperLeft;

            var showMeleeWeaponsRect = new Rect(searchRect.xMax + 15, searchRect.y, 180, 24);
            Widgets.CheckboxLabeled(showMeleeWeaponsRect, "Shields.ShowMeleeWeapons".Translate(), ref showMeleeWeapons);

            var showRangeWeaponsRect = new Rect(showMeleeWeaponsRect.xMax + 30, searchRect.y, 180, 24);
            Widgets.CheckboxLabeled(showRangeWeaponsRect, "Shields.ShowRangeWeapons".Translate(), ref showRangedWeapons);

            var thingDefs = (searchKey.NullOrEmpty() ? allWeapons : allWeapons.Where(x => x.label.ToLower().Contains(searchKey.ToLower())))
                .Where(x => x.IsRangedWeapon && showRangedWeapons || x.IsMeleeWeapon && showMeleeWeapons).OrderBy(x => x.label).ToList();
            
            var resetRect = new Rect(searchLabel.x, searchLabel.yMax + 5, 265, 24f);
            if (Widgets.ButtonText(resetRect, "Shields.ResetModSettingsToDefault".Translate()))
            {
                usableWithShieldsWeapons.Clear();
                ShieldsAutoPatcherStartup.SetValues();
            }

            var explanationTitleRect = new Rect(resetRect.xMax + 15, resetRect.y, inRect.width - (resetRect.width + 35), 24f);
            Widgets.Label(explanationTitleRect, "Shields.ExplanationTitle".Translate());

            var height = GetScrollHeight(thingDefs);
            Rect outerRect = new Rect(rect.x, searchRect.yMax + 35, rect.width, rect.height - 70);
            Rect viewArea = new Rect(rect.x, outerRect.y, rect.width - 16, height);
            Widgets.BeginScrollView(outerRect, ref scrollPosition, viewArea, true);
            Vector2 outerPos = new Vector2(rect.x + 5, outerRect.y);
            float num = 0;
            var entryHeight = 200;
            foreach (var def in thingDefs)
            {
                bool canDrawGroup = num >= scrollPosition.y - entryHeight && num <= (scrollPosition.y + outerRect.height);
                var curNum = outerPos.y;
                if (canDrawGroup)
                {
                    var iconRect = new Rect(outerPos.x + 5, outerPos.y + 5, 24, 24);
                    Widgets.ThingIcon(iconRect, def);
                    var labelRect = new Rect(iconRect.xMax + 15, outerPos.y + 5, viewArea.width - 80, 24f);
                    Widgets.Label(labelRect, def.LabelCap);
                    var pos = new Vector2(viewArea.width - 40, labelRect.y);
                    var value = usableWithShieldsWeapons[def.defName];
                    Widgets.Checkbox(pos, ref value);
                    usableWithShieldsWeapons[def.defName] = value;
                }
                var innerPos = new Vector2(outerPos.x + 10, outerPos.y);
                outerPos.y += 24;
                num += (outerPos.y - curNum);
            }
            Widgets.EndScrollView();
        }

        private float GetScrollHeight(List<ThingDef> defs)
        {
            float num = 0;
            foreach (var def in defs)
            {
                num += 24;
            }
            return num + 5;
        }
        private static Vector2 scrollPosition = Vector2.zero;
    }
}
