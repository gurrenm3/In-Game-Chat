﻿using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using BTD_Mod_Helper.Extensions;
using Assets.Scripts.Unity;
using UnityEngine.EventSystems;
using System;
using In_Game_Chat.Messages;
using System.Linq;
using System.Collections.Generic;

namespace In_Game_Chat
{
    public class ChatBox
    {
        public bool instantiated = false;
        private AssetBundle assetBundle;
        private GameObject chatBoxCanvasGo;

        private GameObject instantiatedChatBox;
        private Text messageDisplay;
        Image textArea;
        ScrollRect textAreaScrollRect;

        private InputField messageInput;
        private KeyCode SendKey = KeyCode.Return;
        private KeyCode SendKeyAlt = KeyCode.KeypadEnter;

        private Button closeButton;
        private List<Button> dmButtons = new List<Button>();

        public bool IsFocused { get { return IsChatFocused(); } }
        public bool Visible { get { return instantiatedChatBox.activeSelf; } set { SetVisibility(value); } }

        private float timeToIgnoreHotkeys = 0f;
        const float timeToIgnoreAfterSendMessage = 1f;

        public void InitializeChatBox()
        {
            if (assetBundle == null)
                assetBundle = AssetBundle.LoadFromMemory(Properties.Resources.chatbox_final);

            if (chatBoxCanvasGo == null)
                chatBoxCanvasGo = assetBundle.LoadAsset("Canvas_Final").Cast<GameObject>();

            if (instantiatedChatBox == null)
                instantiatedChatBox = GameObject.Instantiate(chatBoxCanvasGo);


            textArea = instantiatedChatBox.transform.Find("ChatBox/Image").GetComponent<Image>();
            textAreaScrollRect = instantiatedChatBox.transform.Find("ChatBox/Image").GetComponent<ScrollRect>();
            messageDisplay = instantiatedChatBox.transform.Find("ChatBox/Image/MessageHistory").GetComponent<Text>();

            messageDisplay.text = "";
            messageInput = instantiatedChatBox.transform.Find("ChatBox/Input").GetComponent<InputField>();

            closeButton = instantiatedChatBox.transform.Find("ChatBox/CloseButton").GetComponent<Button>();
            closeButton.onClick.AddListener(new Action(() => { CloseButtonClick(); }));
            AddDmButtons();

            instantiatedChatBox.SetActive(false);
            instantiated = true;
        }

        private void AddDmButtons()
        {
            for (int i = 0; i < 4; i++)
            {
                int buttonNum = i + 1;
                var button = instantiatedChatBox.transform.Find($"ChatBox/DM_Buttons/Player {buttonNum}").GetComponent<Button>();
                button.onClick.AddListener(new Action(() => { DmButtonPressed(button); }));
                dmButtons.Add(button);
            }
        }

        private void DmButtonPressed(Button sender)
        {
            var text = messageInput.text;
            if (IsPrivateMessage(text))
                text = text.Replace("/player1 ", "").Replace("/player2 ", "").Replace("/player3 ", "").Replace("/player4 ", "");

            var recipient = sender.name.ToLower().Replace(" ", "");
            var newMessage = $"/{recipient} {text}";
            messageInput.text = newMessage;
            FocusOnTextInput();
        }


        public void Update() => CheckSendMessages();

        private void CheckSendMessages()
        {
            if (messageInput == null || !messageInput.IsActive())
                return;

            if (Input.GetKeyDown(SendKey) || Input.GetKeyDown(SendKeyAlt))
            {
                SendMessage();
                FocusOnTextInput();
            }
        }


        public void ShowHideChatBox()
        {
            if (!instantiated)
                InitializeChatBox();

            bool visibility = (instantiatedChatBox.activeSelf) ? false : true;
            instantiatedChatBox.SetActive(visibility);

            messageInput.text = "";
            if (visibility == true)
                FocusOnTextInput();
        }


        public void SendMessage()
        {
            string message = messageInput.text;
            if (string.IsNullOrEmpty(message))
                return;

            new Chat_Message(message);
        }

        public void SendMessage(Chat_Message message)
        {
            byte? peerID = GetPeerID(message.Message);
            if (peerID.HasValue)
                message.Message = message.Message.Remove(0, 9);


            var json = message.Serialize();
            SendMessageToPeers(json, peerID);
            messageInput.text = "";
            UpdateChatLog(message.Message, "Me");
        }

        private void SendMessageToPeers(string message, byte? peerId)
        {
            if (Game.instance?.nkGI != null)
                Game.instance.nkGI.SendMessage(message, peerId, code: Chat_Message.chatCoopCode);
        }

        private byte? GetPeerID(string message)
        {
            if (!IsPrivateMessage(message))
                return null;

            Int32.TryParse(message[7].ToString(), out int id);
            return (byte?)id;
        }

        private bool IsPrivateMessage(string message) => message.StartsWith("/player1 ") || message.StartsWith("/player2 ") 
            || message.StartsWith("/player3 ") || message.StartsWith("/player4 ");

        public void UpdateChatLog(string message, string sender)
        {
            ShowMessage($"{sender}:  {message}");
            IncreaseChatlogSize();

            textAreaScrollRect.normalizedPosition = new Vector2(0, 0);
            timeToIgnoreHotkeys = Time.time + timeToIgnoreAfterSendMessage;
            SpawnBloonsIfPossible(message);
        }

        public void ShowMessage(string message)
        {
            if (!instantiatedChatBox.activeSelf)
                ShowHideChatBox();

            var newTest = $"{messageDisplay.text}\n  {message}";
            messageDisplay.text = newTest;
        }


        private void IncreaseChatlogSize()
        {
            var msgDisplaySize = messageDisplay.rectTransform.sizeDelta;

            const float bufferSpace = 1.001f;
            var spaceToAdd = messageDisplay.fontSize + bufferSpace;

            messageDisplay.rectTransform.sizeDelta = new Vector2(msgDisplaySize.x, msgDisplaySize.y + spaceToAdd);
        }

        public void ClearChatLog()
        {
            if (messageDisplay != null)
                messageDisplay.text = "";
        }

        

        private bool IsChatFocused()
        {
            if (messageInput == null || !messageInput.IsActive())
                return false;

            bool inputFocused = messageInput.isFocused;
            bool ignoreHotkeys = Time.time < timeToIgnoreHotkeys;
            return inputFocused || ignoreHotkeys;
        }

        private void FocusOnTextInput()
        {
            messageInput.OnPointerClick(new PointerEventData(EventSystem.current));
        }

        private void SetVisibility(bool visible)
        {
            if (instantiatedChatBox != null)
                instantiatedChatBox.SetActive(visible);
        }

        private void CloseButtonClick() => ShowHideChatBox();


        private void SpawnBloonsIfPossible(string message)
        {
            var split = message.Split(' ');
            var bloonName = split[split.Length - 1].Trim(' ');

            var bloonModel = Game.instance.model.bloons.FirstOrDefault(bloon => bloon.name == bloonName);

            if (bloonModel != null)
                bloonModel.SpawnBloonModel();
        }
    }
}