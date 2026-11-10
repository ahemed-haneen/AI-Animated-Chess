using System;
using UnityEngine;
using BattleChess.Core;

namespace BattleChess
{
    public enum AttackStyle { Slash, Thrust, Smash, Shoot, Cannon, Charge }
    public enum Locomotion { Walk, Gallop, Roll, Sail }
    public enum PlaceholderKind { Soldier, Rider, Cannon, Ship }
    public enum HatKind { None, Tricorne, Bicorne, Bandana, FeatherHat, Helmet }
    public enum WeaponKind { None, Sword, Cutlass, Musket, Pistol, Blunderbuss, Rammer }

    /// <summary>Settings for the built-in placeholder figure used until real art is assigned.</summary>
    [Serializable]
    public class PlaceholderLook
    {
        public PlaceholderKind kind = PlaceholderKind.Soldier;
        public Color coat = new Color(0.7f, 0.12f, 0.1f);
        public Color trousers = new Color(0.93f, 0.91f, 0.86f);
        public Color skin = new Color(0.93f, 0.76f, 0.62f);
        public Color accent = new Color(0.09f, 0.08f, 0.07f);
        public HatKind hat = HatKind.None;
        public WeaponKind weapon = WeaponKind.Sword;
        public float height = 1f;
        public bool beard;
        public bool whiteWig;
    }

    /// <summary>Everything the game needs to know about one piece type of one army.</summary>
    [Serializable]
    public class PieceDefinition
    {
        public string displayName = "Piece";
        [Tooltip("Rigged character prefab with an Animator using a Battle Chess controller (see 'Battle Chess > Make Character From Selected Model'). Leave empty to use the placeholder.")]
        public GameObject prefab;
        public float prefabScale = 1f;
        public AttackStyle attack = AttackStyle.Slash;
        public Locomotion locomotion = Locomotion.Walk;
        [Tooltip("When the blow lands, as a fraction of the attack clip. Ignored if the clip has an 'Impact' animation event.")]
        [Range(0.05f, 0.95f)] public float impactAt = 0.45f;
        public PlaceholderLook placeholder = new PlaceholderLook();
        [Header("Optional audio / VFX overrides")]
        public AudioClip attackSound;
        public AudioClip impactSound;
        public AudioClip deathSound;
        public GameObject impactVfx;
        public GameObject muzzleVfx;

        public bool IsRanged { get { return attack == AttackStyle.Shoot || attack == AttackStyle.Cannon; } }
    }

    [Serializable]
    public class ArmyDefinition
    {
        public string name = "Army";
        public Color color = Color.white;
        public PieceDefinition king = new PieceDefinition();
        public PieceDefinition queen = new PieceDefinition();
        public PieceDefinition rook = new PieceDefinition();
        public PieceDefinition bishop = new PieceDefinition();
        public PieceDefinition knight = new PieceDefinition();
        public PieceDefinition pawn = new PieceDefinition();

        public PieceDefinition Get(PType t)
        {
            switch (t)
            {
                case PType.K: return king;
                case PType.Q: return queen;
                case PType.R: return rook;
                case PType.B: return bishop;
                case PType.N: return knight;
                default: return pawn;
            }
        }
    }

    [CreateAssetMenu(menuName = "Battle Chess/Theme", fileName = "NewTheme")]
    public class ThemeDefinition : ScriptableObject
    {
        public string title = "Battle Chess";
        [Tooltip("White moves first.")]
        public ArmyDefinition white = new ArmyDefinition();
        public ArmyDefinition black = new ArmyDefinition();
        [Header("Battlefield")]
        public Color lightSquare = new Color(0.9f, 0.82f, 0.63f);
        public Color darkSquare = new Color(0.16f, 0.53f, 0.59f);
        public Color frame = new Color(0.29f, 0.18f, 0.1f);
        public Color background = new Color(0.37f, 0.66f, 0.79f);
        public Color ground = new Color(0.83f, 0.75f, 0.54f);
        public Color sunColor = new Color(1f, 0.95f, 0.85f);
        public float sunIntensity = 1.3f;

        public ArmyDefinition Army(bool isWhite) { return isWhite ? white : black; }
        public PieceDefinition Get(PType t, bool isWhite) { return Army(isWhite).Get(t); }
    }
}
