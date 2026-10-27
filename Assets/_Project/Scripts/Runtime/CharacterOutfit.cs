using UnityEngine;

namespace Prince
{
    public sealed class CharacterOutfit : MonoBehaviour
    {
        public bool Body = true;
        public bool Head = true;
        public bool Hair = true;
        public bool Torso;
        public bool Tunic = true;
        public bool Pants = true;
        public bool Shorts;
        public bool Boots = true;
        public bool Gloves = true;
        public bool Bracers;
        public bool Helmet;
        public bool Bags;

        [HideInInspector] public GameObject BodyObject;
        [HideInInspector] public GameObject HeadObject;
        [HideInInspector] public GameObject HairObject;
        [HideInInspector] public GameObject TorsoObject;
        [HideInInspector] public GameObject TunicObject;
        [HideInInspector] public GameObject PantsObject;
        [HideInInspector] public GameObject ShortsObject;
        [HideInInspector] public GameObject BootsObject;
        [HideInInspector] public GameObject GlovesObject;
        [HideInInspector] public GameObject BracersObject;
        [HideInInspector] public GameObject HelmetObject;
        [HideInInspector] public GameObject BagsObject;

        private void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            SetActive(BodyObject, Body);
            SetActive(HeadObject, Head);
            SetActive(HairObject, Hair);
            SetActive(TorsoObject, Torso);
            SetActive(TunicObject, Tunic);
            SetActive(PantsObject, Pants);
            SetActive(ShortsObject, Shorts);
            SetActive(BootsObject, Boots);
            SetActive(GlovesObject, Gloves);
            SetActive(BracersObject, Bracers);
            SetActive(HelmetObject, Helmet);
            SetActive(BagsObject, Bags);
        }

        private static void SetActive(GameObject piece, bool active)
        {
            if (piece != null && piece.activeSelf != active)
                piece.SetActive(active);
        }
    }
}
