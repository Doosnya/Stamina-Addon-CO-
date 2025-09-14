using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace StaminaAddonCO
{
    public class ModSystemStaminaCO : ModSystem
    {
        private ICoreServerAPI sapi;
        private const string PacketKey = "staminaAction";

        // параметры стамины
        private const float MaxStamina = 100f;
        private const float AttackCost = 5f;
        private const float BlockCost = 3f;
        private const float RegenPerSecond = 12f; // ~100 за 8-9 сек
        private const float RegenDelay = 5f;      // пауза перед регеном

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api ?? throw new ArgumentNullException(nameof(api));

            // канал только для рассылки стамины клиентам
            sapi.Network.RegisterChannel(PacketKey)
                .RegisterMessageType<StaminaPacket>();

            // Подписка на вход игроков
            sapi.Event.PlayerJoin += OnPlayerJoin;

            // Подписка на взаимодействие с сущностями (удары/блоки)
            sapi.Event.OnPlayerInteractEntity += OnPlayerInteractEntityHandler;

            // Тик для регена
            sapi.Event.RegisterGameTickListener(OnServerTick, 100);
        }

        private void OnPlayerJoin(IServerPlayer player)
        {
            // при входе проинициализируем стамину
            player.Entity.WatchedAttributes.SetFloat("stamina", MaxStamina);
            player.Entity.WatchedAttributes.SetFloat("lastStaminaUse", 0f);
            SendStaminaUpdate(player, MaxStamina);
        }

        // обработчик взаимодействия игрока с сущностью (удар, блок)
        private void OnPlayerInteractEntityHandler(
            Entity entity,
            IPlayer byPlayer,
            ItemSlot slot,
            Vec3d hitPosition,
            int mode,
            ref EnumHandling handling)
        {
            var serverPlayer = byPlayer as IServerPlayer;
            if (serverPlayer == null) return;

            // Снимаем стамину за взаимодействие (удар/блок)
            DrainStamina(serverPlayer, AttackCost);

            // Если это был «попадание», можешь сразу добавить возврат:
            AddStamina(serverPlayer, AttackCost + 4f);
        }

        private void DrainStamina(IServerPlayer player, float amount)
        {
            var attr = player.Entity.WatchedAttributes;
            float cur = attr.GetFloat("stamina", MaxStamina);
            float newVal = Math.Max(0f, cur - amount);

            attr.SetFloat("stamina", newVal);
            attr.SetFloat("lastStaminaUse", (float)sapi.World.ElapsedMilliseconds / 1000f);

            // выбрасываем оружие, если стамина = 0
            if (newVal <= 0f)
            {
                var slot = player.InventoryManager.ActiveHotbarSlot;
                if (!slot.Empty)
                {
                    sapi.World.SpawnItemEntity(slot.TakeOutWhole(), player.Entity.ServerPos.XYZ);
                }
            }

            SendStaminaUpdate(player, newVal);
        }

        private void AddStamina(IServerPlayer player, float amount)
        {
            var attr = player.Entity.WatchedAttributes;
            float cur = attr.GetFloat("stamina", MaxStamina);
            float newVal = Math.Min(MaxStamina, cur + amount);

            attr.SetFloat("stamina", newVal);
            SendStaminaUpdate(player, newVal);
        }

        private void OnServerTick(float dt)
        {
            foreach (var p in sapi.World.AllOnlinePlayers)
            {
                var player = p as IServerPlayer;
                if (player == null) continue;

                var attr = player.Entity.WatchedAttributes;
                float stamina = attr.GetFloat("stamina", MaxStamina);
                float lastUse = attr.GetFloat("lastStaminaUse", 0);

                if (stamina < MaxStamina &&
                    (sapi.World.ElapsedMilliseconds / 1000f) - lastUse >= RegenDelay)
                {
                    stamina = Math.Min(MaxStamina, stamina + RegenPerSecond * dt);
                    attr.SetFloat("stamina", stamina);
                    SendStaminaUpdate(player, stamina);
                }
            }
        }

        private void SendStaminaUpdate(IServerPlayer player, float stamina)
        {
            sapi.Network.GetChannel(PacketKey).SendPacket(new StaminaPacket()
            {
                PlayerUid = player.PlayerUID,
                Value = stamina,
                Max = MaxStamina
            }, player);
        }
    }
}
