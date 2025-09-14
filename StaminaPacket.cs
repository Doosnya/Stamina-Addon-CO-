using System;
using ProtoBuf;                             // ProtoContract, ProtoMember

namespace StaminaAddonCO
{
    [ProtoContract]
    public class StaminaPacket
    {
        [ProtoMember(1)]
        public string PlayerUid { get; set; }

        [ProtoMember(2)]
        public float Value { get; set; }

        [ProtoMember(3)]
        public float Max { get; set; }
    }
}
