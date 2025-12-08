using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarManager : Subscriber
{
    public Image AvatarElement;

    public Sprite DefaultAvatar;
    public SpriteList Avatars;

    [Header("Event Listeners")]
    public UserEntryGameEvent UserLoaded;

    public override void Subscribe()
    {
        UserLoaded?.Subscribe(OnUserLoaded);
    }

    void OnUserLoaded(UserEntry user)
    {
        if (user.messages.Length == 0 && user.image_index == 0 && DefaultAvatar != null)
        {
            AvatarElement.sprite = DefaultAvatar;
            return;
        }

        if (Avatars != null)
        {
            if (Avatars.Sprites.Count > user.image_index)
            {
                AvatarElement.sprite = Avatars.Sprites[user.image_index];
            }
            else if (DefaultAvatar != null)
            {
                AvatarElement.sprite = DefaultAvatar;
            }
            else if (Avatars.Sprites.Count > 0)
            {
                AvatarElement.sprite = Avatars.Sprites[0];
            }
        }
        else if (DefaultAvatar != null)
        {
            AvatarElement.sprite = DefaultAvatar;
        }
    }
}
