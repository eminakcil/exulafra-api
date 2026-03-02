# Exulofra API - Real-Time Voice Translation and Dubbing Server

*Bunu [Türkçe](README.md) oku*

This project is a real-time voice processing and translation service I developed using .NET 10 and Vertical Slice Architecture (VSA) principles.

The system receives raw microphone audio from clients via SignalR and MessagePack protocols. It then processes these audio streams instantly using Azure Cognitive Services, translates them, and sends the synthesized voice in the target language back to the client directly in Base64 format.

## Core Features

* **Real-Time Communication:** High-performance, zero-latency binary data streaming with SignalR and MessagePack.
* **Dynamic Voice Synthesis:** Translations are read with voices appropriate for the selected language and character (e.g., en-US-JennyNeural or tr-TR-AhmetNeural), sped up using SSML infrastructure.
* **Advanced Session Modes:**
  * **Dubbing (1):** Simultaneous translation and voice dubbing in the target language.
  * **Reporting (2):** Dictation only. No translation or voice synthesis is performed, optimizing cost and performance.
  * **Dialogue (3):** Face-to-face conversation on a single device for people speaking two different languages. The speaker is automatically detected using Azure Continuous Language Detection.
  * **Broadcast (4):** Silent subtitle generation via system audio or screen sharing.
* **Security and Isolation:** Thanks to the JWT Bearer infrastructure and CreatorUserId, each user's sessions and historical records are completely isolated. Websocket connections are also protected with tokens.

## Installation and Requirements

To run the project in your local environment, you need an Azure account and a Speech Services resource. The system delegates the heavy audio processing load to the Azure cloud infrastructure instead of your local hardware.

### Azure Settings

You need to add the API key and region of the Speech Services resource you created via the Azure Portal to your appsettings.json file as follows:

```json
{
  "Azure": {
    "SpeechKey": "YOUR_AZURE_SPEECH_KEY",
    "SpeechRegion": "westeurope"
  }
}

```

### Database and Execution

The project uses Entity Framework Core. To set up the database and start the project, simply run the following commands in your terminal:

```bash
dotnet ef database update
dotnet run

```

## API Documentation (Scalar)

We integrated Scalar into the project so you can test all the REST endpoints we developed. After starting the application, you can go to the following address in your browser to examine the API in detail and perform your tests by getting a JWT token:

`https://localhost:<PORT>/scalar`

## SignalR Hub Usage

We use the /translation-hub endpoint for real-time operations. You can integrate it on the client side with the following steps:

1. **Connection:** The connection is initiated by sending the JWT Token as a QueryString (?access_token=...). The use of the MessagePack protocol is mandatory.
2. **Join:** Enter the relevant room with the Invoke("JoinSession", sessionId) command.
3. **Streaming:** A Subject channel is opened and passed to the StartStream method, and the 16kHz PCM audio data captured from the microphone starts streaming.
4. **Listening:**
* ReceivePartial: Text that flows instantly before the sentence finishes.
* ReceiveTranslation: The final translation and speaker tag generated when the sentence ends.
* ReceiveAudio: Ready-to-play Base64 formatted audio data synthesized in the target language.
