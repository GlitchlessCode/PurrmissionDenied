using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UserPoolDefinition
{
    public List<string> UserFiles;

    UserPoolDefinition(List<string> files)
    {
        UserFiles = files;
    }

    public UserPoolDefinition Clone()
    {
        return new UserPoolDefinition(new List<string>(UserFiles));
    }
}

[Serializable]
public class UserPool
{
    public int PoolIndex;
    public int UserCount;

    [Header("Events")]
    public List<UnitGameEvent> Before;
    public List<UnitGameEvent> After;

    UserPool(int index, int count, List<UnitGameEvent> before, List<UnitGameEvent> after)
    {
        PoolIndex = index;
        UserCount = count;
        Before = before;
        After = after;
    }

    public UserPool Clone()
    {
        return new UserPool(
            PoolIndex,
            UserCount,
            new List<UnitGameEvent>(Before),
            new List<UnitGameEvent>(After)
        );
    }
}

[CreateAssetMenu(menuName = "Day Definition")]
public class DayDefinition : ScriptableObject
{
    public int Index;
    public string Directory;
    public string Date;

    [Header("User Pools")]
    public List<UserPoolDefinition> PoolDefinitions;

    [Header("Pool Ordering")]
    public List<UserPool> PoolOrder;

    public DayDefinition()
    {
        Directory = "daynull";
        Date = "null";
        PoolDefinitions = new List<UserPoolDefinition>();
        PoolOrder = new List<UserPool>();
    }
}
