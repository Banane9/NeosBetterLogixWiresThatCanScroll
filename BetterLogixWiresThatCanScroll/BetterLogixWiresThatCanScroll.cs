using HarmonyLib;
using NeosModLoader;
using System;
using FrooxEngine;
using FrooxEngine.LogiX;
using BaseX;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

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
        public override string Version => "1.2.0";

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

                if (output)
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

        [HarmonyPatch(typeof(LogixTip))]
        private static class LogixTipPatch
        {
            [HarmonyPatch("CheckProxyCandidate")]
            [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
            private static void CheckProxyCandidate(LogixTip @this, Component proxy, float3 origin, ref Component closestProxy, ref float closestDistance)
            {
                throw new NotImplementedException("It's a reverse patch.");
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(LogixTip.OnPrimaryPress))]
            private static bool OnPrimaryPressPrefix(LogixTip __instance, ref Action ____impulseTarget, ref Impulse ____impulseSource, ref IDriverNode ____driver, ref IWorldElement ____output, ref IInputElement ____input, ref double ___lastPrimaryPress, ref float3 ___deletePoint, ref SegmentMesh ____deleteLine, ref float? ___deleteDistance)
            {
                var traverse = Traverse.Create(__instance);

                var flag = false;
                var list = (IEnumerable)traverse.Method("GetColliderHits").GetValue();

                Component closestProxy = null;
                float closestDistance = float.MaxValue;

                foreach (var item in list)
                {
                    var itemTraverse = Traverse.Create(item);
                    var slot = (Slot)itemTraverse.Field("collider").Property("Slot").GetValue();
                    var origin = (float3)itemTraverse.Field("origin").GetValue();

                    InputProxy component = slot.GetComponent<InputProxy>();
                    if (component != null)
                    {
                        CheckProxyCandidate(__instance, component, origin, ref closestProxy, ref closestDistance);
                    }

                    OutputProxy component2 = slot.GetComponent<OutputProxy>();
                    if (component2 != null)
                    {
                        CheckProxyCandidate(__instance, component2, origin, ref closestProxy, ref closestDistance);
                    }

                    MemberProxy component3 = slot.GetComponent<MemberProxy>();
                    if (component3 != null)
                    {
                        CheckProxyCandidate(__instance, component3, origin, ref closestProxy, ref closestDistance);
                    }

                    DriveProxy component4 = slot.GetComponent<DriveProxy>();
                    if (component4 != null)
                    {
                        CheckProxyCandidate(__instance, component4, origin, ref closestProxy, ref closestDistance);
                    }

                    ImpulseSourceProxy component5 = slot.GetComponent<ImpulseSourceProxy>();
                    if (component5 != null)
                    {
                        CheckProxyCandidate(__instance, component5, origin, ref closestProxy, ref closestDistance);
                    }

                    ImpulseTargetProxy component6 = slot.GetComponent<ImpulseTargetProxy>();
                    if (component6 != null)
                    {
                        CheckProxyCandidate(__instance, component6, origin, ref closestProxy, ref closestDistance);
                    }
                }

                Traverse.Create(typeof(Pool)).Method("Return", new object[] { list });

                Type type = null;
                if (!(closestProxy is InputProxy inputProxy))
                {
                    if (!(closestProxy is OutputProxy outputProxy))
                    {
                        if (!(closestProxy is MemberProxy memberProxy))
                        {
                            if (!(closestProxy is DriveProxy driveProxy))
                            {
                                if (!(closestProxy is ImpulseSourceProxy impulseSourceProxy))
                                {
                                    if (closestProxy is ImpulseTargetProxy impulseTargetProxy)
                                    {
                                        ____impulseTarget = impulseTargetProxy.ImpulseTarget.Target;
                                        Slot connectionPoint = LogixHelper.GetConnectionPoint((IWorldElement)____impulseTarget.Target, ____impulseTarget.Method.Name);
                                        SetupTempWire(__instance, connectionPoint, typeof(Action));
                                        flag = true;
                                    }
                                }
                                else
                                {
                                    ____impulseSource = impulseSourceProxy.ImpulseSource.Target;
                                    Slot connectionPoint = LogixHelper.GetConnectionPoint(____impulseSource);
                                    SetupTempWire(__instance, connectionPoint, typeof(Action), true);
                                    flag = true;
                                }
                            }
                            else
                            {
                                ____driver = driveProxy.Drive.Target.FindNearestParent<IDriverNode>();
                                Slot connectionPoint = LogixHelper.GetConnectionPoint(____driver);
                                SetupTempWire(__instance, connectionPoint, ____driver.DriveType, output: true);
                                flag = true;
                            }
                        }
                        else
                        {
                            ____output = memberProxy.Member.Target;
                            IWorldElement output = ____output;
                            type = ((output is ISyncRef syncRef) ? syncRef.TargetType : ((!(output is IField field)) ? ____output.GetType() : field.ValueType));
                            if (____output != null)
                            {
                                Slot connectionPoint = LogixHelper.GetConnectionPoint(____output);
                                SetupTempWire(__instance, connectionPoint, type, output: true);
                                flag = true;
                            }
                        }
                    }
                    else
                    {
                        var output = outputProxy.OutputField.Target;
                        ____output = output;
                        type = outputProxy.OutputField.Target.OutputType;

                        if (____output != null)
                        {
                            Slot connectionPoint = LogixHelper.GetConnectionPoint(____output);
                            SetupTempWire(__instance, connectionPoint, type, LogixHelper.GetSide(output.OwnerNode, output) == ConnectPointSide.Output);
                            flag = true;
                        }
                    }
                }
                else
                {
                    ____input = inputProxy.InputField.Target;
                    Slot connectionPoint = LogixHelper.GetConnectionPoint(____input);
                    SetupTempWire(__instance, connectionPoint, ____input.InputType, LogixHelper.GetSide(____input.OwnerNode, ____input) == ConnectPointSide.Output);
                    flag = true;
                }

                var activeTool = __instance.ActiveTool;
                if (activeTool != null && (activeTool.Laser.IsTouching || activeTool.IsHoldingObjectsWithLaser))
                    return false;

                if (__instance.Time.WorldTime - ___lastPrimaryPress < 0.25)
                {
                    ___lastPrimaryPress = -1.0;
                    SpawnNode(__instance);
                    return false;
                }

                ___lastPrimaryPress = __instance.Time.WorldTime;

                if (!flag)
                {
                    ___deletePoint = (float3)traverse.Property("DeletePointReference").GetValue();

                    var attachedModel = __instance.Slot.AddSlot("DeleteLine").AttachMesh<SegmentMesh, UnlitMaterial>();
                    attachedModel.material.BlendMode.Value = BlendMode.Alpha;
                    attachedModel.material.TintColor.Value = new color(1f, 0f, 0f, 0.5f);
                    attachedModel.material.ZWrite.Value = ZWrite.On;
                    attachedModel.mesh.Radius.Value = 0.002f;
                    attachedModel.mesh.HighPriorityIntegration.Value = true;

                    ____deleteLine = attachedModel.mesh;
                    ___deleteDistance = null;
                }

                return false;
            }

            [HarmonyPatch("SetupTempWire")]
            [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
            private static void SetupTempWire(LogixTip @this, Slot connectionPoint, Type type, bool output = false)
            {
                throw new NotImplementedException("It's a reverse patch.");
            }

            [HarmonyPatch("SpawnNode")]
            [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
            private static void SpawnNode(LogixTip @this)
            {
                throw new NotImplementedException("It's a reverse patch.");
            }
        }
    }
}