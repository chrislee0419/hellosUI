using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using HMUI;

namespace HUI.HarmonyPatches
{
    [HarmonyPatch(typeof(TableViewScroller), "HandleJoystickWasNotCenteredThisFrame")]
    internal class TableViewScrollerHandleJoystickWasNotCenteredThisFramePatch
    {
        // reorder some assignments so the positionDidChangeEvent fires at the right time
        // this is needed to have the fast page up/down buttons change interactable state correctly
        // when using the joystick
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var targetMethod = typeof(Mathf).GetMethod(
                "Min",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(float), typeof(float) },
                null);

            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            for (int i = 0; i < code.Count; ++i)
            {
                var current = code[i];
                if (current.Calls(targetMethod))
                {
                    code[i + 4] = CodeInstruction.StoreField(typeof(TableViewScroller), "_targetPosition");
                    code[i + 6] = new CodeInstruction(OpCodes.Ldloc_0);
                    code[i + 7] = CodeInstruction.Call(typeof(TableViewScroller), "set_position");

                    code.RemoveAt(i + 8);

                    // note: we only need to change this for vertical lists
                    return code;
                }
            }

            // unable to find code
            Plugin.Log.Error($"Unable to patch {nameof(TableViewScroller)}:{nameof(TableViewScroller.HandleJoystickWasNotCenteredThisFrame)}");
            return instructions;
        }

        // original:
        // 0  call float32[UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32, float32)
        // 1  stloc.0
        // 2  ldarg.0
        // 3  ldloc.0
        // 4  call instance void HMUI.TableViewScroller::set_position(float32)
        // 5  ldarg.0
        // 6  ldarg.0
        // 7  call instance float32 HMUI.TableViewScroller::get_position()
        // 8  stfld float32 HMUI.TableViewScroller::_targetPosition

        // intended alteration:
        // 0  call float32[UnityEngine.CoreModule]UnityEngine.Mathf::Min(float32, float32)
        // 1  stloc.0
        // 2  ldarg.0
        // 3  ldloc.0
        // 4  stfld float32 HMUI.TableViewScroller::_targetPosition
        // 5  ldarg.0
        // 6  ldloc.0
        // 7  call instance void HMUI.TableViewScroller::set_position(float32)
    }
}
