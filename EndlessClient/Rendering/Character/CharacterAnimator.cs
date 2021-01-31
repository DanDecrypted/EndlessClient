﻿using System;
using System.Collections.Generic;
using System.Linq;
using EndlessClient.GameExecution;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using EOLib.Domain.Map;
using EOLib.IO.Map;
using Microsoft.Xna.Framework;

namespace EndlessClient.Rendering.Character
{
    public class CharacterAnimator : GameComponent, ICharacterAnimator
    {
        public const int WALK_FRAME_TIME_MS = 100;
        public const int ATTACK_FRAME_TIME_MS = 285;

        private readonly ICharacterRepository _characterRepository;
        private readonly ICurrentMapStateRepository _currentMapStateRepository;
        private readonly ICurrentMapProvider _currentMapProvider;
        private readonly Dictionary<int, RenderFrameActionTime> _otherPlayerStartWalkingTimes;
        private readonly Dictionary<int, RenderFrameActionTime> _otherPlayerStartAttackingTimes;
        private readonly Dictionary<int, RenderFrameActionTime> _otherPlayerStartSpellCastTimes;

        public CharacterAnimator(IEndlessGameProvider gameProvider,
                                 ICharacterRepository characterRepository,
                                 ICurrentMapStateRepository currentMapStateRepository,
                                 ICurrentMapProvider currentMapProvider)
            : base((Game) gameProvider.Game)
        {
            _characterRepository = characterRepository;
            _currentMapStateRepository = currentMapStateRepository;
            _currentMapProvider = currentMapProvider;
            _otherPlayerStartWalkingTimes = new Dictionary<int, RenderFrameActionTime>();
            _otherPlayerStartAttackingTimes = new Dictionary<int, RenderFrameActionTime>();
            _otherPlayerStartSpellCastTimes = new Dictionary<int, RenderFrameActionTime>();
        }

        public override void Update(GameTime gameTime)
        {
            var now = DateTime.Now;

            AnimateCharacterWalking(now);
            AnimateCharacterAttacking(now);
            AnimateCharacterSpells(now);

            base.Update(gameTime);
        }

        public bool IsAttacking(int characterId)
        {
            return _otherPlayerStartAttackingTimes.ContainsKey(characterId);
        }

        public void StartMainCharacterWalkAnimation()
        {
            if (_otherPlayerStartWalkingTimes.ContainsKey(_characterRepository.MainCharacter.ID))
                return;

            var startWalkingTime = new RenderFrameActionTime(_characterRepository.MainCharacter.ID, GetStartingAnimationTime(WALK_FRAME_TIME_MS));
            _otherPlayerStartWalkingTimes.Add(_characterRepository.MainCharacter.ID, startWalkingTime);
        }

        public void StartMainCharacterAttackAnimation()
        {
            if (_otherPlayerStartAttackingTimes.ContainsKey(_characterRepository.MainCharacter.ID))
                return;

            var isRangedWeapon = GetCurrentCharacterFromRepository(_characterRepository.MainCharacter.ID).RenderProperties.IsRangedWeapon;
            var startAttackingTime = new RenderFrameActionTime(_characterRepository.MainCharacter.ID, GetStartingAnimationTime(ATTACK_FRAME_TIME_MS, isRangedWeapon));
            _otherPlayerStartAttackingTimes.Add(_characterRepository.MainCharacter.ID, startAttackingTime);
        }

        public void StartOtherCharacterWalkAnimation(int characterID)
        {
            if (_otherPlayerStartAttackingTimes.ContainsKey(characterID) ||
                _otherPlayerStartSpellCastTimes.ContainsKey(characterID))
                return;

            if (_otherPlayerStartWalkingTimes.ContainsKey(characterID))
            {
                // character is already walking, start over their walking at walk frame 1
                ResetCharacterAnimationFrames(characterID);
                var character = GetCurrentCharacterFromRepository(characterID);
                var updatedFrame = AnimateOneWalkFrame(character.RenderProperties);
                UpdateCharacterInRepository(character, character.WithRenderProperties(updatedFrame));

                _otherPlayerStartWalkingTimes[characterID] = new RenderFrameActionTime(characterID, GetStartingAnimationTime(WALK_FRAME_TIME_MS));
            }
            else
            {
                var startWalkingTimeAndID = new RenderFrameActionTime(characterID, GetStartingAnimationTime(WALK_FRAME_TIME_MS));
                _otherPlayerStartWalkingTimes.Add(characterID, startWalkingTimeAndID);
            }
        }

        public void StartOtherCharacterAttackAnimation(int characterID)
        {
            if (_otherPlayerStartWalkingTimes.ContainsKey(characterID) ||
                _otherPlayerStartSpellCastTimes.ContainsKey(characterID))
                return;

            if (_otherPlayerStartAttackingTimes.TryGetValue(characterID, out var existingStartTime))
            {
                ResetCharacterAnimationFrames(characterID);
                _otherPlayerStartAttackingTimes.Remove(characterID);
            }

            var isRangedWeapon = GetCurrentCharacterFromRepository(characterID).RenderProperties.IsRangedWeapon;
            var startAttackingTimeAndID = new RenderFrameActionTime(characterID, GetStartingAnimationTime(ATTACK_FRAME_TIME_MS, isRangedWeapon));
            _otherPlayerStartAttackingTimes.Add(characterID, startAttackingTimeAndID);
        }

        public void StartOtherCharacterSpellCast(int characterID)
        {
            if (_otherPlayerStartWalkingTimes.ContainsKey(characterID) ||
                _otherPlayerStartAttackingTimes.ContainsKey(characterID))
                return;

            if (_otherPlayerStartSpellCastTimes.TryGetValue(characterID, out var existingStartTime))
            {
                ResetCharacterAnimationFrames(characterID);
                _otherPlayerStartSpellCastTimes.Remove(characterID);
            }

            var isRangedWeapon = GetCurrentCharacterFromRepository(characterID).RenderProperties.IsRangedWeapon;
            var startAttackingTimeAndID = new RenderFrameActionTime(characterID, GetStartingAnimationTime(ATTACK_FRAME_TIME_MS, isRangedWeapon));
            _otherPlayerStartSpellCastTimes.Add(characterID, startAttackingTimeAndID);
        }

        public void StopAllCharacterAnimations()
        {
            _otherPlayerStartWalkingTimes.Clear();
            _otherPlayerStartAttackingTimes.Clear();
            _otherPlayerStartSpellCastTimes.Clear();

            _characterRepository.MainCharacter =
                _characterRepository.MainCharacter.WithRenderProperties(
                    _characterRepository.MainCharacter.RenderProperties.ResetAnimationFrames());

            _currentMapStateRepository.Characters =
                new HashSet<ICharacter>(
                    _currentMapStateRepository.Characters.Select(x => x.WithRenderProperties(x.RenderProperties.ResetAnimationFrames())));
        }

        #region Walk Animation

        private void AnimateCharacterWalking(DateTime now)
        {
            var playersDoneWalking = new List<int>();
            foreach (var pair in _otherPlayerStartWalkingTimes.Values)
            {
                if (pair.ActionStartTime.HasValue &&
                    (now - pair.ActionStartTime).TotalMilliseconds > WALK_FRAME_TIME_MS)
                {
                    var currentCharacter = GetCurrentCharacterFromRepository(pair.UniqueID);
                    if (currentCharacter == null)
                    {
                        playersDoneWalking.Add(pair.UniqueID);
                        continue;
                    }

                    var renderProperties = currentCharacter.RenderProperties;
                    var nextFrameRenderProperties = AnimateOneWalkFrame(renderProperties);

                    pair.UpdateActionStartTime(GetUpdatedActionTime(now, nextFrameRenderProperties));
                    if (!pair.ActionStartTime.HasValue)
                        playersDoneWalking.Add(pair.UniqueID);

                    var nextFrameCharacter = currentCharacter.WithRenderProperties(nextFrameRenderProperties);
                    UpdateCharacterInRepository(currentCharacter, nextFrameCharacter);
                }
            }

            foreach (var key in playersDoneWalking)
            {
                _otherPlayerStartWalkingTimes.Remove(key);

                var character = GetCurrentCharacterFromRepository(key);
                var renderProperties = character.RenderProperties;
                renderProperties = renderProperties
                    .WithMapX(renderProperties.GetDestinationX())
                    .WithMapY(renderProperties.GetDestinationY());
                UpdateCharacterInRepository(character, character.WithRenderProperties(renderProperties));
            }
        }

        private ICharacterRenderProperties AnimateOneWalkFrame(ICharacterRenderProperties renderProperties)
        {
            var map = _currentMapProvider.CurrentMap;
            var isSteppingStone = map.Properties.IsInBounds(renderProperties.MapPosition) && map.Properties.IsInBounds(renderProperties.GetDestination()) &&
                (map.Tiles[renderProperties.MapY, renderProperties.MapX] == TileSpec.Jump ||
                 map.Tiles[renderProperties.GetDestinationY(), renderProperties.GetDestinationX()] == TileSpec.Jump);

            return renderProperties.WithNextWalkFrame(isSteppingStone);
        }

        #endregion

        #region Attack Animation

        private void AnimateCharacterAttacking(DateTime now)
        {
            var playersDoneAttacking = new HashSet<int>();
            foreach (var pair in _otherPlayerStartAttackingTimes.Values)
            {
                if (pair.ActionStartTime.HasValue &&
                    (now - pair.ActionStartTime).TotalMilliseconds > ATTACK_FRAME_TIME_MS)
                {
                    var currentCharacter = GetCurrentCharacterFromRepository(pair.UniqueID);
                    if (currentCharacter == null)
                    {
                        playersDoneAttacking.Add(pair.UniqueID);
                        continue;
                    }

                    var renderProperties = currentCharacter.RenderProperties;
                    var nextFrameRenderProperties = renderProperties.WithNextAttackFrame();

                    pair.UpdateActionStartTime(GetUpdatedActionTime(now, nextFrameRenderProperties));
                    if (!pair.ActionStartTime.HasValue)
                        playersDoneAttacking.Add(pair.UniqueID);

                    var nextFrameCharacter = currentCharacter.WithRenderProperties(nextFrameRenderProperties);
                    UpdateCharacterInRepository(currentCharacter, nextFrameCharacter);
                }
            }

            foreach (var key in playersDoneAttacking)
                _otherPlayerStartAttackingTimes.Remove(key);
        }

        #endregion

        #region Spell Animation

        private void AnimateCharacterSpells(DateTime now)
        {
            var playersDoneCasting = new HashSet<int>();
            foreach (var pair in _otherPlayerStartSpellCastTimes.Values)
            {
                if (pair.ActionStartTime.HasValue &&
                    (now - pair.ActionStartTime).TotalMilliseconds > ATTACK_FRAME_TIME_MS)
                {
                    var currentCharacter = GetCurrentCharacterFromRepository(pair.UniqueID);
                    if (currentCharacter == null)
                    {
                        playersDoneCasting.Add(pair.UniqueID);
                        continue;
                    }

                    var renderProperties = currentCharacter.RenderProperties;
                    var nextFrameRenderProperties = renderProperties.WithNextSpellCastFrame();

                    pair.UpdateActionStartTime(GetUpdatedActionTime(now, nextFrameRenderProperties));
                    if (!pair.ActionStartTime.HasValue)
                        playersDoneCasting.Add(pair.UniqueID);

                    var nextFrameCharacter = currentCharacter.WithRenderProperties(nextFrameRenderProperties);
                    UpdateCharacterInRepository(currentCharacter, nextFrameCharacter);
                }
            }

            foreach (var key in playersDoneCasting)
                _otherPlayerStartSpellCastTimes.Remove(key);
        }

        #endregion

        private static Optional<DateTime> GetUpdatedActionTime(DateTime now, ICharacterRenderProperties nextFrameRenderProperties)
        {
            return nextFrameRenderProperties.IsActing(CharacterActionState.Standing)
                ? Optional<DateTime>.Empty
                : new Optional<DateTime>(now);
        }

        private ICharacter GetCurrentCharacterFromRepository(int id)
        {
            return id == _characterRepository.MainCharacter.ID
                ? _characterRepository.MainCharacter
                : _currentMapStateRepository.Characters.SingleOrDefault(x => x.ID == id);
        }

        private void UpdateCharacterInRepository(ICharacter currentCharacter, ICharacter nextFrameCharacter)
        {
            if (currentCharacter == _characterRepository.MainCharacter)
            {
                _characterRepository.MainCharacter = nextFrameCharacter;
            }
            else
            {
                _currentMapStateRepository.Characters.Remove(currentCharacter);
                _currentMapStateRepository.Characters.Add(nextFrameCharacter);
            }
        }

        private void ResetCharacterAnimationFrames(int characterID)
        {
            var character = _currentMapStateRepository.Characters.Single(x => x.ID == characterID);
            var renderProps = character.RenderProperties.ResetAnimationFrames();
            var newCharacter = character.WithRenderProperties(renderProps);
            _currentMapStateRepository.Characters.Remove(character);
            _currentMapStateRepository.Characters.Add(newCharacter);
        }

        private static DateTime GetStartingAnimationTime(int frameTimer, bool isRangedWeapon = false)
        {
            // make the first frame very short for animation
            // this works around a bug where the first frame is delayed for (seemingly) no good reason
            // maybe I will eventually figure out why that was happening but this seems to work just fine for now
            return DateTime.Now.AddMilliseconds(isRangedWeapon ? 0 : -frameTimer);
        }
    }

    public interface ICharacterAnimator : IGameComponent
    {
        bool IsAttacking(int characterId);

        void StartMainCharacterWalkAnimation();

        void StartMainCharacterAttackAnimation();

        void StartOtherCharacterWalkAnimation(int characterID);

        void StartOtherCharacterAttackAnimation(int characterID);

        void StartOtherCharacterSpellCast(int characterID);

        void StopAllCharacterAnimations();
    }
}
