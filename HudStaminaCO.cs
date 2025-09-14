using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace StaminaAddonCO
{
    public class HudStaminaCO : ModSystem
    {
        private ICoreClientAPI capi;
        private float stamina = 100f;
        private float maxStamina = 100f;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            // Канал для приёма данных от сервера
            capi.Network.RegisterChannel("staminaAction")
                .RegisterMessageType<StaminaPacket>()
                .SetMessageHandler<StaminaPacket>(OnStaminaUpdate);

            // Регистрируем рендерер HUD
            capi.Event.RegisterRenderer(new StaminaRenderer(capi, () => stamina, () => maxStamina), EnumRenderStage.Ortho);
        }

        private void OnStaminaUpdate(StaminaPacket packet)
        {
            stamina = packet.Value;
            maxStamina = packet.Max;
        }

        private class StaminaRenderer : IRenderer
        {
            private readonly ICoreClientAPI capi;
            private readonly Func<float> getStamina;
            private readonly Func<float> getMax;

            public double RenderOrder => 0;
            public int RenderRange => 1;

            public StaminaRenderer(ICoreClientAPI capi, Func<float> getStamina, Func<float> getMax)
            {
                this.capi = capi;
                this.getStamina = getStamina;
                this.getMax = getMax;
            }

            public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
            {
                if (stage != EnumRenderStage.Ortho) return;

                float cur = getStamina();
                float max = getMax();
                float percent = max > 0 ? cur / max : 0f;

                float x = capi.Render.FrameWidth / 2f - 100;
                float y = capi.Render.FrameHeight - 40;
                float width = 200f;
                float height = 10f;

                // фон (тёмно-серый)
                capi.Render.RenderRectangle(x, y, width, height, 0, ColorUtil.ToRgba(255, 50, 50, 50));

                // полоска (жёлтая)
                capi.Render.RenderRectangle(x, y, width * percent, height, 0, ColorUtil.ToRgba(255, 255, 255, 0));
            }

            public void Dispose() { }
        }
    }
}
