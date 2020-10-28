using UnityEngine;
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
            FieldAccessor<VRGraphicRaycaster, PhysicsRaycasterWithCache>.Set(ref vrGraphicRaycaster, "_physicsRaycaster", physicsRaycaster);
        }
    }
}
