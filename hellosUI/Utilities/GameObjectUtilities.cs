using UnityEngine;
using HMUI;
using VRUIControls;
using IPA.Utilities;

namespace HUI.Utilities
{
    public static class GameObjectUtilities
    {
        public static void FixRaycaster(this GameObject go, PhysicsRaycasterWithCache physicsRaycaster)
        {
            // fix for null exceptions during raycast
            // mostly intended for FloatingScreens
            // most likely, what happens is the BSML object is created too "early", so BSML can't find PhysicsRaycaster
            var vrGraphicRaycaster = go.GetComponent<VRGraphicRaycaster>();
            if (vrGraphicRaycaster == null)
                vrGraphicRaycaster = go.GetComponentInChildren<VRGraphicRaycaster>();

            FieldAccessor<VRGraphicRaycaster, PhysicsRaycasterWithCache>.Set(ref vrGraphicRaycaster, "_physicsRaycaster", physicsRaycaster);
        }

        public static void SetSkew(this ImageView image, float skew)
        {
            FieldAccessor<ImageView, float>.Set(ref image, "_skew", skew);
            image.SetVerticesDirty();
        }
    }
}
