# Sendbird Unity sample
![Platform](https://img.shields.io/badge/platform-UNITY%20%7C%20.NET-orange.svg)
![Languages](https://img.shields.io/badge/language-C%23-orange.svg)

## Introduction

[Sendbird](https://sendbird.com) provides the chat API and SDK for your app, enabling real-time communication among the users. Here are various samples built using Sendbird Chat SDK.

- **Chat sample** has core chat features. Group channel and open channel are the two main channel types in which you can create various subtypes where users can send and receive messages. This sample is written in C# with [Sendbird Chat SDK](https://github.com/sendbird/SendBird-SDK-dotNET).

<br />

## 🔒 Security tip
When a new Sendbird application is created in the dashboard the default security settings are set permissive to simplify running samples and implementing your first code.

Before launching make sure to review the security tab under ⚙️ Settings -> Security, and set Access token permission to Read Only or Disabled so that unauthenticated users can not login as someone else. And review the Access Control lists. Most apps will want to disable "Allow retrieving user list" as that could expose usage numbers and other information.

## Installation

To use our Unity samples, you should first install [Chat SDK for Unity](https://github.com/sendbird/SendBird-SDK-dotNET) on your system.

### Requirements

This sample project is tested on `Unity 2017.4.0f1`.

### Chat sample

The sample project works based on our test application's `APP_ID`. You should replace with your own Sendbird application's `APP_ID` which can be found in [Sendbird Dashboard](https://dashboard.sendbird.com).

### Try the sample app using your data 

If you would like to try the sample app specifically fit to your usage, you can do so by replacing the default sample app ID with yours, which you can obtain by [creating your Sendbird application from the dashboard](https://sendbird.com/docs/chat/v3/unity/getting-started/chat-sdk-setup#2-step-1-create-a-sendbird-application-from-your-dashboard). Furthermore, you could also add data of your choice on the dashboard to test. This will allow you to experience the sample app with data from your Sendbird application. 

<br />

## Previous versions

You can access the version 2 sample from the repository by switching from `master` to `v2` branch.
