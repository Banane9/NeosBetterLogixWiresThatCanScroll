using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseX;
using FrooxEngine;
using FrooxEngine.LogiX;

namespace BetterLogixWiresThatCanScroll
{
    internal static class Helpers
    {
        private static readonly Type impulseType = typeof(Impulse);

        private static bool animateWires => BetterLogixWiresThatCanScroll.Config.GetValue(BetterLogixWiresThatCanScroll.AnimateWires);

        public static void CleanList(this ISyncList list)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                ISyncMember syncMember = list.GetElement(i);

                if ((syncMember as FieldDrive<float2>)?.Target == null)
                {
                    list.RemoveElement(i);
                }
            }
        }

        public static FresnelMaterial GetWireMaterial(this ConnectionWire instance, color color, int dimensions, bool isImpulse)
        {
            Slot LogixAssets = instance.World.AssetsSlot.FindOrAdd("LogixAssets");

            // Use extended key for static material to be backwards compatible with old version
            var key = $"Logix_WireMaterial_{color}_{(isImpulse ? "Impulse" : "Value")}_{dimensions}{(animateWires ? "" : "_Static")}";

            var fresnelMaterial = (FresnelMaterial)instance.World.KeyOwner(key);
            if (fresnelMaterial == null)
            {
                fresnelMaterial = LogixAssets.AttachComponent<FresnelMaterial>();
                fresnelMaterial.AssignKey(key, 2);
                fresnelMaterial.BlendMode.Value = BlendMode.Alpha;
                fresnelMaterial.ZWrite.Value = ZWrite.On;
                fresnelMaterial.Sidedness.Value = Sidedness.Double;

                var wireTexture = LogixHelper.GetWireTexture(instance.World, dimensions, isImpulse);
                wireTexture.WrapModeU.Value = TextureWrapMode.Repeat;

                fresnelMaterial.NearTexture.Target = wireTexture;
                fresnelMaterial.FarTexture.Target = wireTexture;

                var value = new float2(0.5f, 1f);
                fresnelMaterial.NearTextureScale.Value = value;
                fresnelMaterial.FarTextureScale.Value = value;

                fresnelMaterial.NearColor.Value = color.MulA(.8f);
                fresnelMaterial.FarColor.Value = color.MulRGB(.5f).MulA(.8f);
            }

            if (!animateWires)
                return fresnelMaterial;

            ValueMultiDriver<float2> multiDriver;
            var pannerKey = $"Logix_WirePanner_{(isImpulse ? "Impulse" : "Value")}";

            var panner = (Panner2D)instance.World.KeyOwner(pannerKey);
            if (panner == null)
            {
                panner = LogixAssets.AttachComponent<Panner2D>();
                panner.Speed = new float2(isImpulse ? -1 : 1, 0);
                panner.AssignKey(pannerKey, 2);

                multiDriver = LogixAssets.AttachComponent<ValueMultiDriver<float2>>();
                panner.Target = multiDriver.Value;
            }
            else
            {
                multiDriver = panner?.Target?.Parent as ValueMultiDriver<float2>;
                if (multiDriver == null)
                {
                    multiDriver = LogixAssets.AttachComponent<ValueMultiDriver<float2>>();
                    panner.Target = multiDriver.Value;
                }
            }

            ISyncList listOfDrives = multiDriver.Drives;
            listOfDrives.CleanList();

            if (fresnelMaterial.NearTextureOffset.IsDriven || fresnelMaterial.NearTextureOffset.IsLinked)
            {
                if ((fresnelMaterial.NearTextureOffset.ActiveLink as SyncElement)?.Component != multiDriver)
                {
                    ((FieldDrive<float2>)listOfDrives.AddElement()).ForceLink(fresnelMaterial.NearTextureOffset);
                }
            }
            else
                ((FieldDrive<float2>)listOfDrives.AddElement()).Target = fresnelMaterial.NearTextureOffset;

            return fresnelMaterial;
        }

        public static Type GetWireType(this Type inputType)
        {
            return inputType.IsGenericType ? inputType.GetGenericArguments()[0] : inputType;
        }

        public static bool IsImpulse(this Type wireType)
        {
            return wireType == impulseType;
        }
    }
}