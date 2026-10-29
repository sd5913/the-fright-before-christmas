using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheFrightBeforeChristmas
{
    public class CharacterSwapController : MonoBehaviour
    {
        public TFBCCharacter[] Characters;

        private int currentCharacterIndex = 0;
        
        private void Start()
        {
            Characters[0].OnCharacterActivated?.Invoke();
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Characters[currentCharacterIndex].OnCharacterDeactivated?.Invoke();
                currentCharacterIndex = (currentCharacterIndex + 1)  % Characters.Length;
                Characters[currentCharacterIndex].OnCharacterActivated?.Invoke();
            }
        }
    }
}