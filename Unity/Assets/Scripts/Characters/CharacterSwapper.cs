using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwapper : MonoBehaviour
{
    private GameCharacter[] m_characters;

    private int currentCharacterIndex = -1;
    
    private void Start()
    {
        m_characters = GetComponentsInChildren<GameCharacter>();
        FocusCharacter(0);
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            var targetIndex = (currentCharacterIndex + 1) % m_characters.Length;
            FocusCharacter(targetIndex);
        }
    }

    private void FocusCharacter(int index)
    {
        currentCharacterIndex = index;
        
        for (int i = 0; i < m_characters.Length; i++)
        {
            m_characters[i].IsFocused = i == index;
        }
    }
}
