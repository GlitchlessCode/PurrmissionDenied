using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public interface IRuleset
{
    void AddRules(Validator validator);
}

public class EmptyRuleset : IRuleset
{
    public void AddRules(Validator validator)
    {
        UnityEngine.Debug.LogWarning("Added empty ruleset, is that a mistake?");
        // No-Op
    }
}

public class Day1Rules : IRuleset
{
    public void AddRules(Validator validator)
    {
        validator.AddCondition(
            "1. NO swearing allowed in chat!",
            (currentUser) =>
            {
                return validator.messageRepeatsSpecific(currentUser, 2, @"\*");
            }
        );

        validator.AddCondition(
            "2. ban appeal MUST exist!",
            (currentUser) =>
            {
                return validator.stringLengthCheck(currentUser.Value.appeal_message, ">", 0);
            }
        );

        validator.AddCondition(
            "3. MAXIMUM 15 words in each chat message!",
            (currentUser) =>
            {
                return validator.wordsPerMessage(currentUser, "<=", 15);
            }
        );

        validator.AddCondition(
            "4. no individual messages sent in ALL CAPS!",
            (currentUser) =>
            {
                return validator.messagesContain(currentUser, @"^[^a-z]*$");
            }
        );

        validator.AddCondition(
            "5. pls unban ALL users that have been banned for over a month!",
            (currentUser) =>
            {
                return true;
            }
        );
    }
}

public class Day2Rules : IRuleset
{
    public void AddRules(Validator validator)
    {
        validator.AddCondition(
            "1. NO swearing allowed in chat!",
            (currentUser) =>
            {
                return validator.messageRepeatsSpecific(currentUser, 2, @"\*");
            }
        );

        validator.AddCondition(
            "2. ban appeal MUST exist!",
            (currentUser) =>
            {
                return validator.stringLengthCheck(currentUser.Value.appeal_message, ">", 0);
            }
        );

        validator.AddCondition(
            "3. MINIMUM 3 words in each chat message!",
            (currentUser) =>
            {
                return validator.wordsPerMessage(currentUser, ">=", 3);
            }
        );

        validator.AddCondition(
            "4. no full chat logs in ENTIRELY lowercase!",
            (currentUser) =>
            {
                return !validator.messagesContain(currentUser, @"[A-Z]");
            }
        );

        validator.AddCondition(
            "5. do NOT share personal information!",
            (currentUser) =>
            {
                foreach (string msg in currentUser.Value.messages)
                {
                    if (Regex.IsMatch(msg.ToLower(), @"[0-9]{3}"))
                    {
                        return false;
                    }
                }
                return true;
            }
        );

        validator.AddCondition(
            "6. NO links in user bios!",
            (currentUser) =>
            {
                bool rule6bio = !validator.stringContains(currentUser.Value.bio, @"http?://");
                //Debug.Log("!Bio link, http: " + rule6bio);
                return rule6bio;
            }
        );

        validator.AddCondition(
            "7. pls unban ALL users that have been banned for over a month!",
            (currentUser) =>
            {
                return true;
            }
        );
    }
}

public class Day3Rules : IRuleset
{
    public void AddRules(Validator validator)
    {
        validator.AddCondition(
            "1. NO discussion or mention of cats or cat-related references in any semblance!",
            (currentUser) =>
            {
                // True implies passes, false implies fails
                bool res = true;
                res &= validator.messagesContain(
                    currentUser,
                    @"(?im)(cat)|(meow)|(purr)|(nyah)|(claw)"
                ); // This one is automatically inverted, does not need a !
                res &= !validator.stringContains(
                    currentUser.Value.bio,
                    @"(?im)(cat)|(meow)|(purr)|(nyah)|(claw)"
                );
                res &= !validator.stringContains(
                    currentUser.Value.name,
                    @"(?im)(cat)|(meow)|(purr)|(nyah)|(claw)"
                );
                res &= !validator.stringContains(
                    currentUser.Value.appeal_message,
                    @"(?im)(cat)|(meow)|(purr)|(nyah)|(claw)"
                );
                return res;
            }
        );

        validator.AddCondition(
            "2. ban appeal MUST exist!",
            (currentUser) =>
            {
                return validator.stringLengthCheck(currentUser.Value.appeal_message, ">", 0);
            }
        );

        validator.AddCondition(
            "3. NO more than 3 messages!",
            (currentUser) =>
            {
                return validator.numberMessages(currentUser, "<=", 3);
            }
        );

        validator.AddCondition(
            "4. NO more than 5 capital letters per message!",
            (currentUser) =>
            {
                foreach (string msg in currentUser.Value.messages)
                {
                    if (Regex.Matches(msg, "[A-Z]").Count > 5)
                    {
                        return false;
                    }
                }
                return true;
            }
        );

        validator.AddCondition(
            "5. do NOT share personal information!",
            (currentUser) =>
            {
                foreach (string msg in currentUser.Value.messages)
                {
                    if (Regex.IsMatch(msg.ToLower(), @"[0-9]{3}"))
                    {
                        return false;
                    }
                }
                return true;
            }
        );

        validator.AddCondition(
            "6. NO links in user bios or chat!",
            (currentUser) =>
            {
                bool rule6bio = !validator.stringContains(currentUser.Value.bio, @"http?://");
                bool rule6chat = !validator.messagesContain(currentUser, @"http?://");
                return rule6bio || rule6chat;
            }
        );

        validator.AddCondition(
            "7. NO dog avatars!",
            (currentUser) =>
            {
                return currentUser.Value.image_index != 35;
            }
        );

        validator.AddCondition(
            "8. NO emoticons in chat messages!",
            (currentUser) =>
            {
                return validator.messagesContain(currentUser, @"(?::)|(?:XD)|(?:;)");
            }
        );

        // NOTE: This assumes all binary messages *specifically* fail
        validator.AddCondition(
            "9. rules must apply in EVERY LANGUAGE!",
            (currentUser) =>
            {
                HashSet<char> chars = new HashSet<char>();
                foreach (string msg in currentUser.Value.messages)
                {
                    chars.UnionWith(msg);
                }
                chars.UnionWith(currentUser.Value.bio);
                chars.UnionWith(currentUser.Value.appeal_message);

                Debug.Log($"{chars.Count} {string.Join(", ", chars)}");

                return !(
                    chars.Count <= 5
                    && chars.Contains('.')
                    && chars.Contains('-')
                    && chars.Contains('/')
                );
            }
        );

        validator.AddCondition(
            "10. do NOT ask the mouser questions in chat!",
            (currentUser) =>
            {
                return validator.messagesContain(currentUser, @"\?");
            }
        );

        validator.AddCondition(
            "11. pls unban ALL users that have been banned for over a month!",
            (currentUser) =>
            {
                return true;
            }
        );
    }
}
