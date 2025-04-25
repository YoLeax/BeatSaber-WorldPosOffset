using HarmonyLib;
using UnityEngine;
using UnityEngine.XR;

namespace WorldPosOffset;

[HarmonyPatch(typeof(VRController), "Update")]
internal class VRControllerUpdatePatch
{
    private static void Postfix(VRController __instance)
    {
        PluginConfig? cfg = PluginConfig.Instance;
        if (cfg == null) return;
        if (!cfg.Enabled) return;
        
        switch (__instance.node)
        {
            case XRNode.LeftHand:
                __instance.transform.Translate(cfg.CurPreset.LeftOffset, Space.World);
                break;
            case XRNode.RightHand:
                __instance.transform.Translate(cfg.CurPreset.RightOffset, Space.World);
                break;
        }
    }
}