﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyConversations : MonoBehaviour, IEventListener {

    private readonly Message[] playerLoss = {
        new Message("Player", "...")
    };
    private readonly Message[] playerWin = {
        new Message("Dummy", "Good luck.")
    };

    private Conversation[,] conversations = new Conversation[2, 2];

    void Awake() {
        conversations[1, 0] = new Conversation(playerLoss);
        conversations[1, 1] = new Conversation(playerWin);
    }

    void OnEnable() {
        EventMessanger.GetInstance().SubscribeEvent(typeof(PostBattleDialogStartEvent), this);
    }

    void OnDisable() {
        EventMessanger.GetInstance().UnsubscribeEvent(typeof(PostBattleDialogStartEvent), this);
    }

    private void Converse(int phase, bool playerVictory) {
        int victoryIndex = playerVictory ? 1 : 0;
        List<Conversation> conversationList = new List<Conversation>();
        if (phase < conversations.GetLength(0) && victoryIndex < conversations.GetLength(1) &&
            conversations[phase, victoryIndex] != null) {
            conversationList.Add(conversations[phase, victoryIndex]);
        } else {
            Message[] defaultMessage = { new Message("Player", "It's over!") };
            conversationList.Add(new Conversation(defaultMessage));
        }
        DialogueManager.Instance.SetConversationList(conversationList);
        DialogueManager.Instance.NextConversation();
    }

    public void ConsumeEvent(IEvent e) {
        if (e.GetType() == typeof(PostBattleDialogStartEvent)) {
            PostBattleDialogStartEvent dialogStartEvent = e as PostBattleDialogStartEvent;
            Converse(dialogStartEvent.phase, dialogStartEvent.playerVictory);
        }
    }

}
