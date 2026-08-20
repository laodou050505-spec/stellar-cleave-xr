using UnityEngine;

namespace CosmicSlice
{
    public enum HandSide
    {
        Left,
        Right
    }

    public enum TargetKind
    {
        Plasma,
        Meteor,
        Quantum,
        Bomb
    }

    /// <summary>
    /// Authoritative authored poses for the two isolated launch phases. Desktop and XR both
    /// derive their pose from the active phase, including reset, rather than maintaining
    /// unrelated camera layouts.
    /// </summary>
    public static class CosmicSliceDesign
    {
        public static readonly Vector3 TitlePlayerRootPosition = new(0f, 0f, -4.65f);
        public const float TitlePlayerYaw = 0f;
        public const float TitleEyeHeight = 1.65f;

        // The gameplay phase intentionally begins from the same authored viewing landmark as
        // the launch phase. It remains a separate authority so either phase can evolve without
        // reintroducing a hidden second camera start.
        public static readonly Vector3 GameplayPlayerRootPosition = new(0f, 0f, -4.65f);
        public const float GameplayPlayerYaw = 0f;
        public const float GameplayEyeHeight = 1.65f;

        // The portal and falling targets live above this actual room-space arena, not on a camera canvas.
        public static readonly Vector3 ArenaOrigin = new(0f, 0f, 0.55f);

        // A walk-up mission console: 3 m from the authored eye position, at comfortable eye height.
        public static readonly Vector3 TitleMissionConsolePosition = new(0f, 1.72f, -1.65f);
        // Gameplay status remains a walk-up world object at the arena edge; it must not become
        // a screen placed between the player and the floating targets / portal.
        public static readonly Vector3 GameplayMissionConsolePosition = new(3.25f, 1.48f, 0.35f);
        public const float MissionConsoleScale = 0.00175f;
        public const float PortalHeightAboveArena = 3.10f;
    }

    /// <summary>Physical option target mounted on the world-space mission console.</summary>
    public sealed class CosmicWorldMenuOption : MonoBehaviour
    {
        public int Selection { get; private set; }

        public void Configure(int selection)
        {
            Selection = selection;
        }
    }

    public static class CosmicPalette
    {
        public static readonly Color Cyan = new(0.06f, 0.86f, 1f, 1f);
        public static readonly Color Amber = new(1f, 0.55f, 0.12f, 1f);
        public static readonly Color Violet = new(0.67f, 0.26f, 1f, 1f);
        public static readonly Color Red = new(1f, 0.12f, 0.18f, 1f);
        public static readonly Color Ink = new(0.012f, 0.018f, 0.025f, 1f);
        public static readonly Color Silver = new(0.72f, 0.79f, 0.84f, 1f);

        public static Color For(TargetKind kind)
        {
            return kind switch
            {
                TargetKind.Plasma => Cyan,
                TargetKind.Meteor => Amber,
                TargetKind.Quantum => Violet,
                _ => Red
            };
        }
    }
}
