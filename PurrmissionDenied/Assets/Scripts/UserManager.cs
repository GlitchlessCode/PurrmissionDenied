using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class UserManager : Subscriber
{
    private InternalDayDefinition day;

    [Header("Validation")]
    private UserEntry? currentUser;
    Validator validator = new Validator();

    private Dictionary<string, UserEntry> users;

    [Header("Event Listeners")]
    public BoolGameEvent ResolveAppeal;
    public UnitGameEvent UserInfoRequest;
    public DayDefinitionGameEvent DayLoaded;

    [Header("Events")]
    public UserEntryGameEvent UserLoaded;
    public StringGameEvent ValidatorLoaded;
    public StringGameEvent DayDate;
    public StringGameEvent Mistake;
    public UnitGameEvent AsyncComplete;

    // `true` implies player chose correctly, `false` implies player chose incorrectly
    public BoolGameEvent AfterAppeal;
    public IntGameEvent DayStart;
    public UnitGameEvent DayFinished;
    public IntGameEvent GetUserAmount;
    private bool dayAlreadyFinished = false;

    public override void Subscribe()
    {
        ResolveAppeal?.Subscribe(OnResolveAppeal);
        UserInfoRequest?.Subscribe(OnUserInfoRequest);
        DayLoaded?.Subscribe(OnDayLoaded);
    }

    public override void AfterSubscribe()
    {
        currentUser = null;
    }

    private void OnDayLoaded(DayDefinition Day)
    {
        day = new InternalDayDefinition(Day);
        sendDayData(day);
        addRules(day.Index);

        // load users
        StartCoroutine(
            JSONImporter.ImportFiles<UserEntry>(
                Path.Combine("lang", "en", "days", day.Directory),
                day.UserFiles,
                (usersOut) =>
                {
                    users = usersOut;
                    MoveToNextUser();
                    DayStart?.Emit(day.Index);
                    AsyncComplete?.Emit();
                    GetUserAmount?.Emit(day.PoolOrder.ConvertAll((order) => order.UserCount).Sum());
                }
            )
        );
    }

    private void sendDayData(InternalDayDefinition day)
    {
        DayDate?.Emit(day.Date);
    }

    private static readonly IRuleset[] RULESETS = new IRuleset[3]
    {
        new Day1Rules(),
        new Day2Rules(),
        new Day3Rules(),
    };

    private void addRules(int index)
    {
        IRuleset ruleset = RULESETS[index - 1];
        ruleset.AddRules(validator);

        ValidatorLoaded?.Emit(validator.GetConditionText());
    }

    // moves to next user index
    private bool MoveToNextUser()
    {
        // if we somehow get here with no users
        if (users == null || users.Count == 0)
        {
            currentUser = null;
            SignalDayFinishedOnce();
            return false;
        }

        string user_file = day.PopNextUser();
        if (user_file != null && users.ContainsKey(user_file))
        {
            currentUser = users[user_file];
        }
        else
        {
            currentUser = null;
            SignalDayFinishedOnce();
            return false;
        }

        OnUserInfoRequest();

        // return true if successful
        return true;
    }

    private void SignalDayFinishedOnce()
    {
        if (dayAlreadyFinished)
            return;
        dayAlreadyFinished = true;
        DayFinished?.Emit();
        UserEntry empty = new UserEntry();
        empty.messages = new string[0];
        UserLoaded?.Emit(empty);
    }

    private void OnResolveAppeal(bool decision)
    {
        UserEntry? user = currentUser;
        //Canvas.ForceUpdateCanvases();

        if (user != null)
        {
            // red ring stuff
            bool correct = validator.Validate(user, day.Date);
            if (correct != decision)
            {
                if (correct)
                {
                    if (validator.DateCheck(user, day.Date))
                    {
                        Mistake?.Emit("User had been banned for a month already...");
                    }
                    else
                    {
                        Mistake?.Emit("No Rules Broken");
                    }
                }
                else
                {
                    if (user.Value.image_index == 39)
                    {
                        Mistake?.Emit("Rules Broken: #1,9");
                    }
                    else
                    {
                        Mistake?.Emit(validator.GetBrokenRules(user, day.Date));
                    }
                }
            }
            AfterAppeal?.Emit(correct == decision);
        }

        MoveToNextUser();
    }

    private void OnUserInfoRequest()
    {
        UserEntry? user = currentUser;
        if (user != null)
        {
            UserLoaded?.Emit(user.Value);
        }
    }
}

class InternalDayDefinition
{
    public int Index;
    public string Directory;
    public string Date;
    public List<UserPoolDefinition> PoolDefinitions;
    public List<UserPool> PoolOrder;

    private int currentOrder;
    private int currentCountInOrder;

    public HashSet<string> UserFiles
    {
        get
        {
            HashSet<string> files = new HashSet<string>();

            foreach (UserPoolDefinition pool in PoolDefinitions)
            {
                pool.UserFiles.ForEach((item) => files.Add(item));
            }

            return files;
        }
    }

    public InternalDayDefinition(DayDefinition day)
    {
        Index = day.Index;
        Directory = day.Directory;
        Date = day.Date;
        PoolDefinitions = new List<UserPoolDefinition>();
        PoolOrder = new List<UserPool>();

        currentOrder = 0;
        currentCountInOrder = 0;

        // Copy pools
        foreach (UserPoolDefinition def in day.PoolDefinitions)
        {
            PoolDefinitions.Add(def.Clone());
        }
        foreach (UserPool pool in day.PoolOrder)
        {
            PoolOrder.Add(pool.Clone());
        }

        if (PoolOrder.Count != 0)
        {
            foreach (UnitGameEvent before in PoolOrder[0].Before)
            {
                before.Emit();
            }
        }
    }

    public string PopNextUser()
    {
        // If we try to access an order past how many we have defined
        if (currentOrder >= PoolOrder.Count)
        {
            return null;
        }

        // If we finish the current order
        if (++currentCountInOrder > PoolOrder[currentOrder].UserCount)
        {
            // Call all after events
            foreach (UnitGameEvent after in PoolOrder[currentOrder].After)
            {
                after.Emit();
            }

            // Increment the order and reset the order counter
            currentCountInOrder = 1;
            currentOrder += 1;

            // Check again for an access of an order past the defined count
            if (currentOrder >= PoolOrder.Count)
            {
                return null;
            }

            // Call all before events on the new order
            foreach (UnitGameEvent before in PoolOrder[currentOrder].Before)
            {
                before.Emit();
            }
        }

        // If the selected pool definition doesn't exist
        if (PoolOrder[currentOrder].PoolIndex > PoolDefinitions.Count)
        {
            return null;
        }

        UserPoolDefinition pool = PoolDefinitions[PoolOrder[currentOrder].PoolIndex];

        // If the selected pool definition has no remaining users
        if (pool.UserFiles.Count == 0)
        {
            return null;
        }

        // Pick a random index
        System.Random rand = new System.Random();
        int idx = rand.Next(pool.UserFiles.Count);

        string file = pool.UserFiles[idx];
        pool.UserFiles.RemoveAt(idx);

        return file;
    }
}
