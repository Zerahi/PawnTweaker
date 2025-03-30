using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnTweaker {
    public class PawnTweaker : Mod {
        public List<PawnTweakData> Data { get; private set; }
        public static List<string> AllTechHediffTags { get; private set; }
        public static List<string> AllApparelTags { get; private set; }
        public static List<string> AllWeaponTags { get; private set; }

        private string searchText = "";
        private FactionDef selectedFaction = null;
        private bool showOnlyModified = false;
        private List<PawnTweakData> selectedPawnKinds = new List<PawnTweakData>();
        private string selectedMultiplyField = null;
        private string multiplyValueStr = "1";
        private Vector2 scrollPosition = Vector2.zero;
        private string lastSearchText = "";
        private FactionDef lastSelectedFaction = null;
        private bool lastShowOnlyModified = false;
        private List<PawnTweakData> cachedFilteredPawns = null;
        private bool showNoFaction = false;

        private static readonly List<string> columns = new List<string> { "Select", "DefName", "Actions", "ApparelMoney", "WeaponMoney", "TechHediffsMoney", "TechHediffsChance", "ApparelTags", "WeaponTags", "TechHediffsTags" };
        private static readonly Dictionary<string, float> columnWidths = new Dictionary<string, float>
        {
            { "Select", 30f }, { "DefName", 200f }, { "Actions", 120f }, { "ApparelMoney", 100f },
            { "WeaponMoney", 100f }, { "TechHediffsMoney", 100f }, { "TechHediffsChance", 50f },
            { "ApparelTags", 100f }, { "WeaponTags", 100f }, { "TechHediffsTags", 100f }
        };
        private static readonly List<string> multipliableFields = new List<string> { "apparelMoney", "weaponMoney", "techHediffsMoney", "techHediffsChance" };
        public static string FilePath => Path.Combine(GenFilePaths.ConfigFolderPath, "PawnTweaks.csv");

        public PawnTweaker(ModContentPack content) : base(content) {
            LongEventHandler.QueueLongEvent(InitializePawnData, "Initializing Pawn Data", false, null);
        }

        private void InitializePawnData() {
            Data = new List<PawnTweakData>();
            foreach (var def in DefDatabase<PawnKindDef>.AllDefs) {
                Data.Add(new PawnTweakData(def));
            }
            AllTechHediffTags = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.isTechHediff)
                .SelectMany(def => def.techHediffsTags ?? Enumerable.Empty<string>())
                .Distinct()
                .OrderBy(tag => tag)
                .ToList();
            AllApparelTags = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.apparel != null && def.apparel.tags != null)
                .SelectMany(def => def.apparel.tags)
                .Distinct()
                .OrderBy(tag => tag)
                .ToList();
            AllWeaponTags = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.weaponTags != null)
                .SelectMany(def => def.weaponTags)
                .Distinct()
                .OrderBy(tag => tag)
                .ToList();
            LoadChanges();
            ApplyAllChanges();
            UpdateFilteredPawns();
        }

        public override string SettingsCategory() {
            return "Pawn Tweaker";
        }

        public void LoadChanges() {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath)) return;
            try {
                foreach (var tweak in Data) {
                    tweak.SetDefault();
                }

                List<string> lines = File.ReadAllLines(FilePath).ToList();
                foreach (var line in lines) {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(',');
                    if (parts.Length < 1) continue;

                    var tweak = Data.FirstOrDefault(x => x.PawnKindDef.defName == parts[0]);
                    if (tweak == null) continue;

                    // Apparel Money (column 1)
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1])) {
                        string[] rangeParts = parts[1].Split('~');
                        if (rangeParts.Length == 2 && float.TryParse(rangeParts[0], out float min) && float.TryParse(rangeParts[1], out float max)) {
                            tweak.apparelMoney = new FloatRange(min, max);
                        }
                    }

                    // Weapon Money (column 2)
                    if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2])) {
                        string[] rangeParts = parts[2].Split('~');
                        if (rangeParts.Length == 2 && float.TryParse(rangeParts[0], out float min) && float.TryParse(rangeParts[1], out float max)) {
                            tweak.weaponMoney = new FloatRange(min, max);
                        }
                    }

                    // Tech Hediffs Money (column 3)
                    if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3])) {
                        string[] rangeParts = parts[3].Split('~');
                        if (rangeParts.Length == 2 && float.TryParse(rangeParts[0], out float min) && float.TryParse(rangeParts[1], out float max)) {
                            tweak.techHediffsMoney = new FloatRange(min, max);
                        }
                    }

                    // Tech Hediffs Chance (column 4)
                    if (parts.Length > 4 && !string.IsNullOrEmpty(parts[4])) {
                        if (float.TryParse(parts[4], out float chance)) {
                            tweak.techHediffsChance = chance;
                        }
                    }

                    // Tech Hediffs Tags (column 5)
                    if (parts.Length > 5 && !string.IsNullOrEmpty(parts[5])) {
                        List<string> tags = parts[5].Split(';').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                        if (tags.Count > 0) tweak.techHediffsTags = tags;
                    }

                    // Apparel Tags (column 6)
                    if (parts.Length > 6 && !string.IsNullOrEmpty(parts[6])) {
                        List<string> tags = parts[6].Split(';').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                        if (tags.Count > 0) tweak.apparelTags = tags;
                    }

                    // Weapon Tags (column 7)
                    if (parts.Length > 7 && !string.IsNullOrEmpty(parts[7])) {
                        List<string> tags = parts[7].Split(';').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                        if (tags.Count > 0) tweak.weaponTags = tags;
                    }
                }
            } catch (Exception ex) {
                Log.Error("Pawn Tweaker: Error loading CSV: " + ex.Message);
                Messages.Message($"Error loading config: {ex.Message}", MessageTypeDefOf.NegativeEvent);
            }
        }

        public void ApplyAllChanges() {
            Data.ForEach(x => x.ApplyTweak());
        }

        public override void DoSettingsWindowContents(Rect inRect) {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Rect controlRow = listing.GetRect(24f);
            float x = controlRow.x;

            FactionDef previousFaction = selectedFaction;
            Rect factionRect = new Rect(x, controlRow.y, 100f, 24f);
            if (Widgets.ButtonText(factionRect, selectedFaction != null ? selectedFaction.label : (showNoFaction ? "No Faction" : "All Factions"))) {
                List<FactionDef> factionDefsWithPawns = new List<FactionDef>();
                foreach (PawnKindDef p in DefDatabase<PawnKindDef>.AllDefs) {
                    if (p.defaultFactionType != null && !factionDefsWithPawns.Contains(p.defaultFactionType))
                        factionDefsWithPawns.Add(p.defaultFactionType);
                }
                factionDefsWithPawns.Sort((a, b) => a.label.CompareTo(b.label));
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("All Factions", delegate {
                    selectedFaction = null;
                    showNoFaction = false;
                    selectedPawnKinds.Clear();
                    UpdateFilteredPawns();
                }));
                options.Add(new FloatMenuOption("No Faction", delegate {
                    selectedFaction = null;
                    showNoFaction = true;
                    selectedPawnKinds.Clear();
                    UpdateFilteredPawns();
                }));
                foreach (FactionDef f in factionDefsWithPawns) {
                    options.Add(new FloatMenuOption(f.label, delegate {
                        selectedFaction = f;
                        showNoFaction = false;
                        selectedPawnKinds.Clear();
                        UpdateFilteredPawns();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(factionRect, "Filter pawns by their default faction.");
            x += 105f;

            bool previousShowOnlyModified = showOnlyModified;
            Rect modifiedRect = new Rect(x, controlRow.y, 200f, 24f);
            Widgets.CheckboxLabeled(modifiedRect, "Show Only Modified Pawns", ref showOnlyModified);
            TooltipHandler.TipRegion(modifiedRect, "Show only pawns with custom settings.");
            x += 205f;

            if (Widgets.ButtonText(new Rect(x, controlRow.y, 80f, 24f), "Select All")) {
                selectedPawnKinds.Clear();
                selectedPawnKinds.AddRange(cachedFilteredPawns);
            }
            TooltipHandler.TipRegion(new Rect(x, controlRow.y, 80f, 24f), "Select all pawns in the list below.");
            x += 85f;

            if (Widgets.ButtonText(new Rect(x, controlRow.y, 90f, 24f), "Deselect All")) {
                selectedPawnKinds.Clear();
            }
            TooltipHandler.TipRegion(new Rect(x, controlRow.y, 90f, 24f), "Deselect all pawns in the list below.");
            x += 95f;

            string previousSearchText = searchText;
            Widgets.Label(new Rect(x, controlRow.y, 80f, 24f), "Filter Pawns");
            x += 80f;
            Rect searchRect = new Rect(x, controlRow.y, 150f, 24f);
            searchText = Widgets.TextField(searchRect, searchText);
            TooltipHandler.TipRegion(searchRect, "Type here to search pawns by name.");
            x += 155f;

            if (previousSearchText != searchText || previousFaction != selectedFaction || previousShowOnlyModified != showOnlyModified) {
                UpdateFilteredPawns();
                selectedPawnKinds.Clear();
            }

            listing.GapLine();

            Rect headerRect = listing.GetRect(24f);
            x = headerRect.x;
            foreach (string column in columns) {
                string displayColumn = column == "TechHediffsMoney" ? "Tech Money" : column == "TechHediffsTags" ? "Tech Tags" : column == "TechHediffsChance" ? "Tech%" : column;
                Rect cellRect = new Rect(x, headerRect.y, columnWidths[column], 24f);
                Widgets.Label(cellRect, displayColumn);
                x += columnWidths[column];
            }

            Rect listRect = listing.GetRect(400f);
            float totalWidth = columnWidths.Values.Sum();
            Rect pawnViewRect = new Rect(0, 0, totalWidth, cachedFilteredPawns.Count * 24f);
            Widgets.BeginScrollView(listRect, ref scrollPosition, pawnViewRect);

            float y = 0;
            foreach (PawnTweakData tweak in cachedFilteredPawns) {
                x = 0;
                Rect checkboxRect = new Rect(x, y, columnWidths["Select"], 24f);
                bool isSelected = selectedPawnKinds.Contains(tweak);
                Widgets.Checkbox(checkboxRect.x, checkboxRect.y, ref isSelected);
                if (isSelected && !selectedPawnKinds.Contains(tweak)) selectedPawnKinds.Add(tweak);
                else if (!isSelected && selectedPawnKinds.Contains(tweak)) selectedPawnKinds.Remove(tweak);
                x += columnWidths["Select"];

                Widgets.Label(new Rect(x, y, columnWidths["DefName"], 24f), tweak.PawnKindDef.defName);
                x += columnWidths["DefName"];

                Rect actionsRect = new Rect(x, y, columnWidths["Actions"], 24f);
                Rect editRect = new Rect(actionsRect.x, actionsRect.y, 55f, 24f);
                if (Widgets.ButtonText(editRect, "Edit")) {
                    Find.WindowStack.Add(new EditPawnDialog(tweak));
                }
                TooltipHandler.TipRegion(editRect, "Open a window to edit this pawn’s settings.");
                Rect defaultRect = new Rect(actionsRect.x + 60f, actionsRect.y, 55f, 24f);
                if (Widgets.ButtonText(defaultRect, "Clear")) {
                    tweak.SetDefault();
                    UpdateFilteredPawns();
                }
                TooltipHandler.TipRegion(defaultRect, "Clear pawn's changes (Requires restart to restore default in game values!).");
                x += columnWidths["Actions"];

                Widgets.Label(new Rect(x, y, columnWidths["ApparelMoney"], 24f), tweak.ApparelMoneyString);
                x += columnWidths["ApparelMoney"];

                Widgets.Label(new Rect(x, y, columnWidths["WeaponMoney"], 24f), tweak.WeaponMoneyString);
                x += columnWidths["WeaponMoney"];

                Widgets.Label(new Rect(x, y, columnWidths["TechHediffsMoney"], 24f), tweak.TechHediffsMoneyString);
                x += columnWidths["TechHediffsMoney"];

                Widgets.Label(new Rect(x, y, columnWidths["TechHediffsChance"], 24f), tweak.TechHediffsChanceString);
                x += columnWidths["TechHediffsChance"];

                Rect apparelTagsRect = new Rect(x, y, columnWidths["ApparelTags"], 24f);
                Widgets.Label(apparelTagsRect, tweak.ApparelTagsString);
                TooltipHandler.TipRegion(apparelTagsRect, tweak.ApparelTagsString);
                x += columnWidths["ApparelTags"];

                Rect weaponTagsRect = new Rect(x, y, columnWidths["WeaponTags"], 24f);
                Widgets.Label(weaponTagsRect, tweak.WeaponTagsString);
                TooltipHandler.TipRegion(weaponTagsRect, tweak.WeaponTagsString);
                x += columnWidths["WeaponTags"];

                Rect techHediffsTagsRect = new Rect(x, y, columnWidths["TechHediffsTags"], 24f);
                Widgets.Label(techHediffsTagsRect, tweak.TechHediffsTagsString);
                TooltipHandler.TipRegion(techHediffsTagsRect, tweak.TechHediffsTagsString);
                x += columnWidths["TechHediffsTags"];

                y += 24f;
            }
            Widgets.EndScrollView();
            listing.GapLine();

            Rect actionRow = listing.GetRect(30f);
            x = actionRow.x;

            Rect verifyRect = new Rect(x, actionRow.y, 80f, 30f);
            if (Widgets.ButtonText(verifyRect, "Verify")) {
                VerifySelectedPawns();
            }
            TooltipHandler.TipRegion(verifyRect, "Check if selected pawns’ current game settings match what’s shown here.");
            x += 85f;

            Rect separator1Rect = new Rect(x, actionRow.y, 2f, 30f);
            Widgets.DrawLineVertical(separator1Rect.x, separator1Rect.y, separator1Rect.height);
            x += 5f;

            Rect fieldRect = new Rect(x, actionRow.y, 120f, 30f);
            if (Widgets.ButtonText(fieldRect, selectedMultiplyField != null ? selectedMultiplyField : "Select Field")) {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (string f in multipliableFields) {
                    options.Add(new FloatMenuOption(f, delegate { selectedMultiplyField = f; }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(fieldRect, "Pick a setting to multiply for selected pawns.");
            x += 125f;

            Rect multInputRect = new Rect(x, actionRow.y, 80f, 30f);
            multiplyValueStr = Widgets.TextField(multInputRect, multiplyValueStr);
            TooltipHandler.TipRegion(multInputRect, "Enter a number to multiply the selected setting by.");
            x += 85f;

            Rect applyRect = new Rect(x, actionRow.y, 120f, 30f);
            if (Widgets.ButtonText(applyRect, "Multiply") && float.TryParse(multiplyValueStr, out float multiplier)) {
                if (selectedPawnKinds.Count == 0) {
                    Messages.Message("No pawns selected to Multiply.", MessageTypeDefOf.RejectInput);
                    return;
                }
                if (selectedMultiplyField == null) {
                    Messages.Message("Select a Field to Multiply.", MessageTypeDefOf.RejectInput);
                    return;
                }
                foreach (PawnTweakData pawn in selectedPawnKinds) {
                    if (selectedMultiplyField == "apparelMoney") {
                        FloatRange current = pawn.ApparelMoney;
                        FloatRange multiplied = new FloatRange(current.min * multiplier, current.max * multiplier);
                        if (!multiplied.Equals(current)) {
                            pawn.apparelMoney = multiplied;
                        }
                    } else if (selectedMultiplyField == "weaponMoney") {
                        FloatRange current = pawn.WeaponMoney;
                        FloatRange multiplied = new FloatRange(current.min * multiplier, current.max * multiplier);
                        if (!multiplied.Equals(current)) {
                            pawn.weaponMoney = multiplied;
                        }
                    } else if (selectedMultiplyField == "techHediffsMoney") {
                        FloatRange current = pawn.TechHediffsMoney;
                        FloatRange multiplied = new FloatRange(current.min * multiplier, current.max * multiplier);
                        if (!multiplied.Equals(current)) {
                            pawn.techHediffsMoney = multiplied;
                        }
                    } else if (selectedMultiplyField == "techHediffsChance") {
                        float current = pawn.TechHediffsChance;
                        float multipliedChance = Mathf.Clamp(current * multiplier, 0f, 1f);
                        if (Math.Abs(multipliedChance - current) > 0.001f) { // Small epsilon for float comparison
                            pawn.techHediffsChance = multipliedChance;
                        }
                    }
                }
                UpdateFilteredPawns();
            }
            TooltipHandler.TipRegion(applyRect, "Apply the multiplier to the selected pawns’ chosen setting.");

            x += 125f;

            Rect separator2Rect = new Rect(x, actionRow.y, 2f, 30f);
            Widgets.DrawLineVertical(separator2Rect.x, separator2Rect.y, separator2Rect.height);
            x += 5f;

            Rect loadConfigRect = new Rect(x, actionRow.y, 100f, 30f);
            if (Widgets.ButtonText(loadConfigRect, "Load Config")) {
                LoadChanges();
                Messages.Message("Config loaded successfully.", MessageTypeDefOf.PositiveEvent);
                UpdateFilteredPawns();
            }
            TooltipHandler.TipRegion(loadConfigRect, "Load the config file, undoing any changes not yet saved.");
            x += 105f;

            Rect exportRect = new Rect(x, actionRow.y, 100f, 30f);
            if (Widgets.ButtonText(exportRect, "Export/Apply")) {
                ExportChanges();
                Messages.Message($"Changes exported and applied successfully. {PawnTweaker.FilePath}", MessageTypeDefOf.PositiveEvent);
            }
            TooltipHandler.TipRegion(exportRect, "Save changes to the config file and apply them to new pawns in-game.");

            listing.End();
        }

        private void UpdateFilteredPawns() {
            cachedFilteredPawns = new List<PawnTweakData>();
            foreach (PawnTweakData p in Data) {
                bool factionMatch;
                if (showNoFaction) {
                    factionMatch = p.PawnKindDef.defaultFactionType == null;
                } else if (selectedFaction != null) {
                    factionMatch = p.PawnKindDef.defaultFactionType == selectedFaction;
                } else {
                    factionMatch = true; // All factions
                }
                if (factionMatch &&
                    (string.IsNullOrEmpty(searchText) || p.PawnKindDef.defName.ToLower().Contains(searchText.ToLower())) &&
                    (!showOnlyModified || !p.IsEmpty())) {
                    cachedFilteredPawns.Add(p);
                }
            }
            cachedFilteredPawns.Sort((a, b) => a.PawnKindDef.defName.CompareTo(b.PawnKindDef.defName));
            lastSearchText = searchText;
            lastSelectedFaction = selectedFaction;
            lastShowOnlyModified = showOnlyModified;
        }

        private void VerifySelectedPawns() {
            if (selectedPawnKinds.Count == 0) {
                Messages.Message("No pawns selected to verify.", MessageTypeDefOf.RejectInput);
                return;
            }
            bool allMatch = true;
            string mismatchDetails = "";
            foreach (PawnTweakData pawn in selectedPawnKinds) {
                var (matches, mismatches) = pawn.Verify();
                if (!matches) {
                    allMatch = false;
                    mismatchDetails += "\nPawn: " + pawn.PawnKindDef.defName + mismatches;
                }
            }
            if (allMatch)
                Messages.Message("All selected pawns’ in-game values match the config window values.", MessageTypeDefOf.PositiveEvent);
            else
                Messages.Message("Mismatch detected for the following pawns:" + mismatchDetails, MessageTypeDefOf.NegativeEvent);
        }

        private void ExportChanges() {
            try {
                string path = FilePath;
                if (string.IsNullOrEmpty(path)) throw new Exception("Unable to determine file path.");
                List<string> lines = new List<string>();
                foreach (PawnTweakData pawn in Data) {
                    if (pawn.IsEmpty()) continue;
                    List<string> lineParts = new List<string>
                    {
                        pawn.PawnKindDef.defName,
                        pawn.apparelMoney.HasValue ? $"{pawn.apparelMoney.Value}" : "",
                        pawn.weaponMoney.HasValue ? $"{pawn.weaponMoney.Value}" : "",
                        pawn.techHediffsMoney.HasValue ? $"{pawn.techHediffsMoney.Value}" : "",
                        pawn.techHediffsChance.HasValue ? pawn.techHediffsChance.Value.ToString() : "",
                        pawn.techHediffsTags != null && pawn.techHediffsTags.Any() ? string.Join(";", pawn.techHediffsTags) : "",
                        pawn.apparelTags != null && pawn.apparelTags.Any() ? string.Join(";", pawn.apparelTags) : "",
                        pawn.weaponTags != null && pawn.weaponTags.Any() ? string.Join(";", pawn.weaponTags) : ""
                    };
                    lines.Add(string.Join(",", lineParts));
                }
                File.WriteAllLines(path, lines.ToArray());
                ApplyAllChanges();
                Log.Message("Exported changes to " + path);
            } catch (Exception ex) {
                Log.Error("Export failed: " + ex.Message);
                Messages.Message($"Error exporting changes: {ex.Message}", MessageTypeDefOf.NegativeEvent);
            }
        }
    }

    public class PawnTweakData {
        public PawnKindDef PawnKindDef { get; private set; }
        public FloatRange ApparelMoney {
            get { return apparelMoney ?? PawnKindDef.apparelMoney; }
            set { apparelMoney = value; }
        }
        public FloatRange? apparelMoney;

        public List<string> ApparelTags {
            get { return apparelTags ?? (PawnKindDef.apparelTags != null ? PawnKindDef.apparelTags.ToList() : new List<string>()); }
            set { apparelTags = value; }
        }
        public List<string> apparelTags;

        public FloatRange WeaponMoney {
            get { return weaponMoney ?? PawnKindDef.weaponMoney; }
            set { weaponMoney = value; }
        }
        public FloatRange? weaponMoney;

        public List<string> WeaponTags {
            get { return weaponTags ?? (PawnKindDef.weaponTags != null ? PawnKindDef.weaponTags.ToList() : new List<string>()); }
            set { weaponTags = value; }
        }
        public List<string> weaponTags;

        public FloatRange TechHediffsMoney {
            get { return techHediffsMoney ?? PawnKindDef.techHediffsMoney; }
            set { techHediffsMoney = value; }
        }
        public FloatRange? techHediffsMoney;

        public float TechHediffsChance {
            get { return techHediffsChance ?? PawnKindDef.techHediffsChance; }
            set { techHediffsChance = value; }
        }
        public float? techHediffsChance;

        public List<string> TechHediffsTags {
            get { return techHediffsTags ?? (PawnKindDef.techHediffsTags != null ? PawnKindDef.techHediffsTags.ToList() : new List<string>()); }
            set { techHediffsTags = value; }
        }
        public List<string> techHediffsTags;

        public PawnTweakData(PawnKindDef def) {
            PawnKindDef = def;
        }

        public string ApparelMoneyString { get { return apparelMoney.HasValue ? $"{apparelMoney.Value}* " : PawnKindDef.apparelMoney.ToString(); } }
        public string WeaponMoneyString { get { return weaponMoney.HasValue ? $"{weaponMoney.Value}*" : PawnKindDef.weaponMoney.ToString(); } }
        public string TechHediffsMoneyString { get { return techHediffsMoney.HasValue ? $"{techHediffsMoney}*" : PawnKindDef.techHediffsMoney.ToString(); } }
        public string TechHediffsChanceString { get { return techHediffsChance.HasValue ? $"{techHediffsChance.Value:F2}*" : $"{PawnKindDef.techHediffsChance:F2}"; } }
        public string ApparelTagsString {
            get {
                if (apparelTags != null)
                    return (apparelTags.Any() ? "*" + string.Join(",", apparelTags) : "None");
                var defaultTags = PawnKindDef.apparelTags;
                return defaultTags != null && defaultTags.Any() ? string.Join(",", defaultTags) : "None";
            }
        }
        public string WeaponTagsString {
            get {
                if (weaponTags != null)
                    return (weaponTags.Any() ? "*" + string.Join(",", weaponTags) : "None");
                var defaultTags = PawnKindDef.weaponTags;
                return defaultTags != null && defaultTags.Any() ? string.Join(",", defaultTags) : "None";
            }
        }
        public string TechHediffsTagsString {
            get {
                if (techHediffsTags != null)
                    return (techHediffsTags.Any() ? "*" + string.Join(",", techHediffsTags) : "None");
                var defaultTags = PawnKindDef.techHediffsTags;
                return defaultTags != null && defaultTags.Any() ? string.Join(",", defaultTags) : "None";
            }
        }

        public bool ApparelMoneyChanged => apparelMoney.HasValue;
        public bool WeaponMoneyChanged => weaponMoney.HasValue;
        public bool TechHediffsMoneyChanged => techHediffsMoney.HasValue;
        public bool TechHediffsChanceChanged => techHediffsChance.HasValue;
        public bool ApparelTagsChanged => apparelTags != null;
        public bool WeaponTagsChanged => weaponTags != null;
        public bool TechHediffsTagsChanged => techHediffsTags != null;

        public bool IsEmpty() {
            if (apparelMoney != null) return false;
            if (weaponMoney != null) return false;
            if (techHediffsMoney != null) return false;
            if (techHediffsChance != null) return false;
            if (apparelTags != null) return false;
            if (weaponTags != null) return false;
            if (techHediffsTags != null) return false;
            return true;
        }

        public void SetDefault() {
            apparelMoney = null;
            weaponMoney = null;
            techHediffsMoney = null;
            techHediffsChance = null;
            apparelTags = null;
            weaponTags = null;
            techHediffsTags = null;
        }

        public (bool matches, string mismatches) Verify() {
            bool matches = true;
            string mismatches = "";
            if (apparelMoney.HasValue && !apparelMoney.Value.Equals(PawnKindDef.apparelMoney)) {
                matches = false;
                mismatches += $"\n  ApparelMoney: Config = {apparelMoney.Value.min}~{apparelMoney.Value.max}, In-Game = {PawnKindDef.apparelMoney.min}~{PawnKindDef.apparelMoney.max}";
            }
            if (weaponMoney.HasValue && !weaponMoney.Value.Equals(PawnKindDef.weaponMoney)) {
                matches = false;
                mismatches += $"\n  WeaponMoney: Config = {weaponMoney.Value.min}~{weaponMoney.Value.max}, In-Game = {PawnKindDef.weaponMoney.min}~{PawnKindDef.weaponMoney.max}";
            }
            if (techHediffsMoney.HasValue && !techHediffsMoney.Value.Equals(PawnKindDef.techHediffsMoney)) {
                matches = false;
                mismatches += $"\n  TechHediffsMoney: Config = {techHediffsMoney.Value.min}~{techHediffsMoney.Value.max}, In-Game = {PawnKindDef.techHediffsMoney.min}~{PawnKindDef.techHediffsMoney.max}";
            }
            if (techHediffsChance.HasValue && Math.Abs(techHediffsChance.Value - PawnKindDef.techHediffsChance) > 0.001f) {
                matches = false;
                mismatches += $"\n  TechHediffsChance: Config = {techHediffsChance.Value}, In-Game = {PawnKindDef.techHediffsChance}";
            }
            if (apparelTags != null && !apparelTags.OrderBy(t => t).SequenceEqual((PawnKindDef.apparelTags ?? new List<string>()).OrderBy(t => t))) {
                matches = false;
                mismatches += $"\n  ApparelTags: Config = {string.Join(", ", apparelTags)}, In-Game = {string.Join(", ", PawnKindDef.apparelTags ?? new List<string>())}";
            }
            if (weaponTags != null && !weaponTags.OrderBy(t => t).SequenceEqual((PawnKindDef.weaponTags ?? new List<string>()).OrderBy(t => t))) {
                matches = false;
                mismatches += $"\n  WeaponTags: Config = {string.Join(", ", weaponTags)}, In-Game = {string.Join(", ", PawnKindDef.weaponTags ?? new List<string>())}";
            }
            if (techHediffsTags != null && !techHediffsTags.OrderBy(t => t).SequenceEqual((PawnKindDef.techHediffsTags ?? new List<string>()).OrderBy(t => t))) {
                matches = false;
                mismatches += $"\n  TechHediffsTags: Config = {string.Join(", ", techHediffsTags)}, In-Game = {string.Join(", ", PawnKindDef.techHediffsTags ?? new List<string>())}";
            }
            return (matches, mismatches);
        }

        public void ApplyTweak() {
            try {
                if (apparelMoney.HasValue)
                    PawnKindDef.apparelMoney = apparelMoney.Value;
                if (weaponMoney.HasValue)
                    PawnKindDef.weaponMoney = weaponMoney.Value;
                if (techHediffsMoney.HasValue)
                    PawnKindDef.techHediffsMoney = techHediffsMoney.Value;
                if (techHediffsChance.HasValue)
                    PawnKindDef.techHediffsChance = techHediffsChance.Value;
                if (apparelTags != null)
                    PawnKindDef.apparelTags = apparelTags;
                if (weaponTags != null)
                    PawnKindDef.weaponTags = weaponTags;
                if (techHediffsTags != null)
                    PawnKindDef.techHediffsTags = techHediffsTags;
            } catch (Exception ex) {
                Log.Error($"PawnTweaker: Error applying tweak to {PawnKindDef.defName}: {ex.Message}");
            }
        }
    }

    public static class CopiedPawnValues {
        public static FloatRange? CopiedApparelMoney = null;
        public static FloatRange? CopiedWeaponMoney = null;
        public static FloatRange? CopiedTechHediffsMoney = null;
        public static float? CopiedTechHediffsChance = null;
        public static List<string> CopiedApparelTags = null;
        public static List<string> CopiedWeaponTags = null;
        public static List<string> CopiedTechHediffsTags = null;

        public static void Clear() {
            CopiedApparelMoney = null;
            CopiedWeaponMoney = null;
            CopiedTechHediffsMoney = null;
            CopiedTechHediffsChance = null;
            CopiedApparelTags = null;
            CopiedWeaponTags = null;
            CopiedTechHediffsTags = null;
        }
    }
}