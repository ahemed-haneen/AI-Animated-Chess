using UnityEngine;

namespace BattleChess
{
    /// <summary>Joint handles of a placeholder human (soldier, rider or gunner).</summary>
    public class HumanRig
    {
        public Transform root, hips, torso, head, legL, legR, armL, armR, gripR, muzzle;
        public bool seated;
        public WeaponKind weapon;
    }

    /// <summary>Joint handles of a whole placeholder piece.</summary>
    public class PlaceholderRig
    {
        public PlaceholderKind kind;
        public HumanRig human;          // soldier, rider or gunner
        public Transform horseBody;
        public Transform[] horseLegs;
        public Transform[] wheels;
        public Transform barrel;
        public Transform muzzle;        // where shots / cannonballs leave
        public Transform hull;
    }

    /// <summary>
    /// Builds simple primitive-based stand-in characters so the game is fully playable
    /// before real art exists. Replace them by assigning prefabs in the Theme asset.
    /// </summary>
    public static class PlaceholderFactory
    {
        static readonly Color Boot = new Color(0.09f, 0.08f, 0.07f);

        public static PlaceholderRig Build(PlaceholderLook look, Transform parent)
        {
            var rig = new PlaceholderRig { kind = look.kind };
            switch (look.kind)
            {
                case PlaceholderKind.Soldier:
                    rig.human = Human(look, parent, Vector3.zero, look.height, false);
                    rig.muzzle = rig.human.muzzle;
                    break;
                case PlaceholderKind.Rider:
                    BuildHorse(rig, look, parent);
                    rig.human = Human(look, rig.horseBody, new Vector3(0, 0.17f, -0.05f), 0.85f, true);
                    rig.muzzle = rig.human.muzzle;
                    break;
                case PlaceholderKind.Cannon:
                    BuildCannon(rig, parent);
                    var g = look; // gunner uses the army colours
                    rig.human = Human(g, parent, new Vector3(0, 0, -0.46f), 0.85f, false);
                    break;
                case PlaceholderKind.Ship:
                    BuildShip(rig, look, parent);
                    break;
            }
            return rig;
        }

        public static HumanRig Human(PlaceholderLook look, Transform parent, Vector3 pos, float size, bool seated)
        {
            var h = new HumanRig { seated = seated, weapon = look.weapon };
            var coat = Mats.Lit(look.coat, 0, 0.3f);
            var trous = Mats.Lit(look.trousers, 0, 0.25f);
            var skin = Mats.Lit(look.skin, 0, 0.35f);
            var acc = Mats.Lit(look.accent, 0, 0.35f);
            var boots = Mats.Lit(Boot, 0, 0.5f);
            var dark = Mats.Lit(new Color(0.1f, 0.07f, 0.05f), 0, 0.2f);

            h.root = Mats.Pivot("Human", parent, pos);
            h.root.localScale = Vector3.one * size;
            h.hips = Mats.Pivot("Hips", h.root, new Vector3(0, seated ? 0 : 0.5f, 0));

            Transform[] legs = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1 : 1;
                var leg = Mats.Pivot(i == 0 ? "LegL" : "LegR", h.hips, new Vector3(s * 0.08f, 0, 0));
                Mats.Prim(PrimitiveType.Capsule, leg, trous, new Vector3(0, -0.22f, 0), new Vector3(0.12f, 0.24f, 0.12f));
                Mats.Prim(PrimitiveType.Cube, leg, boots, new Vector3(0, -0.46f, 0.03f), new Vector3(0.11f, 0.07f, 0.18f));
                if (seated) leg.localEulerAngles = new Vector3(-70, 0, s * 28);
                legs[i] = leg;
            }
            h.legL = legs[0]; h.legR = legs[1];

            h.torso = Mats.Pivot("Torso", h.hips, Vector3.zero);
            Mats.Prim(PrimitiveType.Capsule, h.torso, coat, new Vector3(0, 0.22f, 0), new Vector3(0.32f, 0.24f, 0.22f));
            Mats.Prim(PrimitiveType.Cylinder, h.torso, coat, new Vector3(0, -0.02f, 0), new Vector3(0.31f, 0.08f, 0.23f));
            Mats.Prim(PrimitiveType.Cylinder, h.torso, acc, new Vector3(0, 0.07f, 0), new Vector3(0.325f, 0.025f, 0.24f));
            for (int i = 0; i < 3; i++) Mats.Prim(PrimitiveType.Sphere, h.torso, Mats.Gold, new Vector3(0, 0.16f + i * 0.08f, 0.11f), Vector3.one * 0.025f);

            h.head = Mats.Pivot("Head", h.torso, new Vector3(0, 0.52f, 0));
            Mats.Prim(PrimitiveType.Sphere, h.head, skin, Vector3.zero, new Vector3(0.18f, 0.2f, 0.18f));
            Mats.Prim(PrimitiveType.Sphere, h.head, dark, new Vector3(-0.04f, 0.02f, 0.083f), Vector3.one * 0.025f);
            Mats.Prim(PrimitiveType.Sphere, h.head, dark, new Vector3(0.04f, 0.02f, 0.083f), Vector3.one * 0.025f);
            Mats.Prim(PrimitiveType.Sphere, h.head, skin, new Vector3(0, -0.01f, 0.09f), new Vector3(0.035f, 0.045f, 0.04f));
            if (look.beard) Mats.Prim(PrimitiveType.Sphere, h.head, dark, new Vector3(0, -0.06f, 0.045f), new Vector3(0.15f, 0.12f, 0.11f));
            if (look.whiteWig) Mats.Prim(PrimitiveType.Sphere, h.head, Mats.Lit(new Color(0.93f, 0.92f, 0.9f)), new Vector3(0, 0.03f, -0.03f), new Vector3(0.2f, 0.19f, 0.17f));
            Hat(look, h.head);

            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1 : 1;
                var arm = Mats.Pivot(i == 0 ? "ArmL" : "ArmR", h.torso, new Vector3(s * 0.2f, 0.38f, 0));
                Mats.Prim(PrimitiveType.Capsule, arm, coat, new Vector3(0, -0.17f, 0), new Vector3(0.085f, 0.18f, 0.085f));
                Mats.Prim(PrimitiveType.Sphere, arm, skin, new Vector3(0, -0.37f, 0), Vector3.one * 0.075f);
                if (i == 0) h.armL = arm; else h.armR = arm;
            }
            h.gripR = Mats.Pivot("Grip", h.armR, new Vector3(0, -0.37f, 0));
            h.muzzle = Weapon(look.weapon, h.gripR);
            return h;
        }

        static void Hat(PlaceholderLook look, Transform head)
        {
            var black = Mats.Lit(new Color(0.08f, 0.07f, 0.06f), 0, 0.3f);
            var acc = Mats.Lit(look.accent, 0, 0.35f);
            switch (look.hat)
            {
                case HatKind.Tricorne:
                    Mats.Prim(PrimitiveType.Cylinder, head, black, new Vector3(0, 0.1f, 0), new Vector3(0.3f, 0.015f, 0.3f));
                    Mats.Prim(PrimitiveType.Cylinder, head, black, new Vector3(0, 0.13f, 0), new Vector3(0.17f, 0.04f, 0.17f));
                    Mats.Prim(PrimitiveType.Cylinder, head, Mats.Gold, new Vector3(0, 0.095f, 0), new Vector3(0.305f, 0.004f, 0.305f));
                    break;
                case HatKind.Bicorne:
                    Mats.Prim(PrimitiveType.Cube, head, black, new Vector3(0, 0.13f, 0), new Vector3(0.36f, 0.12f, 0.06f));
                    Mats.Prim(PrimitiveType.Cube, head, Mats.Gold, new Vector3(0, 0.075f, 0), new Vector3(0.37f, 0.015f, 0.065f));
                    break;
                case HatKind.Bandana:
                    Mats.Prim(PrimitiveType.Sphere, head, acc, new Vector3(0, 0.035f, -0.01f), new Vector3(0.195f, 0.16f, 0.195f));
                    break;
                case HatKind.FeatherHat:
                    Mats.Prim(PrimitiveType.Cylinder, head, black, new Vector3(0, 0.1f, 0), new Vector3(0.36f, 0.01f, 0.36f), new Vector3(0, 0, 8));
                    Mats.Prim(PrimitiveType.Cylinder, head, black, new Vector3(0, 0.13f, 0), new Vector3(0.17f, 0.045f, 0.17f));
                    Mats.Prim(PrimitiveType.Capsule, head, acc, new Vector3(-0.08f, 0.2f, -0.03f), new Vector3(0.03f, 0.1f, 0.03f), new Vector3(0, 0, 45));
                    break;
                case HatKind.Helmet:
                    Mats.Prim(PrimitiveType.Sphere, head, Mats.Lit(new Color(0.7f, 0.5f, 0.23f), 0.85f, 0.6f), new Vector3(0, 0.04f, -0.005f), new Vector3(0.205f, 0.19f, 0.215f));
                    Mats.Prim(PrimitiveType.Cube, head, black, new Vector3(0, 0.14f, -0.01f), new Vector3(0.03f, 0.08f, 0.2f));
                    break;
            }
        }

        /// <summary>Weapons extend along the grip's +Y. Returns the muzzle / tip transform.</summary>
        static Transform Weapon(WeaponKind w, Transform g)
        {
            float tip = 0.4f;
            switch (w)
            {
                case WeaponKind.Sword:
                    Mats.Prim(PrimitiveType.Cube, g, Mats.Gold, new Vector3(0, 0.03f, 0), new Vector3(0.12f, 0.02f, 0.03f));
                    Mats.Prim(PrimitiveType.Cube, g, Mats.Steel, new Vector3(0, 0.28f, 0), new Vector3(0.03f, 0.48f, 0.008f));
                    tip = 0.52f; break;
                case WeaponKind.Cutlass:
                    Mats.Prim(PrimitiveType.Sphere, g, Mats.Gold, new Vector3(0, 0.0f, 0), new Vector3(0.09f, 0.06f, 0.09f));
                    Mats.Prim(PrimitiveType.Cube, g, Mats.Steel, new Vector3(0, 0.17f, 0), new Vector3(0.05f, 0.28f, 0.008f));
                    Mats.Prim(PrimitiveType.Cube, g, Mats.Steel, new Vector3(0, 0.38f, 0.025f), new Vector3(0.05f, 0.18f, 0.008f), new Vector3(-18, 0, 0));
                    tip = 0.47f; break;
                case WeaponKind.Musket:
                    Mats.Prim(PrimitiveType.Cube, g, Mats.Wood, new Vector3(0, 0.15f, 0), new Vector3(0.04f, 0.55f, 0.06f));
                    Mats.Prim(PrimitiveType.Cylinder, g, Mats.Iron, new Vector3(0, 0.45f, 0.015f), new Vector3(0.022f, 0.25f, 0.022f));
                    Mats.Prim(PrimitiveType.Cube, g, Mats.Steel, new Vector3(0, 0.78f, 0.02f), new Vector3(0.012f, 0.14f, 0.012f));
                    tip = 0.7f; break;
                case WeaponKind.Pistol:
                    Mats.Prim(PrimitiveType.Cube, g, Mats.Wood, new Vector3(0, 0f, -0.01f), new Vector3(0.03f, 0.08f, 0.045f));
                    Mats.Prim(PrimitiveType.Cylinder, g, Mats.Iron, new Vector3(0, 0.09f, 0.01f), new Vector3(0.022f, 0.08f, 0.022f));
                    tip = 0.17f; break;
                case WeaponKind.Blunderbuss:
                    Mats.Prim(PrimitiveType.Cube, g, Mats.Wood, new Vector3(0, 0.05f, 0), new Vector3(0.045f, 0.3f, 0.06f));
                    Mats.Prim(PrimitiveType.Cylinder, g, Mats.Lit(new Color(0.69f, 0.48f, 0.23f), 0.85f, 0.6f), new Vector3(0, 0.3f, 0.01f), new Vector3(0.035f, 0.18f, 0.035f));
                    Mats.Prim(PrimitiveType.Cylinder, g, Mats.Lit(new Color(0.69f, 0.48f, 0.23f), 0.85f, 0.6f), new Vector3(0, 0.49f, 0.01f), new Vector3(0.07f, 0.03f, 0.07f));
                    tip = 0.5f; break;
                case WeaponKind.Rammer:
                    Mats.Prim(PrimitiveType.Cylinder, g, Mats.Wood, new Vector3(0, 0.25f, 0), new Vector3(0.02f, 0.38f, 0.02f));
                    Mats.Prim(PrimitiveType.Cylinder, g, Mats.Lit(new Color(0.23f, 0.16f, 0.1f)), new Vector3(0, 0.62f, 0), new Vector3(0.07f, 0.03f, 0.07f));
                    tip = 0.62f; break;
            }
            return Mats.Pivot("Muzzle", g, new Vector3(0, tip, 0.015f));
        }

        static void BuildHorse(PlaceholderRig rig, PlaceholderLook look, Transform parent)
        {
            var hide = Mats.Lit(look.accent, 0, 0.45f);
            var dark = Mats.Lit(new Color(0.08f, 0.06f, 0.05f), 0, 0.2f);
            rig.horseBody = Mats.Pivot("HorseBody", parent, new Vector3(0, 0.58f, 0));
            Mats.Prim(PrimitiveType.Capsule, rig.horseBody, hide, Vector3.zero, new Vector3(0.3f, 0.36f, 0.3f), new Vector3(90, 0, 0));
            Mats.Prim(PrimitiveType.Cylinder, rig.horseBody, hide, new Vector3(0, 0.2f, 0.32f), new Vector3(0.13f, 0.2f, 0.15f), new Vector3(35, 0, 0));
            Mats.Prim(PrimitiveType.Cube, rig.horseBody, hide, new Vector3(0, 0.36f, 0.48f), new Vector3(0.12f, 0.13f, 0.3f), new Vector3(40, 0, 0));
            Mats.Prim(PrimitiveType.Cube, rig.horseBody, dark, new Vector3(0, 0.28f, 0.26f), new Vector3(0.03f, 0.12f, 0.24f), new Vector3(35, 0, 0));
            Mats.Prim(PrimitiveType.Capsule, rig.horseBody, dark, new Vector3(0, -0.02f, -0.42f), new Vector3(0.06f, 0.18f, 0.06f), new Vector3(-30, 0, 0));
            Mats.Prim(PrimitiveType.Cube, rig.horseBody, Mats.Lit(look.coat), new Vector3(0, 0.14f, -0.04f), new Vector3(0.33f, 0.03f, 0.34f));
            rig.horseLegs = new Transform[4];
            int k = 0;
            for (int fz = 1; fz >= -1; fz -= 2)
                for (int fx = -1; fx <= 1; fx += 2)
                {
                    var leg = Mats.Pivot("HorseLeg", rig.horseBody, new Vector3(fx * 0.1f, -0.08f, fz * 0.27f));
                    Mats.Prim(PrimitiveType.Capsule, leg, hide, new Vector3(0, -0.22f, 0), new Vector3(0.075f, 0.24f, 0.075f));
                    Mats.Prim(PrimitiveType.Cylinder, leg, dark, new Vector3(0, -0.47f, 0), new Vector3(0.08f, 0.025f, 0.08f));
                    rig.horseLegs[k++] = leg;
                }
        }

        static void BuildCannon(PlaceholderRig rig, Transform parent)
        {
            var bronze = Mats.Lit(new Color(0.69f, 0.48f, 0.23f), 0.85f, 0.6f);
            Mats.Prim(PrimitiveType.Cube, parent, Mats.Wood, new Vector3(0, 0.18f, 0), new Vector3(0.22f, 0.1f, 0.44f));
            Mats.Prim(PrimitiveType.Cube, parent, Mats.Wood, new Vector3(0, 0.09f, -0.33f), new Vector3(0.12f, 0.05f, 0.36f), new Vector3(-18, 0, 0));
            rig.barrel = Mats.Pivot("Barrel", parent, new Vector3(0, 0.32f, 0.05f));
            Mats.Prim(PrimitiveType.Cylinder, rig.barrel, bronze, Vector3.zero, new Vector3(0.14f, 0.3f, 0.14f), new Vector3(82, 0, 0));
            rig.muzzle = Mats.Pivot("Muzzle", rig.barrel, new Vector3(0, 0.04f, 0.32f));
            rig.wheels = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1 : 1;
                var w = Mats.Pivot("Wheel", parent, new Vector3(s * 0.17f, 0.18f, 0.02f));
                Mats.Prim(PrimitiveType.Cylinder, w, Mats.Lit(new Color(0.29f, 0.19f, 0.1f)), Vector3.zero, new Vector3(0.36f, 0.02f, 0.36f), new Vector3(0, 0, 90));
                Mats.Prim(PrimitiveType.Cube, w, Mats.Iron, Vector3.zero, new Vector3(0.05f, 0.34f, 0.03f));
                Mats.Prim(PrimitiveType.Cube, w, Mats.Iron, Vector3.zero, new Vector3(0.05f, 0.03f, 0.34f));
                rig.wheels[i] = w;
            }
        }

        static void BuildShip(PlaceholderRig rig, PlaceholderLook look, Transform parent)
        {
            var hullM = Mats.Lit(look.coat, 0, 0.3f);
            var deck = Mats.Lit(new Color(0.6f, 0.45f, 0.28f), 0, 0.2f);
            var sail = Mats.Lit(look.trousers, 0, 0.1f);
            rig.hull = Mats.Pivot("Hull", parent, Vector3.zero);
            Mats.Prim(PrimitiveType.Cube, rig.hull, hullM, new Vector3(0, 0.14f, -0.05f), new Vector3(0.42f, 0.22f, 0.72f));
            Mats.Prim(PrimitiveType.Cube, rig.hull, hullM, new Vector3(0, 0.14f, 0.3f), new Vector3(0.3f, 0.22f, 0.3f), new Vector3(0, 45, 0));
            Mats.Prim(PrimitiveType.Cube, rig.hull, deck, new Vector3(0, 0.255f, -0.05f), new Vector3(0.38f, 0.01f, 0.68f));
            Mats.Prim(PrimitiveType.Cube, rig.hull, hullM, new Vector3(0, 0.33f, -0.33f), new Vector3(0.4f, 0.16f, 0.18f));
            Mats.Prim(PrimitiveType.Cube, rig.hull, Mats.Gold, new Vector3(0, 0.41f, -0.33f), new Vector3(0.41f, 0.015f, 0.19f));
            float[] mz = { 0.02f, 0.28f }; float[] mh = { 1.1f, 0.85f };
            for (int i = 0; i < 2; i++)
            {
                Mats.Prim(PrimitiveType.Cylinder, rig.hull, Mats.Wood, new Vector3(0, 0.25f + mh[i] / 2, mz[i]), new Vector3(0.035f, mh[i] / 2, 0.035f));
                Mats.Prim(PrimitiveType.Cube, rig.hull, sail, new Vector3(0, 0.25f + mh[i] * 0.45f, mz[i] + 0.03f), new Vector3(0.48f - i * 0.08f, 0.26f, 0.01f));
                Mats.Prim(PrimitiveType.Cube, rig.hull, sail, new Vector3(0, 0.25f + mh[i] * 0.78f, mz[i] + 0.03f), new Vector3(0.38f - i * 0.06f, 0.2f, 0.01f));
            }
            Mats.Prim(PrimitiveType.Cube, rig.hull, Mats.Lit(look.accent), new Vector3(0.09f, 1.4f, 0.02f), new Vector3(0.18f, 0.11f, 0.005f));
            Mats.Prim(PrimitiveType.Cylinder, rig.hull, Mats.Iron, new Vector3(0, 0.22f, 0.5f), new Vector3(0.05f, 0.06f, 0.05f), new Vector3(90, 0, 0));
            rig.muzzle = Mats.Pivot("Muzzle", rig.hull, new Vector3(0, 0.22f, 0.57f));
        }
    }
}
