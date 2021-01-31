﻿using EOLib.Domain.Map;
using EOLib.Net.API;

namespace EOLib.Domain.Character
{
    public class CharacterRenderProperties : ICharacterRenderProperties
    {
        public const int MAX_NUMBER_OF_WALK_FRAMES   = 5;
        public const int MAX_NUMBER_OF_ATTACK_FRAMES  = 3;
        public const int MAX_NUMBER_OF_RANGED_ATTACK_FRAMES = 2;
        private const int MAX_NUMBER_OF_EMOTE_FRAMES  = 3;

        public CharacterActionState CurrentAction { get; private set; }

        public byte HairStyle { get; private set; }
        public byte HairColor { get; private set; }
        public byte Race { get; private set; }
        public byte Gender { get; private set; }

        public short BootsGraphic { get; private set; }
        public short ArmorGraphic { get; private set; }
        public short HatGraphic { get; private set; }
        public short ShieldGraphic { get; private set; }
        public short WeaponGraphic { get; private set; }

        public EODirection Direction { get; private set; }
        public int MapX { get; private set; }
        public int MapY { get; private set; }
        public MapCoordinate MapPosition => new MapCoordinate(MapX, MapY);

        public int ActualWalkFrame { get; private set; }
        public int RenderWalkFrame { get; private set; }
        public int AttackFrame { get; private set; }
        public int EmoteFrame { get; private set; }

        public SitState SitState { get; private set; }
        public Emote Emote { get; private set; }

        public bool IsHidden { get; private set; }
        public bool IsDead { get; private set; }

        public bool IsRangedWeapon { get; private set; }

        public ICharacterRenderProperties WithHairStyle(byte newHairStyle)
        {
            var props = MakeCopy(this);
            props.HairStyle = newHairStyle;
            return props;
        }

        public ICharacterRenderProperties WithHairColor(byte newHairColor)
        {
            var props = MakeCopy(this);
            props.HairColor = newHairColor;
            return props;
        }

        public ICharacterRenderProperties WithRace(byte newRace)
        {
            var props = MakeCopy(this);
            props.Race = newRace;
            return props;
        }

        public ICharacterRenderProperties WithGender(byte newGender)
        {
            var props = MakeCopy(this);
            props.Gender = newGender;
            return props;
        }

        public ICharacterRenderProperties WithBootsGraphic(short bootsGraphic)
        {
            var props = MakeCopy(this);
            props.BootsGraphic = bootsGraphic;
            return props;
        }

        public ICharacterRenderProperties WithArmorGraphic(short armorGraphic)
        {
            var props = MakeCopy(this);
            props.ArmorGraphic = armorGraphic;
            return props;
        }

        public ICharacterRenderProperties WithHatGraphic(short hatGraphic)
        {
            var props = MakeCopy(this);
            props.HatGraphic = hatGraphic;
            return props;
        }

        public ICharacterRenderProperties WithShieldGraphic(short shieldGraphic)
        {
            var props = MakeCopy(this);
            props.ShieldGraphic = shieldGraphic;
            return props;
        }

        public ICharacterRenderProperties WithWeaponGraphic(short weaponGraphic, bool isRangedWeapon)
        {
            var props = MakeCopy(this);
            props.WeaponGraphic = weaponGraphic;
            props.IsRangedWeapon = isRangedWeapon;
            return props;
        }

        public ICharacterRenderProperties WithDirection(EODirection newDirection)
        {
            var props = MakeCopy(this);
            props.Direction = newDirection;
            return props;
        }

        public ICharacterRenderProperties WithMapX(int mapX)
        {
            var props = MakeCopy(this);
            props.MapX = mapX;
            return props;
        }

        public ICharacterRenderProperties WithMapY(int mapY)
        {
            var props = MakeCopy(this);
            props.MapY = mapY;
            return props;
        }

        public ICharacterRenderProperties WithNextWalkFrame(bool isSteppingStone = false)
        {
            var props = MakeCopy(this);
            props.ActualWalkFrame = (props.ActualWalkFrame + 1) % MAX_NUMBER_OF_WALK_FRAMES;

            if (isSteppingStone && props.ActualWalkFrame > 1)
            {
                // force first walk frame when on a stepping stone (this is the graphic used for jump, Y adjusted)
                props.RenderWalkFrame = 1;
            }
            else
            {
                props.RenderWalkFrame = props.ActualWalkFrame;
            }

            props.CurrentAction = props.RenderWalkFrame == 0 ? CharacterActionState.Standing : CharacterActionState.Walking;

            return props;
        }

        public ICharacterRenderProperties WithNextAttackFrame()
        {
            var props = MakeCopy(this);
            props.AttackFrame = (props.AttackFrame + 1) % (IsRangedWeapon ? MAX_NUMBER_OF_RANGED_ATTACK_FRAMES : MAX_NUMBER_OF_ATTACK_FRAMES);
            props.CurrentAction = props.AttackFrame == 0 ? CharacterActionState.Standing : CharacterActionState.Attacking;
            return props;
        }

        public ICharacterRenderProperties WithNextEmoteFrame()
        {
            var props = MakeCopy(this);
            props.EmoteFrame = (props.EmoteFrame + 1) % MAX_NUMBER_OF_EMOTE_FRAMES;
            props.CurrentAction = props.EmoteFrame == 0 ? CharacterActionState.Standing : CharacterActionState.Emote;
            return props;
        }

        public ICharacterRenderProperties WithNextSpellCastFrame()
        {
            var props = MakeCopy(this);
            props.CurrentAction = props.CurrentAction == CharacterActionState.Standing
                ? CharacterActionState.SpellCast
                : CharacterActionState.Standing;
            return props;
        }

        public ICharacterRenderProperties ResetAnimationFrames()
        {
            var props = MakeCopy(this);
            props.RenderWalkFrame = 0;
            props.AttackFrame = 0;
            props.EmoteFrame = 0;
            props.CurrentAction = CharacterActionState.Standing;
            return props;
        }

        public ICharacterRenderProperties WithSitState(SitState newState)
        {
            var props = MakeCopy(this);
            props.SitState = newState;
            props.CurrentAction = newState == SitState.Standing ? CharacterActionState.Standing : CharacterActionState.Sitting;
            return props;
        }

        public ICharacterRenderProperties WithEmote(Emote emote)
        {
            var props = MakeCopy(this);
            props.Emote = emote;
            return props;
        }

        public ICharacterRenderProperties WithIsHidden(bool hidden)
        {
            var props = MakeCopy(this);
            props.IsHidden = hidden;
            return props;
        }

        public ICharacterRenderProperties WithDead()
        {
            var props = MakeCopy(this);
            props.IsDead = true;
            return props;
        }

        public ICharacterRenderProperties WithAlive()
        {
            var props = MakeCopy(this);
            props.IsDead = false;
            return props;
        }

        public object Clone()
        {
            return MakeCopy(this);
        }

        private static CharacterRenderProperties MakeCopy(ICharacterRenderProperties other)
        {
            return new CharacterRenderProperties
            {
                CurrentAction = other.CurrentAction,

                HairStyle = other.HairStyle,
                HairColor = other.HairColor,
                Race = other.Race,
                Gender = other.Gender,

                BootsGraphic = other.BootsGraphic,
                ArmorGraphic = other.ArmorGraphic,
                HatGraphic = other.HatGraphic,
                ShieldGraphic = other.ShieldGraphic,
                WeaponGraphic = other.WeaponGraphic,

                Direction = other.Direction,
                MapX = other.MapX,
                MapY = other.MapY,

                ActualWalkFrame = other.ActualWalkFrame,
                RenderWalkFrame = other.RenderWalkFrame,
                AttackFrame = other.AttackFrame,
                EmoteFrame = other.EmoteFrame,

                SitState = other.SitState,
                Emote = other.Emote,

                IsHidden = other.IsHidden,
                IsDead = other.IsDead,
                IsRangedWeapon = other.IsRangedWeapon
            };
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterRenderProperties properties &&
                   CurrentAction == properties.CurrentAction &&
                   HairStyle == properties.HairStyle &&
                   HairColor == properties.HairColor &&
                   Race == properties.Race &&
                   Gender == properties.Gender &&
                   BootsGraphic == properties.BootsGraphic &&
                   ArmorGraphic == properties.ArmorGraphic &&
                   HatGraphic == properties.HatGraphic &&
                   ShieldGraphic == properties.ShieldGraphic &&
                   WeaponGraphic == properties.WeaponGraphic &&
                   Direction == properties.Direction &&
                   MapX == properties.MapX &&
                   MapY == properties.MapY &&
                   ActualWalkFrame == properties.ActualWalkFrame &&
                   RenderWalkFrame == properties.RenderWalkFrame &&
                   AttackFrame == properties.AttackFrame &&
                   EmoteFrame == properties.EmoteFrame &&
                   SitState == properties.SitState &&
                   Emote == properties.Emote &&
                   IsHidden == properties.IsHidden &&
                   IsDead == properties.IsDead &&
                   IsRangedWeapon == properties.IsRangedWeapon;
        }

        public override int GetHashCode()
        {
            int hashCode = 1754760722;
            hashCode = hashCode * -1521134295 + CurrentAction.GetHashCode();
            hashCode = hashCode * -1521134295 + HairStyle.GetHashCode();
            hashCode = hashCode * -1521134295 + HairColor.GetHashCode();
            hashCode = hashCode * -1521134295 + Race.GetHashCode();
            hashCode = hashCode * -1521134295 + Gender.GetHashCode();
            hashCode = hashCode * -1521134295 + BootsGraphic.GetHashCode();
            hashCode = hashCode * -1521134295 + ArmorGraphic.GetHashCode();
            hashCode = hashCode * -1521134295 + HatGraphic.GetHashCode();
            hashCode = hashCode * -1521134295 + ShieldGraphic.GetHashCode();
            hashCode = hashCode * -1521134295 + WeaponGraphic.GetHashCode();
            hashCode = hashCode * -1521134295 + Direction.GetHashCode();
            hashCode = hashCode * -1521134295 + MapX.GetHashCode();
            hashCode = hashCode * -1521134295 + MapY.GetHashCode();
            hashCode = hashCode * -1521134295 + ActualWalkFrame.GetHashCode();
            hashCode = hashCode * -1521134295 + RenderWalkFrame.GetHashCode();
            hashCode = hashCode * -1521134295 + AttackFrame.GetHashCode();
            hashCode = hashCode * -1521134295 + EmoteFrame.GetHashCode();
            hashCode = hashCode * -1521134295 + SitState.GetHashCode();
            hashCode = hashCode * -1521134295 + Emote.GetHashCode();
            hashCode = hashCode * -1521134295 + IsHidden.GetHashCode();
            hashCode = hashCode * -1521134295 + IsDead.GetHashCode();
            hashCode = hashCode * -1521134295 + IsRangedWeapon.GetHashCode();
            return hashCode;
        }
    }
}
