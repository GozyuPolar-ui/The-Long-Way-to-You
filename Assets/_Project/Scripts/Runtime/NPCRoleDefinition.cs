using System;
using UnityEngine;

namespace Prince
{
    public enum NPCOutfitPiece
    {
        Hair,
        Torso,
        Tunic,
        Pants,
        Shorts,
        Boots,
        Gloves,
        Bracers,
        Helmet,
        Bags
    }

    public enum NPCBodyVariant
    {
        Body01,
        Body02
    }

    [Serializable]
    public struct NPCOptionalOutfitPiece
    {
        public NPCOutfitPiece piece;
        [Range(0f, 1f)] public float chance;
    }

    [CreateAssetMenu(menuName = "Prince/NPC Role Definition", fileName = "NPC_Role")]
    public sealed class NPCRoleDefinition : ScriptableObject
    {
        public string roleName;
        public NPCOutfitPiece[] requiredPieces = Array.Empty<NPCOutfitPiece>();
        public NPCOptionalOutfitPiece[] optionalPieces = Array.Empty<NPCOptionalOutfitPiece>();
        public NPCOutfitPiece[] topChoices = Array.Empty<NPCOutfitPiece>();
        public NPCOutfitPiece[] bottomChoices = Array.Empty<NPCOutfitPiece>();
        public NPCBodyVariant[] bodyChoices = { NPCBodyVariant.Body01 };
        public Color[] clothingPalette = { Color.white };
        public Color[] skinPalette = { Color.white };
        public Color[] hairPalette = { Color.white };
        public Vector2 scaleRange = new Vector2(0.95f, 1.05f);
        public AnimationClip[] idleClips = Array.Empty<AnimationClip>();
        public AnimationClip[] dialogueClips = Array.Empty<AnimationClip>();
        public bool lookAtPlayer = true;
    }
}
