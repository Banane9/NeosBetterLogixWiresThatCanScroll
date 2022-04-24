using HarmonyLib;
using NeosModLoader;
using System;
using FrooxEngine;
using FrooxEngine.LogiX;
using BaseX;
using System.Linq;

namespace BetterLogixWiresThatCanScroll
{
    public class BetterLogixWiresThatCanScroll : NeosMod
    {
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> AnimateWires = new ModConfigurationKey<bool>("AnimateWires", "Make the arrows on LogiX wires scroll.", () => false);

        public static ModConfiguration Config;
        public override string Author => "Banane9";
        public override string Link => "https://github.com/Banane9/NeosBetterLogixWiresThatCanScroll";
        public override string Name => "BetterLogixWiresThatCanScroll";
        public override string Version => "1.1.1";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony($"{Author}.{Name}");
            Config = GetConfiguration();
            Config.Save(true);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(ConnectionWire))]
        private static class BetterLogixWiresThatScrollPatch
        {
            private static readonly Type outputAttribute = typeof(AsOutput);

            [HarmonyPrefix]
            [HarmonyPatch("DeleteHighlight")]
            private static bool DeleteHighlightPrefix(SyncRef<FresnelMaterial> ___Material, SyncRef<Slot> ___WireSlot, ConnectionWire __instance)
            {
                Type wireType = __instance.InputField.Target.GetType().GetWireType();

                ___Material.Target = __instance.GetWireMaterial(color.Red, wireType.GetDimensions(), wireType.IsImpulse());
                ___WireSlot.Target.GetComponent<MeshRenderer>().Materials[0] = ___Material.Target;

                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch("OnAttach")]
            private static bool OnAttachPrefix(SyncRef<Slot> ___WireSlot, FieldDrive<float3> ___WirePoint, FieldDrive<float3> ___WireTangent, FieldDrive<floatQ> ___WireOrientation, FieldDrive<float> ___WireWidth, ConnectionWire __instance)
            {
                ___WireSlot.Target = __instance.Slot.AddSlot("Wire");

                StripeWireMesh stripeWireMesh = ___WireSlot.Target.AttachComponent<StripeWireMesh>();
                stripeWireMesh.Orientation0.Value = floatQ.Euler(0f, 0f, -90f);

                SyncField<float3> tangent = stripeWireMesh.Tangent0;
                float3 left = float3.Left;
                tangent.Value = (left) * 0.25f;

                stripeWireMesh.Width0.Value = 0.025600001f;
                ___WirePoint.Target = stripeWireMesh.Point1;
                ___WireTangent.Target = stripeWireMesh.Tangent1;
                ___WireOrientation.Target = stripeWireMesh.Orientation1;
                ___WireWidth.Target = stripeWireMesh.Width1;

                MeshCollider meshCollider = ___WireSlot.Target.AttachComponent<MeshCollider>();
                meshCollider.Mesh.Target = stripeWireMesh;
                meshCollider.Sidedness.Value = MeshColliderSidedness.DualSided;

                ___WireSlot.Target.AttachComponent<SearchBlock>();
                ___WireSlot.Target.ActiveSelf = false;
                ___WireSlot.Target.AttachComponent<MeshRenderer>().Mesh.Target = stripeWireMesh;

                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch("SetTypeColor")]
            private static bool SetTypeColorPrefix(SyncRef<FresnelMaterial> ___Material, Sync<color> ___TypeColor, SyncRef<Slot> ___WireSlot, ConnectionWire __instance)
            {
                Type wireType = __instance.InputField.Target.GetType().GetWireType();

                ___Material.Target = __instance.GetWireMaterial(___TypeColor, wireType.GetDimensions(), wireType.IsImpulse());
                ___WireSlot.Target.GetComponent<MeshRenderer>().Materials[0] = ___Material.Target;

                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch("SetupStyle")]
            private static bool SetupStylePrefix(color color, int dimensions, bool isImpulse, Sync<color> ___TypeColor, SyncRef<FresnelMaterial> ___Material, SyncRef<Slot> ___WireSlot, ConnectionWire __instance)
            {
                ___TypeColor.Value = color;
                ___Material.Target = __instance.GetWireMaterial(color, dimensions, isImpulse);
                ___WireSlot.Target.GetComponent<MeshRenderer>().Materials.Add(___Material.Target);

                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch("SetupTempWire")]
            private static bool SetupTempWirePrefix(Slot targetPoint, bool output, Sync<bool> ___TempWire, SyncRef<FresnelMaterial> ___Material, SyncRef<Slot> ___WireSlot, Sync<color> ___TypeColor, ConnectionWire __instance)
            {
                ___TempWire.Value = true;
                __instance.TargetSlot.Target = targetPoint;
                ___WireSlot.Target.ActiveSelf = true;

                ___Material.Target = ___WireSlot.Target.DuplicateComponent(___Material.Target);

                var value = new float2(0, 1);
                ___Material.Target.FarTextureScale.Value = value;
                ___Material.Target.NearTextureScale.Value = value;

                ___WireSlot.Target.GetComponent<MeshRenderer>().Materials[0] = ___Material.Target;
                ___WireSlot.Target.GetComponent<MeshCollider>()?.Destroy();

                var input = __instance.Slot.GetComponentInParents<InputProxy>()?.InputField.Target;
                var inputParent = input?.FindNearestParent<LogixNode>();

                if (output || (input != null && Enumerable.Range(0, inputParent.SyncMemberCount)
                                        .Select(i => inputParent.GetSyncMemberFieldInfo(i))
                                        .Where(field => field.GetCustomAttributes(outputAttribute, false).Any())
                                        .Any(field => field.GetValue(inputParent) == input)))
                    __instance.SetupAsOutput();

                if (Config.GetValue(AnimateWires))
                {
                    Panner2D panner = ___WireSlot.Target.AttachComponent<Panner2D>();
                    panner.Speed = new float2(___TypeColor == color.White ? -1 : 1, 0);
                    panner.Target = ___Material.Target.NearTextureOffset;
                }

                return false;
            }
        }
    }
}