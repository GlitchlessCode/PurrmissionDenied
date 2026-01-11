using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct CoupledEventSequence
{
    public UnitGameEvent trigger;
    public DirectMessageSequenceDefinition sequence;
}

public class DirectMessageManager : Subscriber
{
    private enum MessageType
    {
        None,
        Pool,
        Sequence,
    };

    private MessageType lastMessageType = MessageType.None;

    private enum FeedbackState
    {
        NoFeedback,
        GoodFeedback,
        BadFeedback,
    }

    [Header("Feedback")]
    public DirectMessagePoolDefinition StartingGoodMessages;
    public DirectMessagePoolDefinition StartingBadMessages;
    public DirectMessagePoolDefinition StayingGoodMessages;
    public DirectMessagePoolDefinition StayingBadMessages;
    public DirectMessagePoolDefinition GettingGoodMessages;
    public DirectMessagePoolDefinition GettingBadMessages;

    public List<int> FeedbackPoints;

    [Range(0.0f, 1.0f)]
    public float RatioForGood = 0.75f;

    private FeedbackState state;
    private int appealCount;
    private int correctAppealCount;

    [Header("Sequences")]
    public List<CoupledEventSequence> MessageSequences;

    [Header("Audio")]
    public Audio DMArrivedAudio;

    [Header("Event Listeners")]
    public BoolGameEvent AfterAppeal;
    public UnitGameEvent AsyncComplete;

    private bool asyncComplete = false;

    [Header("Events")]
    public DirectMessageGameEvent MessageTarget;
    public AudioGameEvent AudioBus;
    public UnitGameEvent AddTimestamp;

    private int loadedPools = 0;
    private const int TOTAL_POOLS = 6;
    private InternalDirectMessagePool startingGoodMessages;
    private InternalDirectMessagePool startingBadMessages;
    private InternalDirectMessagePool stayingGoodMessages;
    private InternalDirectMessagePool stayingBadMessages;
    private InternalDirectMessagePool gettingGoodMessages;
    private InternalDirectMessagePool gettingBadMessages;
    private List<InternalDirectMessageSequence> messageSequences;

    private Queue<bool> queuedAppeals;
    private Dictionary<Guid, int> queuedSequences;

    private Queue<(MessageType, DirectMessage)> queuedMessages =
        new Queue<(MessageType, DirectMessage)>();
    private bool isRunningQueue = false;

    private bool setup = false;

    public override void Subscribe()
    {
        state = FeedbackState.NoFeedback;
        appealCount = 0;
        correctAppealCount = 0;
        asyncComplete = false;
        isRunningQueue = false;

        if (!setup)
        {
            int persisterCount = FindObjectsOfType<DirectMessageManager>().Count();
            if (persisterCount > 1)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                DontDestroyOnLoad(gameObject);

                AfterAppeal?.Subscribe(OnAfterAppealQueued);

                queuedAppeals = new Queue<bool>();
                StartCoroutine(
                    StartingGoodMessages.GetMessages(
                        (messages) =>
                        {
                            startingGoodMessages = new InternalDirectMessagePool(messages);
                            loadedPools++;
                            catchUpAppeals();
                        }
                    )
                );
                StartCoroutine(
                    StartingBadMessages.GetMessages(
                        (messages) =>
                        {
                            startingBadMessages = new InternalDirectMessagePool(messages);
                            loadedPools++;
                            catchUpAppeals();
                        }
                    )
                );
                StartCoroutine(
                    StayingGoodMessages.GetMessages(
                        (messages) =>
                        {
                            stayingGoodMessages = new InternalDirectMessagePool(messages);
                            loadedPools++;
                            catchUpAppeals();
                        }
                    )
                );
                StartCoroutine(
                    StayingBadMessages.GetMessages(
                        (messages) =>
                        {
                            stayingBadMessages = new InternalDirectMessagePool(messages);
                            loadedPools++;
                            catchUpAppeals();
                        }
                    )
                );
                StartCoroutine(
                    GettingGoodMessages.GetMessages(
                        (messages) =>
                        {
                            gettingGoodMessages = new InternalDirectMessagePool(messages);
                            loadedPools++;
                            catchUpAppeals();
                        }
                    )
                );
                StartCoroutine(
                    GettingBadMessages.GetMessages(
                        (messages) =>
                        {
                            gettingBadMessages = new InternalDirectMessagePool(messages);
                            loadedPools++;
                            catchUpAppeals();
                        }
                    )
                );

                setup = true;
            }
        }
        else
        {
            lastMessageType = MessageType.None;
            AfterAppeal?.Subscribe(OnAfterAppeal);
        }

        AsyncComplete?.Subscribe(OnAsyncComplete);
        queuedSequences = new Dictionary<Guid, int>();
        messageSequences = new List<InternalDirectMessageSequence>();
        foreach (CoupledEventSequence coupled in MessageSequences)
        {
            if (coupled.sequence != null && coupled.trigger != null)
            {
                Guid id = Guid.NewGuid();
                Action action = CreateOnSequenceTriggerQueued(id);
                StartCoroutine(
                    coupled.sequence.GetMessages(
                        (messages) =>
                        {
                            InternalDirectMessageSequence seq = new InternalDirectMessageSequence(
                                messages
                            );
                            messageSequences.Add(seq);
                            catchUpSequence(id, seq, action, coupled.trigger);
                        }
                    )
                );
                queuedSequences[id] = 0;

                coupled.trigger.Subscribe(action);
            }
        }
    }

    void OnAsyncComplete()
    {
        if (!asyncComplete)
        {
            asyncComplete = true;
            if (!isRunningQueue && queuedMessages.Count > 0)
            {
                StartCoroutine(RunQueue());
            }
        }
    }

    void OnAfterAppealQueued(bool success)
    {
        queuedAppeals.Enqueue(success);
    }

    void OnAfterAppeal(bool currentCorrect)
    {
        appealCount++;
        if (currentCorrect)
            correctAppealCount++;

        if (FeedbackPoints.Contains(appealCount))
        {
            InternalDirectMessagePool goodPool;
            InternalDirectMessagePool badPool;
            switch (state)
            {
                case FeedbackState.BadFeedback:
                    goodPool = gettingGoodMessages;
                    badPool = stayingBadMessages;
                    break;
                case FeedbackState.GoodFeedback:
                    goodPool = stayingGoodMessages;
                    badPool = gettingBadMessages;
                    break;
                case FeedbackState.NoFeedback:
                default:
                    goodPool = startingGoodMessages;
                    badPool = startingBadMessages;
                    break;
            }

            if (((float)correctAppealCount / (float)appealCount) >= RatioForGood)
            {
                QueueMessage(MessageType.Pool, goodPool.GetRandomMessage());
                state = FeedbackState.GoodFeedback;
            }
            else
            {
                QueueMessage(MessageType.Pool, badPool.GetRandomMessage());
                state = FeedbackState.BadFeedback;
            }
        }
    }

    void catchUpAppeals()
    {
        if (loadedPools == TOTAL_POOLS)
        {
            if (queuedAppeals.Count > 0)
            {
                foreach (bool success in queuedAppeals)
                {
                    OnAfterAppeal(success);
                }
                queuedAppeals.Clear();
            }

            AfterAppeal?.Subscribe(OnAfterAppeal);
            AfterAppeal?.Unsubscribe(OnAfterAppealQueued);
        }
    }

    Action CreateOnSequenceTriggerQueued(Guid id)
    {
        void OnSequenceTriggerQueued()
        {
            queuedSequences[id] += 1;
        }

        return OnSequenceTriggerQueued;
    }

    Action CreateOnSequence(InternalDirectMessageSequence seq)
    {
        void OnSequenceTrigger()
        {
            foreach (DirectMessage msg in seq.GetMessages())
            {
                QueueMessage(MessageType.Sequence, msg);
            }
        }

        return OnSequenceTrigger;
    }

    void QueueMessage(MessageType type, DirectMessage message)
    {
        queuedMessages.Enqueue((type, message));
        if (!isRunningQueue && asyncComplete)
        {
            StartCoroutine(RunQueue());
        }
    }

    IEnumerator RunQueue()
    {
        isRunningQueue = true;

        while (queuedMessages.Count > 0)
        {
            var (type, message) = queuedMessages.Dequeue();
            yield return new WaitForSeconds(message.message.Length * 0.0019f + 0.5f);
            if (type != lastMessageType && lastMessageType != MessageType.None)
                AddTimestamp?.Emit();
            lastMessageType = type;
            MessageTarget?.Emit(message);
            if (DMArrivedAudio.clip != null)
            {
                AudioBus?.Emit(DMArrivedAudio);
            }
        }

        isRunningQueue = false;
    }

    void catchUpSequence(
        Guid id,
        InternalDirectMessageSequence seq,
        Action old_action,
        UnitGameEvent trigger
    )
    {
        Action action = CreateOnSequence(seq);
        for (int count = 0; count < queuedSequences[id]; count++)
            action();
        queuedSequences[id] = -1;
        trigger.Subscribe(action);
        trigger.Unsubscribe(old_action);
    }
}

class InternalDirectMessagePool
{
    List<DirectMessage> messages;
    private System.Random randState;

    public InternalDirectMessagePool(List<DirectMessage> messagesIn)
    {
        messages = messagesIn;
        randState = new System.Random();
    }

    public DirectMessage GetRandomMessage()
    {
        int idx = randState.Next(messages.Count);
        DirectMessage item = messages[idx];
        messages.RemoveAt(idx);
        return item;
    }
}

class InternalDirectMessageSequence
{
    List<DirectMessage> messages;

    public InternalDirectMessageSequence(List<DirectMessage> messagesIn)
    {
        messages = messagesIn;
    }

    public List<DirectMessage> GetMessages()
    {
        return messages;
    }
}
