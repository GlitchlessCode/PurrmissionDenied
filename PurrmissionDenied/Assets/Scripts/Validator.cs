using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class Validator
{
    // Dictionary of conditions with their string descriptions
    private Dictionary<string, Func<UserEntry?, bool>> _conditions =
        new Dictionary<string, Func<UserEntry?, bool>>();

    // Method to add conditions with a string description
    public void AddCondition(string description, Func<UserEntry?, bool> condition)
    {
        _conditions[description] = condition;
    }

    // Method to check if a UserEntry satisfies all conditions
    public bool Validate(UserEntry? user, string Date)
    {
        if (DateCheck(user, Date))
        {
            return DateCheck(user, Date);
        }

        foreach (var condition in _conditions.Values)
        {
            if (!condition(user))
            {
                // string myKey = _conditions.FirstOrDefault(x => x.Value == condition).Key;
                //Debug.Log(myKey);
                return false;
            }
        }

        return true;
    }

    // Get which rules broken
    public string GetBrokenRules(UserEntry? user, string Date)
    {
        string broken = "Rules Broken: #";

        foreach (var kvp in _conditions)
        {
            string text = kvp.Key;
            var condition = kvp.Value;

            if (!condition(user))
            {
                // string myKey = _conditions.FirstOrDefault(x => x.Value == condition).Key;
                //Debug.Log(myKey);
                broken = broken + $"{Regex.Match(text, @"^([0-9]*)\.").Groups[1]}, ";
            }
        }
        return broken.Substring(0, broken.Length - 2);
    }

    public bool DateCheck(UserEntry? user, string Date)
    {
        Regex regex = new Regex(@"(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})");
        Match banMatch = regex.Match(user.Value.date);
        Match todayMatch = regex.Match(Date);

        try
        {
            DateTime parsedBanDate = new DateTime(
                int.Parse(banMatch.Groups["year"].Value),
                int.Parse(banMatch.Groups["month"].Value),
                int.Parse(banMatch.Groups["day"].Value)
            );

            DateTime parsedTodayDate = new DateTime(
                int.Parse(todayMatch.Groups["year"].Value),
                int.Parse(todayMatch.Groups["month"].Value),
                int.Parse(todayMatch.Groups["day"].Value)
            );

            // if more than a year has passed
            if (parsedBanDate.Year < (parsedTodayDate.Year - 1))
            {
                //Debug.Log("Date Override: Today -" +parsedTodayDate+" Banned -"+parsedBanDate);
                return true;
            }
            // if the ban was last year
            else if (parsedBanDate.Year == (parsedTodayDate.Year - 1))
            {
                // check if the ban was like end of december and we are in january
                if (
                    !(
                        (parsedBanDate.Month == 12)
                        && (parsedTodayDate.Month == 1)
                        && (parsedBanDate.Day < parsedTodayDate.Day)
                    )
                )
                {
                    //Debug.Log("Date Override: Today -" +parsedTodayDate+" Banned -"+parsedBanDate);
                    return true;
                }
            }
            // if we are in the same year
            else if (parsedBanDate.Year == parsedTodayDate.Year)
            {
                // check if more than a month has passed
                if (parsedBanDate.Month < (parsedTodayDate.Month - 1))
                {
                    //Debug.Log("Date Override: Today -" +parsedTodayDate+" Banned -"+parsedBanDate);
                    return true;
                }
                // check if exactly one month has passed - make sure the days work out, too
                if (
                    (parsedBanDate.Month == (parsedTodayDate.Month - 1))
                    && (parsedBanDate.Day <= parsedTodayDate.Day)
                )
                {
                    //Debug.Log("Date Override: Today -" +parsedTodayDate+" Banned -"+parsedBanDate);
                    return true;
                }
            }
        }
        catch (FormatException ex)
        {
            Debug.LogError("Error parsing date: " + ex.Message);
            return false;
        }
        return false;
    }

    // Method to remove a condition based on its description
    public bool RemoveCondition(string description)
    {
        return _conditions.Remove(description);
    }

    // message checks

    public bool messagesContain(UserEntry? user, string text)
    {
        bool res = true;

        foreach (string message in user.Value.messages)
        {
            if (Regex.IsMatch(message, text))
            {
                res = false;
            }
        }

        return res;
    }

    public bool messageRepeats(UserEntry? user, int reps)
    {
        string text = @".*(.)\1{" + (reps - 1) + ",}.*";

        foreach (string message in user.Value.messages)
        {
            if (Regex.IsMatch(message.ToLower(), text))
            {
                return false;
            }
        }

        return true;
    }

    public bool messageRepeatsSpecific(UserEntry? user, int reps, string character)
    {
        string text = @".*(" + character + @")\1{" + (reps - 1) + ",}.*";

        foreach (string message in user.Value.messages)
        {
            if (Regex.IsMatch(message.ToLower(), text))
            {
                return false;
            }
        }

        return true;
    }

    public bool messageLengthCheck(UserEntry? user, string check, int length)
    {
        foreach (string message in user.Value.messages)
        {
            switch (check)
            {
                case "<=":
                    if (message.Length > length)
                    {
                        return false;
                    }
                    break;
                case "<":
                    if (message.Length >= length)
                    {
                        return false;
                    }
                    break;
                case ">=":
                    if (message.Length < length)
                    {
                        return false;
                    }
                    break;
                case ">":
                    if (message.Length <= length)
                    {
                        return false;
                    }
                    break;
                case "==":
                    if (message.Length != length)
                    {
                        return false;
                    }
                    break;
                default:
                    break;
            }
        }

        return true;
    }

    public bool numberMessages(UserEntry? user, string check, int num)
    {
        int n = 0;
        foreach (string message in user.Value.messages)
        {
            n++;
        }

        switch (check)
        {
            case "<=":
                if (n > num)
                {
                    return false;
                }
                break;
            case "<":
                if (n >= num)
                {
                    return false;
                }
                break;
            case ">=":
                if (n < num)
                {
                    return false;
                }
                break;
            case ">":
                if (n <= num)
                {
                    return false;
                }
                break;
            case "==":
                if (n != num)
                {
                    return false;
                }
                break;
            default:
                break;
        }
        return true;
    }

    public bool wordsPerMessage(UserEntry? user, string check, int num)
    {
        foreach (string message in user.Value.messages)
        {
            int length = (message.Split(' ')).Length;

            switch (check)
            {
                case "<=":
                    if (length > num)
                    {
                        return false;
                    }
                    break;
                case "<":
                    if (length >= num)
                    {
                        return false;
                    }
                    break;
                case ">=":
                    if (length < num)
                    {
                        return false;
                    }
                    break;
                case ">":
                    if (length <= num)
                    {
                        return false;
                    }
                    break;
                case "==":
                    if (length != num)
                    {
                        return false;
                    }
                    break;
                default:
                    break;
            }
        }
        return true;
    }

    // general string checks

    public bool stringContains(string s, string text)
    {
        return (Regex.IsMatch(s.ToLower(), text));
    }

    public bool stringRepeats(string s, int reps)
    {
        return (Regex.IsMatch(s.ToLower(), @".*(.)\1{" + (reps - 1) + ",}.*"));
    }

    public bool stringLengthCheck(string s, string check, int length)
    {
        switch (check)
        {
            case "<=":
                if (s.Length > length)
                {
                    return false;
                }
                break;
            case "<":
                if (s.Length >= length)
                {
                    return false;
                }
                break;
            case ">=":
                if (s.Length < length)
                {
                    return false;
                }
                break;
            case ">":
                if (s.Length <= length)
                {
                    return false;
                }
                break;
            case "==":
                if (s.Length != length)
                {
                    return false;
                }
                break;
            default:
                break;
        }

        return true;
    }

    // combine rule text
    public string GetConditionText()
    {
        return string.Join("\n\n", _conditions.Keys);
    }

    // public
}
