using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(menuName = "Direct Messages/Sequence")]
public class DirectMessageSequenceDefinition : ScriptableObject
{
    public string directory;
    public List<string> dmFiles;

    public IEnumerator GetMessages(Action<List<DirectMessage>> callback)
    {
        yield return JSONImporter.ImportFiles<DirectMessage>(
            Path.Combine("lang", "en", "messages", directory),
            dmFiles,
            (files) =>
            {
                List<DirectMessage> messagesOut = new List<DirectMessage>();
                foreach (string filename in dmFiles)
                {
                    messagesOut.Add(files[filename]);
                }

                callback(messagesOut);
            }
        );
    }
}
