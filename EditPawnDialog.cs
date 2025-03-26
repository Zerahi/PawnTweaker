using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnTweaker {
    public class EditPawnDialog : Window {
        private readonly PawnTweakData PawnTweak;
        private string apparelMoneyMinStr;
        private string apparelMoneyMaxStr;
        private string weaponMoneyMinStr;
        private string weaponMoneyMaxStr;
        private string techHediffsMoneyMinStr;
        private string techHediffsMoneyMaxStr;
        private string techHediffsChanceStr;
        private HashSet<string> selectedApparelTags;
        private HashSet<string> selectedWeaponTags;
        private HashSet<string> selectedTechHediffsTags;
        private string apparelMultStr = "1";
        private string weaponMultStr = "1";
        private string techMoneyMultStr = "1";
        private string chanceMultStr = "1";
        private Vector2 apparelScrollPosition;
        private Vector2 weaponScrollPosition;
        private Vector2 techHediffsScrollPosition;

        public EditPawnDialog(PawnTweakData tweak) {
            PawnTweak = tweak;
            doCloseX = true;
            closeOnClickedOutside = true;

            var apparelMoney = PawnTweak.ApparelMoney;
            apparelMoneyMinStr = apparelMoney.min.ToString("F0");
            apparelMoneyMaxStr = apparelMoney.max.ToString("F0");

            var weaponMoney = PawnTweak.WeaponMoney;
            weaponMoneyMinStr = weaponMoney.min.ToString("F0");
            weaponMoneyMaxStr = weaponMoney.max.ToString("F0");

            var techHediffsMoney = PawnTweak.TechHediffsMoney;
            techHediffsMoneyMinStr = techHediffsMoney.min.ToString("F0");
            techHediffsMoneyMaxStr = techHediffsMoney.max.ToString("F0");

            var techHediffsChance = PawnTweak.TechHediffsChance;
            techHediffsChanceStr = techHediffsChance.ToString("F2");

            selectedApparelTags = new HashSet<string>(PawnTweak.ApparelTags);
            selectedWeaponTags = new HashSet<string>(PawnTweak.WeaponTags);
            selectedTechHediffsTags = new HashSet<string>(PawnTweak.TechHediffsTags);
        }

        public override Vector2 InitialSize => new Vector2(1080f, 700f);

        public override void DoWindowContents(Rect inRect) {
            float padding = 5f;
            float topPadding = 15f; // Adjusted top gap as requested
            float horizontalSpacing = 10f;
            float verticalSpacing = 10f;
            float buttonHeight = 30f;
            float controlBoxHeight = 50f;

            float currentY = inRect.y;

            // Top buttons (unchanged)
            Rect buttonRect = new Rect(inRect.x, currentY, inRect.width, buttonHeight);
            DrawTopButtons(buttonRect);
            currentY += buttonHeight + verticalSpacing + topPadding; // Only top gap adjusted

            // 2x2 Control boxes
            float controlBoxWidth = (inRect.width - horizontalSpacing) / 2;

            // First row: Apparel Money and Weapon Money
            Rect appMoneyRect = new Rect(inRect.x, currentY, controlBoxWidth, controlBoxHeight);
            DrawControlBox(appMoneyRect, "Apparel Money", ref apparelMoneyMinStr, ref apparelMoneyMaxStr, ref apparelMultStr, CopiedPawnValues.CopiedApparelMoney, PawnTweak.ApparelMoneyChanged, (value) => CopiedPawnValues.CopiedApparelMoney = value);

            Rect weaponMoneyRect = new Rect(inRect.x + controlBoxWidth + horizontalSpacing, currentY, controlBoxWidth, controlBoxHeight);
            DrawControlBox(weaponMoneyRect, "Weapon Money", ref weaponMoneyMinStr, ref weaponMoneyMaxStr, ref weaponMultStr, CopiedPawnValues.CopiedWeaponMoney, PawnTweak.WeaponMoneyChanged, (value) => CopiedPawnValues.CopiedWeaponMoney = value);

            currentY += controlBoxHeight + verticalSpacing;

            // Second row: Tech Money and Tech Chance
            Rect techMoneyRect = new Rect(inRect.x, currentY, controlBoxWidth, controlBoxHeight);
            DrawControlBox(techMoneyRect, "Tech Money", ref techHediffsMoneyMinStr, ref techHediffsMoneyMaxStr, ref techMoneyMultStr, CopiedPawnValues.CopiedTechHediffsMoney, PawnTweak.TechHediffsMoneyChanged, (value) => CopiedPawnValues.CopiedTechHediffsMoney = value);

            Rect techChanceRect = new Rect(inRect.x + controlBoxWidth + horizontalSpacing, currentY, controlBoxWidth, controlBoxHeight);
            DrawTechChanceBox(techChanceRect, "Tech Chance", ref techHediffsChanceStr, ref chanceMultStr, CopiedPawnValues.CopiedTechHediffsChance, PawnTweak.TechHediffsChanceChanged, (value) => CopiedPawnValues.CopiedTechHediffsChance = value);

            currentY += controlBoxHeight + verticalSpacing;

            // Tag scroll areas: Three columns
            float scrollColumnWidth = (inRect.width - 2 * horizontalSpacing) / 3;
            float scrollAreaHeight = inRect.height - currentY;

            Rect apparelScrollRect = new Rect(inRect.x, currentY, scrollColumnWidth, scrollAreaHeight);
            DrawTagScrollArea(apparelScrollRect, "Apparel Tags", PawnTweaker.AllApparelTags, selectedApparelTags, ref CopiedPawnValues.CopiedApparelTags, ref apparelScrollPosition, PawnTweak.ApparelTagsChanged);

            Rect weaponScrollRect = new Rect(inRect.x + scrollColumnWidth + horizontalSpacing, currentY, scrollColumnWidth, scrollAreaHeight);
            DrawTagScrollArea(weaponScrollRect, "Weapon Tags", PawnTweaker.AllWeaponTags, selectedWeaponTags, ref CopiedPawnValues.CopiedWeaponTags, ref weaponScrollPosition, PawnTweak.WeaponTagsChanged);

            Rect techScrollRect = new Rect(inRect.x + 2 * (scrollColumnWidth + horizontalSpacing), currentY, scrollColumnWidth, scrollAreaHeight);
            DrawTagScrollArea(techScrollRect, "Tech Tags", PawnTweaker.AllTechHediffTags, selectedTechHediffsTags, ref CopiedPawnValues.CopiedTechHediffsTags, ref techHediffsScrollPosition, PawnTweak.TechHediffsTagsChanged);
        }

        private void DrawTopButtons(Rect rect) {
            float buttonWidth = 100f;
            float gap = 10f;
            float totalWidth = 3 * buttonWidth + 2 * gap;
            float startX = rect.x + (rect.width - totalWidth) / 2;

            Rect saveRect = new Rect(startX, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(saveRect, "Save")) {
                if (float.TryParse(apparelMoneyMinStr, out float apparelMin) && float.TryParse(apparelMoneyMaxStr, out float apparelMax)) {
                    FloatRange newApparelMoney = new FloatRange(apparelMin, apparelMax);
                    if (!newApparelMoney.Equals(PawnTweak.ApparelMoney)) {
                        PawnTweak.apparelMoney = newApparelMoney;
                    } else {
                        PawnTweak.apparelMoney = null;
                    }
                } else {
                    PawnTweak.apparelMoney = null;
                }

                if (float.TryParse(weaponMoneyMinStr, out float weaponMin) && float.TryParse(weaponMoneyMaxStr, out float weaponMax)) {
                    FloatRange newWeaponMoney = new FloatRange(weaponMin, weaponMax);
                    if (!newWeaponMoney.Equals(PawnTweak.WeaponMoney)) {
                        PawnTweak.weaponMoney = newWeaponMoney;
                    } else {
                        PawnTweak.weaponMoney = null;
                    }
                } else {
                    PawnTweak.weaponMoney = null;
                }

                if (float.TryParse(techHediffsMoneyMinStr, out float techMin) && float.TryParse(techHediffsMoneyMaxStr, out float techMax)) {
                    FloatRange newTechMoney = new FloatRange(techMin, techMax);
                    if (!newTechMoney.Equals(PawnTweak.TechHediffsMoney)) {
                        PawnTweak.techHediffsMoney = newTechMoney;
                    } else {
                        PawnTweak.techHediffsMoney = null;
                    }
                } else {
                    PawnTweak.techHediffsMoney = null;
                }

                if (float.TryParse(techHediffsChanceStr, out float chance)) {
                    chance = Mathf.Clamp(chance, 0f, 1f);
                    if (Math.Abs(chance - PawnTweak.TechHediffsChance) > 0.001f) {
                        PawnTweak.techHediffsChance = chance;
                    } else {
                        PawnTweak.techHediffsChance = null;
                    }
                } else {
                    PawnTweak.techHediffsChance = null;
                }

                var apparelTagsList = selectedApparelTags.ToList();
                if (!apparelTagsList.OrderBy(t => t).SequenceEqual(PawnTweak.ApparelTags.OrderBy(t => t))) {
                    PawnTweak.apparelTags = apparelTagsList;
                } else {
                    PawnTweak.apparelTags = null;
                }

                var weaponTagsList = selectedWeaponTags.ToList();
                if (!weaponTagsList.OrderBy(t => t).SequenceEqual(PawnTweak.WeaponTags.OrderBy(t => t))) {
                    PawnTweak.weaponTags = weaponTagsList;
                } else {
                    PawnTweak.weaponTags = null;
                }

                var techHediffsTagsList = selectedTechHediffsTags.ToList();
                if (!techHediffsTagsList.OrderBy(t => t).SequenceEqual(PawnTweak.TechHediffsTags.OrderBy(t => t))) {
                    PawnTweak.techHediffsTags = techHediffsTagsList;
                } else {
                    PawnTweak.techHediffsTags = null;
                }

                Close();
            }

            Rect copyAllRect = new Rect(startX + buttonWidth + gap, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(copyAllRect, "Copy All")) {
                if (float.TryParse(apparelMoneyMinStr, out float apparelMin) && float.TryParse(apparelMoneyMaxStr, out float apparelMax)) {
                    CopiedPawnValues.CopiedApparelMoney = new FloatRange(apparelMin, apparelMax);
                } else CopiedPawnValues.CopiedApparelMoney = null;

                if (float.TryParse(weaponMoneyMinStr, out float weaponMin) && float.TryParse(weaponMoneyMaxStr, out float weaponMax)) {
                    CopiedPawnValues.CopiedWeaponMoney = new FloatRange(weaponMin, weaponMax);
                } else CopiedPawnValues.CopiedWeaponMoney = null;

                if (float.TryParse(techHediffsMoneyMinStr, out float techMin) && float.TryParse(techHediffsMoneyMaxStr, out float techMax)) {
                    CopiedPawnValues.CopiedTechHediffsMoney = new FloatRange(techMin, techMax);
                } else CopiedPawnValues.CopiedTechHediffsMoney = null;

                if (float.TryParse(techHediffsChanceStr, out float chance)) {
                    CopiedPawnValues.CopiedTechHediffsChance = chance;
                } else CopiedPawnValues.CopiedTechHediffsChance = null;

                CopiedPawnValues.CopiedApparelTags = selectedApparelTags.ToList();
                CopiedPawnValues.CopiedWeaponTags = selectedWeaponTags.ToList();
                CopiedPawnValues.CopiedTechHediffsTags = selectedTechHediffsTags.ToList();
            }

            Rect pasteAllRect = new Rect(startX + 2 * (buttonWidth + gap), rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(pasteAllRect, "Paste All")) {
                if (CopiedPawnValues.CopiedApparelMoney.HasValue) {
                    apparelMoneyMinStr = CopiedPawnValues.CopiedApparelMoney.Value.min.ToString("F0");
                    apparelMoneyMaxStr = CopiedPawnValues.CopiedApparelMoney.Value.max.ToString("F0");
                }
                if (CopiedPawnValues.CopiedWeaponMoney.HasValue) {
                    weaponMoneyMinStr = CopiedPawnValues.CopiedWeaponMoney.Value.min.ToString("F0");
                    weaponMoneyMaxStr = CopiedPawnValues.CopiedWeaponMoney.Value.max.ToString("F0");
                }
                if (CopiedPawnValues.CopiedTechHediffsMoney.HasValue) {
                    techHediffsMoneyMinStr = CopiedPawnValues.CopiedTechHediffsMoney.Value.min.ToString("F0");
                    techHediffsMoneyMaxStr = CopiedPawnValues.CopiedTechHediffsMoney.Value.max.ToString("F0");
                }
                if (CopiedPawnValues.CopiedTechHediffsChance.HasValue) {
                    techHediffsChanceStr = CopiedPawnValues.CopiedTechHediffsChance.Value.ToString("F2");
                }
                if (CopiedPawnValues.CopiedApparelTags != null) {
                    selectedApparelTags = new HashSet<string>(CopiedPawnValues.CopiedApparelTags);
                }
                if (CopiedPawnValues.CopiedWeaponTags != null) {
                    selectedWeaponTags = new HashSet<string>(CopiedPawnValues.CopiedWeaponTags);
                }
                if (CopiedPawnValues.CopiedTechHediffsTags != null) {
                    selectedTechHediffsTags = new HashSet<string>(CopiedPawnValues.CopiedTechHediffsTags);
                }
            }
        }

        private void DrawControlBox(Rect boxRect, string label, ref string minStr, ref string maxStr, ref string multStr, FloatRange? copiedValue, bool changed, Action<FloatRange?> setCopiedValue) {
            Widgets.DrawBox(boxRect);
            float innerX = boxRect.x + 5f;
            float innerY = boxRect.y + (boxRect.height - 24f) / 2;

            Color originalColor = GUI.color;
            if (changed) GUI.color = Color.green;
            Widgets.Label(new Rect(innerX, innerY, 100f, 24f), label);
            GUI.color = originalColor;
            innerX += 105f;

            minStr = Widgets.TextField(new Rect(innerX, innerY, 60f, 24f), minStr);
            innerX += 65f;
            Widgets.Label(new Rect(innerX, innerY, 10f, 24f), "-");
            innerX += 15f;
            maxStr = Widgets.TextField(new Rect(innerX, innerY, 60f, 24f), maxStr);
            innerX += 65f;
            multStr = Widgets.TextField(new Rect(innerX, innerY, 40f, 24f), multStr);
            innerX += 45f;

            float multiplyWidth = Text.CalcSize("Multiply").x + 10f;
            if (Widgets.ButtonText(new Rect(innerX, innerY, multiplyWidth, 24f), "Multiply")) {
                if (float.TryParse(multStr, out float multiplier) && float.TryParse(minStr, out float min) && float.TryParse(maxStr, out float max)) {
                    minStr = (min * multiplier).ToString("F0");
                    maxStr = (max * multiplier).ToString("F0");
                }
            }
            innerX += multiplyWidth + 5f;

            float copyWidth = Text.CalcSize("Copy").x + 10f;
            if (Widgets.ButtonText(new Rect(innerX, innerY, copyWidth, 24f), "Copy")) {
                if (float.TryParse(minStr, out float min) && float.TryParse(maxStr, out float max)) {
                    setCopiedValue(new FloatRange(min, max));
                } else {
                    setCopiedValue(null);
                }
            }
            if (copiedValue.HasValue) {
                innerX += copyWidth + 5f;
                float pasteWidth = Text.CalcSize("Paste").x + 10f;
                if (Widgets.ButtonText(new Rect(innerX, innerY, pasteWidth, 24f), "Paste")) {
                    minStr = copiedValue.Value.min.ToString("F0");
                    maxStr = copiedValue.Value.max.ToString("F0");
                }
            }
        }

        private void DrawTechChanceBox(Rect boxRect, string label, ref string chanceStr, ref string multStr, float? copiedChance, bool changed, Action<float?> setCopiedChance) {
            Widgets.DrawBox(boxRect);
            float innerX = boxRect.x + 5f;
            float innerY = boxRect.y + (boxRect.height - 24f) / 2;

            Color originalColor = GUI.color;
            if (changed) GUI.color = Color.green;
            Widgets.Label(new Rect(innerX, innerY, 100f, 24f), label);
            GUI.color = originalColor;
            innerX += 105f;

            chanceStr = Widgets.TextField(new Rect(innerX, innerY, 60f, 24f), chanceStr);
            innerX += 65f;
            multStr = Widgets.TextField(new Rect(innerX, innerY, 40f, 24f), multStr);
            innerX += 45f;

            float multiplyWidth = Text.CalcSize("Multiply").x + 10f;
            if (Widgets.ButtonText(new Rect(innerX, innerY, multiplyWidth, 24f), "Multiply")) {
                if (float.TryParse(multStr, out float multiplier) && float.TryParse(chanceStr, out float chance)) {
                    chanceStr = Mathf.Clamp(chance * multiplier, 0f, 1f).ToString("F2");
                }
            }
            innerX += multiplyWidth + 5f;

            float copyWidth = Text.CalcSize("Copy").x + 10f;
            if (Widgets.ButtonText(new Rect(innerX, innerY, copyWidth, 24f), "Copy")) {
                if (float.TryParse(chanceStr, out float chance)) {
                    setCopiedChance(chance);
                } else {
                    setCopiedChance(null);
                }
            }
            if (copiedChance.HasValue) {
                innerX += copyWidth + 5f;
                float pasteWidth = Text.CalcSize("Paste").x + 10f;
                if (Widgets.ButtonText(new Rect(innerX, innerY, pasteWidth, 24f), "Paste")) {
                    chanceStr = copiedChance.Value.ToString("F2");
                }
            }
        }

        private void DrawTagScrollArea(Rect rect, string label, List<string> allTags, HashSet<string> selectedTags, ref List<string> copiedTags, ref Vector2 scrollPosition, bool changed) {
            float currentY = rect.y;

            Rect labelRect = new Rect(rect.x, currentY, rect.width, 24f);
            Color originalColor = GUI.color;
            if (changed) {
                GUI.color = Color.green;
            }
            Widgets.Label(labelRect, label);
            GUI.color = originalColor;
            currentY += 24f;

            float buttonWidth = 50f;
            Rect copyRect = new Rect(rect.x, currentY, buttonWidth, 24f);
            if (Widgets.ButtonText(copyRect, "Copy")) {
                copiedTags = selectedTags.ToList();
            }

            if (copiedTags != null && copiedTags.Any()) {
                Rect pasteRect = new Rect(rect.x + buttonWidth + 5f, currentY, buttonWidth, 24f);
                if (Widgets.ButtonText(pasteRect, "Paste")) {
                    selectedTags.Clear();
                    selectedTags.UnionWith(copiedTags);
                }
            }

            currentY += 24f + 10f;

            float scrollHeight = rect.height - (currentY - rect.y);
            Rect scrollRect = new Rect(rect.x, currentY, rect.width, scrollHeight);
            float viewHeight = allTags.Count * 24f;
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float tagY = 0f;
            foreach (string tag in allTags) {
                Rect checkRect = new Rect(0f, tagY, viewRect.width, 24f);
                bool isChecked = selectedTags.Contains(tag);
                Widgets.CheckboxLabeled(checkRect, tag, ref isChecked, placeCheckboxNearText: true);
                if (isChecked != selectedTags.Contains(tag)) {
                    if (isChecked) selectedTags.Add(tag);
                    else selectedTags.Remove(tag);
                }
                tagY += 24f;
            }
            Widgets.EndScrollView();
        }
    }
}