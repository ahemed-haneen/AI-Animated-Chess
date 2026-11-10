using UnityEngine;

namespace BattleChess
{
    /// <summary>Builds the starter themes in code. The editor menu saves them as assets you can edit in the Inspector.</summary>
    public static class ThemeLibrary
    {
        static Color C(string hex) { Color c; ColorUtility.TryParseHtmlString(hex, out c); return c; }

        static PieceDefinition P(string name, AttackStyle atk, Locomotion loco, PlaceholderKind kind, string coat, string trousers, string accent, HatKind hat, WeaponKind weapon, float height = 1f, bool beard = false, bool wig = false, string skin = "#EFC39E")
        {
            return new PieceDefinition
            {
                displayName = name, attack = atk, locomotion = loco,
                placeholder = new PlaceholderLook
                {
                    kind = kind, coat = C(coat), trousers = C(trousers), accent = C(accent), skin = C(skin),
                    hat = hat, weapon = weapon, height = height, beard = beard, whiteWig = wig
                }
            };
        }

        public static ThemeDefinition PiratesVsBritish()
        {
            var t = ScriptableObject.CreateInstance<ThemeDefinition>();
            t.name = "PiratesVsBritish";
            t.title = "Pirates vs British";

            t.white = new ArmyDefinition
            {
                name = "Pirates", color = C("#1D1A18"),
                king   = P("Pirate Captain", AttackStyle.Slash, Locomotion.Walk, PlaceholderKind.Soldier, "#2A1F24", "#2A1F24", "#B8231E", HatKind.Tricorne, WeaponKind.Cutlass, 1.12f, true, false, "#D9A07A"),
                queen  = P("Pirate Queen", AttackStyle.Shoot, Locomotion.Walk, PlaceholderKind.Soldier, "#5A1414", "#1A1A1C", "#B8231E", HatKind.FeatherHat, WeaponKind.Pistol, 1.03f, false, false, "#E3B088"),
                rook   = P("Pirate Ship", AttackStyle.Cannon, Locomotion.Sail, PlaceholderKind.Ship, "#4A2C17", "#2A2622", "#111111", HatKind.None, WeaponKind.None),
                bishop = P("Gunner", AttackStyle.Shoot, Locomotion.Walk, PlaceholderKind.Soldier, "#3A2A1A", "#4A3A2A", "#B8231E", HatKind.Bandana, WeaponKind.Blunderbuss, 1.05f, true, false, "#C68A5C"),
                knight = P("Swashbuckler", AttackStyle.Slash, Locomotion.Walk, PlaceholderKind.Soldier, "#7A1A1A", "#1A1A1C", "#2A2018", HatKind.FeatherHat, WeaponKind.Sword),
                pawn   = P("Deckhand", AttackStyle.Slash, Locomotion.Walk, PlaceholderKind.Soldier, "#E8DCC0", "#5A4632", "#24386B", HatKind.Bandana, WeaponKind.Cutlass, 0.95f, false, false, "#C68A5C"),
            };
            t.black = new ArmyDefinition
            {
                name = "British Army", color = C("#B3201E"),
                king   = P("Governor", AttackStyle.Slash, Locomotion.Walk, PlaceholderKind.Soldier, "#B3201E", "#EFE9DC", "#D6A93A", HatKind.Bicorne, WeaponKind.Sword, 1.12f, false, true),
                queen  = P("Admiral", AttackStyle.Slash, Locomotion.Walk, PlaceholderKind.Soldier, "#1B2A4A", "#EFE9DC", "#D6A93A", HatKind.Bicorne, WeaponKind.Sword, 1.05f, false, true),
                rook   = P("Cannon", AttackStyle.Cannon, Locomotion.Roll, PlaceholderKind.Cannon, "#B3201E", "#EFE9DC", "#16130F", HatKind.Tricorne, WeaponKind.Rammer),
                bishop = P("Officer", AttackStyle.Slash, Locomotion.Walk, PlaceholderKind.Soldier, "#B3201E", "#EFE9DC", "#D6A93A", HatKind.Bicorne, WeaponKind.Sword, 1f, false, true),
                knight = P("Dragoon", AttackStyle.Charge, Locomotion.Gallop, PlaceholderKind.Rider, "#B3201E", "#EFE9DC", "#4A2E1C", HatKind.Helmet, WeaponKind.Sword),
                pawn   = P("Redcoat", AttackStyle.Shoot, Locomotion.Walk, PlaceholderKind.Soldier, "#B3201E", "#EFE9DC", "#16130F", HatKind.Tricorne, WeaponKind.Musket, 0.95f),
            };

            t.lightSquare = C("#E6D2A0");
            t.darkSquare = C("#2A8FA0");
            t.frame = C("#4A2F1A");
            t.background = C("#5FA8C9");
            t.ground = C("#D4BF8A");
            t.sunColor = C("#FFF1D0");
            t.sunIntensity = 1.4f;
            return t;
        }
    }
}
