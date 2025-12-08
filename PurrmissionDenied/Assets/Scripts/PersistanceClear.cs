using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistanceClear : MonoBehaviour
{
    void Start()
    {
        foreach (ScoreManager manager in FindObjectsOfType<ScoreManager>())
        {
            Destroy(manager.gameObject);
        }
        foreach (DayManager manager in FindObjectsOfType<DayManager>())
        {
            Destroy(manager.gameObject);
        }
        foreach (RecordKeeper records in FindObjectsOfType<RecordKeeper>())
        {
            Destroy(records.gameObject);
        }
        Destroy(gameObject);
    }
}
